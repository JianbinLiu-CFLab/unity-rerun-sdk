// Phase 4 Gate: managed gRPC feasibility spike.
// Tests Grpc.Net.Client -> Rerun Viewer's MessageProxyService.WriteMessages.

using System.Threading.Channels;
using Grpc.Core;
using Grpc.Net.Client;
using Rerun.LogMsg.V1Alpha1;
using Rerun.Common.V1Alpha1;
using Rerun.SdkComms.V1Alpha1;
using RerunAppId = Rerun.Common.V1Alpha1.ApplicationId;

var endpoint = args.Length > 0 ? args[0] : "http://127.0.0.1:9876";

Console.WriteLine($"Phase 4 gRPC Gate: connecting to {endpoint}");

// 1. Verify plaintext HTTP/2 channel creation
using var channel = GrpcChannel.ForAddress(endpoint, new GrpcChannelOptions
{
    Credentials = ChannelCredentials.Insecure
});
Console.WriteLine("[OK] GrpcChannel created (plaintext HTTP/2)");

// 2. Create client
var client = new MessageProxyService.MessageProxyServiceClient(channel);
Console.WriteLine("[OK] MessageProxyServiceClient created");

// 3. Open client-stream and send a SetStoreInfo
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try
{
    using var call = client.WriteMessages(cancellationToken: cts.Token);

    // Build a minimal SetStoreInfo wrapped in LogMsg
    var logMsg = new LogMsg
    {
        SetStoreInfo = new SetStoreInfo
        {
            RowId = new Tuid { TimeNs = 1, Inc = 1 },
            Info = new StoreInfo
            {
                StoreId = new StoreId
                {
                    Kind = StoreKind.Recording,
                    RecordingId = "grpc-gate-test",
                    ApplicationId = new RerunAppId { Id = "grpc_gate_spike" }
                },
                StoreVersion = new StoreVersion { CrateVersionBits = 0x00170000 },
                StoreSource = new StoreSource
                {
                    Kind = StoreSourceKind.Other,
                    Extra = new StoreSourceExtra
                    {
                        Payload = Google.Protobuf.ByteString.CopyFromUtf8("grpc-gate")
                    }
                }
            }
        }
    };

    var request = new WriteMessagesRequest { LogMsg = logMsg };
    await call.RequestStream.WriteAsync(request);
    Console.WriteLine("[OK] SetStoreInfo sent via WriteMessages");

    // 4. Send a mock ArrowMsg
    var arrowMsg = new ArrowMsg
    {
        StoreId = new StoreId
        {
            Kind = StoreKind.Recording,
            RecordingId = "grpc-gate-test",
            ApplicationId = new RerunAppId { Id = "grpc_gate_spike" }
        },
        Compression = Compression.None,
        Encoding = Rerun.LogMsg.V1Alpha1.Encoding.ArrowIpc,
        UncompressedSize = 0,
        Payload = Google.Protobuf.ByteString.Empty
    };

    var logMsg2 = new LogMsg { ArrowMsg = arrowMsg };
    var request2 = new WriteMessagesRequest { LogMsg = logMsg2 };
    await call.RequestStream.WriteAsync(request2);
    Console.WriteLine("[OK] ArrowMsg sent via WriteMessages");

    // 5. Complete stream and await response
    await call.RequestStream.CompleteAsync();
    var response = await call;
    Console.WriteLine($"[OK] WriteMessages completed: {response}");

    Console.WriteLine("\n=== GATE PASSED ===");
    Console.WriteLine("Managed gRPC (Grpc.Net.Client 2.76.0) successfully connected to Rerun Viewer.");
}
catch (RpcException ex)
{
    Console.WriteLine($"\n[FAIL] gRPC error: {ex.Status.StatusCode} - {ex.Status.Detail}");
    Console.WriteLine("Ensure Rerun Viewer is running: `python -m rerun` or `rerun`");
}
catch (Exception ex)
{
    Console.WriteLine($"\n[FAIL] Unexpected error: {ex.GetType().Name}: {ex.Message}");
}
