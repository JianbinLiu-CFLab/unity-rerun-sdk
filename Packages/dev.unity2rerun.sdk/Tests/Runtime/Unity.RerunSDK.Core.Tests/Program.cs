// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Exercises Program behavior for release and regression validation.

// Entry point for generating Phase3 .rrd from dotnet run
if (args.Length == 0)
{
    var outPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "phase3_scene.rrd");
    Phase3RrdWriter.WritePhase3Rrd(outPath);
    Console.WriteLine($"Phase3 .rrd written to: {outPath}");
    Console.WriteLine($"Size: {new FileInfo(outPath).Length} bytes");
}
else if (args[0] == "--write-phase3-rrd")
{
    var outPath = args.Length > 1 ? args[1] : "out/phase3_scene.rrd";
    var dir = Path.GetDirectoryName(outPath);
    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    Phase3RrdWriter.WritePhase3Rrd(outPath);
    Console.WriteLine($"Phase3 .rrd written to: {Path.GetFullPath(outPath)}");
    Console.WriteLine($"Size: {new FileInfo(outPath).Length} bytes");
}
else if (args[0] == "--write-phase8-rrd")
{
    var outPath = args.Length > 1 ? args[1] : "out/phase8_scene.rrd";
    var dir = Path.GetDirectoryName(outPath);
    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    Phase8RrdWriter.WritePhase8Rrd(outPath);
    Console.WriteLine($"Phase8 .rrd written to: {Path.GetFullPath(outPath)}");
    Console.WriteLine($"Size: {new FileInfo(outPath).Length} bytes");
}
else if (args[0] == "--write-phase10-rrd")
{
    var outPath = args.Length > 1 ? args[1] : "out/phase10_scene.rrd";
    var dir = Path.GetDirectoryName(outPath);
    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    Phase10RrdWriter.WritePhase10Rrd(outPath);
    Console.WriteLine($"Phase10 .rrd written to: {Path.GetFullPath(outPath)}");
    Console.WriteLine($"Size: {new FileInfo(outPath).Length} bytes");
}
else if (args[0] == "--write-phase11-rrd")
{
    var outPath = args.Length > 1 ? args[1] : "out/phase11_scene.rrd";
    var dir = Path.GetDirectoryName(outPath);
    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    Phase11RrdWriter.WritePhase11Rrd(outPath);
    Console.WriteLine($"Phase11 .rrd written to: {Path.GetFullPath(outPath)}");
    Console.WriteLine($"Size: {new FileInfo(outPath).Length} bytes");
}
else
{
    Console.WriteLine("Usage: dotnet run [--write-phase3-rrd <output-path>] [--write-phase8-rrd <output-path>] [--write-phase10-rrd <output-path>] [--write-phase11-rrd <output-path>]");
}
