using System.Reflection;

string pluginsDir = @"D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\Packages\dev.unity2rerun.sdk\Runtime\Plugins";
foreach (var dll in new[] {
    "Grpc.Net.Client.dll", "Grpc.Net.Common.dll", "Grpc.Core.Api.dll",
    "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
    "Microsoft.Extensions.Logging.Abstractions.dll", "System.Threading.Channels.dll" })
{
    var path = Path.Combine(pluginsDir, dll);
    var asm = Assembly.LoadFrom(path);
    Console.WriteLine($"\n=== {dll} ===");
    foreach (var r in asm.GetReferencedAssemblies())
    {
        if (r.Name.Contains("Bcl") || r.Name.Contains("Async") || r.Name.Contains("Diagnostic"))
            Console.WriteLine($"  -> {r.Name} v{r.Version}");
    }
}
