// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Exercises Tuid Tests behavior for release and regression validation.

// TUID uniqueness tests
using Xunit;
using Unity.RerunSDK.Encoding;
/// <summary>
/// Regression tests for Tuid Tests.
/// </summary>
public class TuidTests
{
    [Fact]
    public void Next_generates_unique_ids()
    {
        var ids = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var t = RerunTuidGenerator.Next();
            var hex = RerunTuidGenerator.ToHexString(t);
            Assert.DoesNotContain(hex, ids);
            ids.Add(hex);
        }
    }

    [Fact]
    public void Roundtrip_bytes()
    {
        var t = RerunTuidGenerator.Next();
        var bytes = RerunTuidGenerator.ToBytes(t);
        Assert.Equal(16, bytes.Length);

        // Big-endian encoding check
        Assert.Equal(t.TimeNs, System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(
            bytes.AsSpan(0, 8)));
        Assert.Equal(t.Inc, System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(
            bytes.AsSpan(8, 8)));
    }

    [Fact]
    public void Hex_string_is_32_chars()
    {
        var t = new RerunTuid(0xABCD, 0x1234);
        var hex = RerunTuidGenerator.ToHexString(t);
        Assert.Equal(32, hex.Length);
        Assert.Equal("000000000000ABCD0000000000001234", hex);
    }
}
