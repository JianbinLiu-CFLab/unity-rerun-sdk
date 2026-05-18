// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Inspects RRD ArrowMsg compression fields for release evidence.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.RerunSDK.IO.Rrd;
using RerunCommon = Rerun.Common.V1Alpha1;
using RerunLogMsg = Rerun.LogMsg.V1Alpha1;

/// <summary>
/// Reads Unity2Rerun RRD files and summarizes ArrowMsg compression fields.
/// </summary>
public static class RrdInspector
{
    public static RrdInspectionResult InspectFile(string path)
    {
        return InspectBytes(File.ReadAllBytes(path), Path.GetFullPath(path));
    }

    public static RrdInspectionResult InspectBytes(byte[] bytes, string? inputPath = null)
    {
        if (bytes.Length < RrdConstants.StreamHeaderSize)
            throw new InvalidDataException("RRD data is shorter than the stream header.");

        ValidateStreamHeader(bytes);

        var sawStreamFooter = HasStreamFooter(bytes);
        var dataEnd = sawStreamFooter
            ? bytes.Length - RrdConstants.StreamFooterFixedSize
            : bytes.Length;
        var offset = RrdConstants.StreamHeaderSize;
        var result = new RrdInspectionResult(inputPath ?? "<memory>", sawStreamFooter);

        while (offset < dataEnd)
        {
            if (dataEnd - offset < RrdConstants.MessageHeaderSize)
                throw new InvalidDataException("RRD data ended before a complete message header.");

            var kind = BitConverter.ToUInt64(bytes, offset);
            var payloadLength = BitConverter.ToUInt64(bytes, offset + 8);
            offset += RrdConstants.MessageHeaderSize;

            if (payloadLength > (ulong)(dataEnd - offset))
            {
                throw new InvalidDataException(
                    $"RRD message declared payload length {payloadLength} but only {dataEnd - offset} bytes remain.");
            }

            var payload = new ReadOnlySpan<byte>(bytes, offset, checked((int)payloadLength));
            offset += checked((int)payloadLength);
            result.RrdRecordCount++;

            if (kind == RrdConstants.MsgKindEnd)
                break;

            if (kind != RrdConstants.MsgKindArrowMsg)
                continue;

            var arrowMsg = RerunLogMsg.ArrowMsg.Parser.ParseFrom(payload.ToArray());
            result.AddArrowMessage(arrowMsg);
        }

        return result;
    }

    public static string FormatSummary(RrdInspectionResult result)
    {
        var unknownValues = result.UnknownCompressionValues.Count == 0
            ? "none"
            : string.Join(", ", result.UnknownCompressionValues
                .OrderBy(pair => pair.Key)
                .Select(pair => $"{pair.Key}={pair.Value}"));
        var ratio = result.StoredToUncompressedRatio.HasValue
            ? result.StoredToUncompressedRatio.Value.ToString("0.000000", CultureInfo.InvariantCulture)
            : "n/a";

        var builder = new StringBuilder();
        builder.AppendLine($"Input: {result.InputPath}");
        builder.AppendLine($"RrdRecords: {result.RrdRecordCount}");
        builder.AppendLine($"ArrowMsg: {result.ArrowMsgCount}");
        builder.AppendLine($"CompressionNone: {result.CompressionNoneCount}");
        builder.AppendLine($"CompressionLz4: {result.CompressionLz4Count}");
        builder.AppendLine($"CompressionOther: {result.UnknownCompressionCount}");
        builder.AppendLine($"UnknownValues: {unknownValues}");
        builder.AppendLine($"StoredPayloadBytes: {result.TotalStoredPayloadBytes}");
        builder.AppendLine($"DeclaredUncompressedBytes: {result.TotalDeclaredUncompressedBytes}");
        builder.AppendLine($"StoredToUncompressedRatio: {ratio}");
        builder.AppendLine($"SawStreamFooter: {result.SawStreamFooter}");
        builder.Append($"Accepted: {result.IsReleaseEvidenceAccepted}");
        return builder.ToString();
    }

    private static void ValidateStreamHeader(byte[] bytes)
    {
        for (var i = 0; i < RrdConstants.FourCC.Length; i++)
        {
            if (bytes[i] != RrdConstants.FourCC[i])
                throw new InvalidDataException("RRD stream header does not start with RRF2.");
        }
    }

    private static bool HasStreamFooter(byte[] bytes)
    {
        if (bytes.Length < RrdConstants.StreamFooterFixedSize)
            return false;

        var fixedOffset = bytes.Length - RrdConstants.StreamFooterStaticPartSize;
        return bytes[fixedOffset] == (byte)'R'
            && bytes[fixedOffset + 1] == (byte)'R'
            && bytes[fixedOffset + 2] == (byte)'F'
            && bytes[fixedOffset + 3] == (byte)'2'
            && bytes[fixedOffset + 4] == (byte)'F'
            && bytes[fixedOffset + 5] == (byte)'O'
            && bytes[fixedOffset + 6] == (byte)'O'
            && bytes[fixedOffset + 7] == (byte)'T';
    }
}

/// <summary>
/// Compression statistics collected from one RRD file.
/// </summary>
public sealed class RrdInspectionResult
{
    private readonly Dictionary<int, int> _unknownCompressionValues = new();

    public RrdInspectionResult(string inputPath, bool sawStreamFooter)
    {
        InputPath = inputPath;
        SawStreamFooter = sawStreamFooter;
    }

    public string InputPath { get; }
    public bool SawStreamFooter { get; }
    public int RrdRecordCount { get; set; }
    public int ArrowMsgCount { get; private set; }
    public int CompressionNoneCount { get; private set; }
    public int CompressionLz4Count { get; private set; }
    public int UnknownCompressionCount { get; private set; }
    public ulong TotalStoredPayloadBytes { get; private set; }
    public ulong TotalDeclaredUncompressedBytes { get; private set; }
    public IReadOnlyDictionary<int, int> UnknownCompressionValues => _unknownCompressionValues;

    public double? StoredToUncompressedRatio
    {
        get
        {
            if (TotalDeclaredUncompressedBytes == 0)
                return null;

            return TotalStoredPayloadBytes / (double)TotalDeclaredUncompressedBytes;
        }
    }

    public bool IsReleaseEvidenceAccepted => ArrowMsgCount > 0 && UnknownCompressionCount == 0;

    public void AddArrowMessage(RerunLogMsg.ArrowMsg arrowMsg)
    {
        ArrowMsgCount++;
        TotalStoredPayloadBytes += (ulong)arrowMsg.Payload.Length;
        TotalDeclaredUncompressedBytes += arrowMsg.UncompressedSize;

        switch (arrowMsg.Compression)
        {
            case RerunCommon.Compression.None:
                CompressionNoneCount++;
                break;
            case RerunCommon.Compression.Lz4:
                CompressionLz4Count++;
                break;
            default:
                UnknownCompressionCount++;
                var value = (int)arrowMsg.Compression;
                _unknownCompressionValues.TryGetValue(value, out var count);
                _unknownCompressionValues[value] = count + 1;
                break;
        }
    }
}
