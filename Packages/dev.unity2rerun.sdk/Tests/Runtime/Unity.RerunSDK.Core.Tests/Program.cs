// Entry point for generating Phase 3 .rrd from dotnet run
if (args.Length == 0)
{
    var outPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "phase3_scene.rrd");
    Phase3RrdWriter.WritePhase3Rrd(outPath);
    Console.WriteLine($"Phase 3 .rrd written to: {outPath}");
    Console.WriteLine($"Size: {new FileInfo(outPath).Length} bytes");
}
else if (args[0] == "--write-phase3-rrd")
{
    var outPath = args.Length > 1 ? args[1] : "out/phase3_scene.rrd";
    var dir = Path.GetDirectoryName(outPath);
    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    Phase3RrdWriter.WritePhase3Rrd(outPath);
    Console.WriteLine($"Phase 3 .rrd written to: {Path.GetFullPath(outPath)}");
    Console.WriteLine($"Size: {new FileInfo(outPath).Length} bytes");
}
else
{
    Console.WriteLine("Usage: dotnet run [--write-phase3-rrd <output-path>]");
}
