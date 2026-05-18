// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Verifies deterministic Phase 14 compression comparison smoke output.

using System.IO;
using Xunit;

/// <summary>
/// Regression tests for Phase 14 compression evidence smoke generation.
/// </summary>
public class Phase14CompressionComparisonTests
{
    [Fact]
    public void Comparison_writer_outputs_none_and_lz4_rrd_with_matching_inspector_results()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"phase14_comparison_{Path.GetRandomFileName()}");
        var prefix = Path.Combine(dir, "phase14_compression");

        try
        {
            var comparison = Phase14RrdWriter.WriteCompressionComparison(prefix);

            Assert.Equal(Path.GetFullPath(prefix + "_none.rrd"), comparison.NonePath);
            Assert.Equal(Path.GetFullPath(prefix + "_lz4.rrd"), comparison.Lz4Path);
            Assert.True(File.Exists(comparison.NonePath));
            Assert.True(File.Exists(comparison.Lz4Path));

            Assert.True(comparison.NoneResult.ArrowMsgCount >= 4);
            Assert.Equal(comparison.NoneResult.ArrowMsgCount, comparison.NoneResult.CompressionNoneCount);
            Assert.Equal(0, comparison.NoneResult.CompressionLz4Count);
            Assert.True(comparison.NoneResult.IsReleaseEvidenceAccepted);

            Assert.True(comparison.Lz4Result.ArrowMsgCount >= 4);
            Assert.Equal(comparison.Lz4Result.ArrowMsgCount, comparison.Lz4Result.CompressionLz4Count);
            Assert.Equal(0, comparison.Lz4Result.CompressionNoneCount);
            Assert.True(comparison.Lz4Result.IsReleaseEvidenceAccepted);

            var summary = Phase14RrdWriter.FormatComparisonSummary(comparison);
            Assert.Contains("Phase14 Compression Comparison", summary);
            Assert.Contains("None Recording", summary);
            Assert.Contains("LZ4 Recording", summary);
            Assert.Contains("CompressionNone:", summary);
            Assert.Contains("CompressionLz4:", summary);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }
}
