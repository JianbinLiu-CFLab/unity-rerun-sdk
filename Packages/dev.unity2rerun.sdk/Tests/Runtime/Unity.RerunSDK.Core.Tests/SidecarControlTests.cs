// Pure C# tests for Phase 10 sidecar control contract.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Unity.RerunSDK.Unity.Control;
using Xunit;

public class SidecarControlTests
{
    [Fact]
    public void Command_parser_reads_set_pose_vectors()
    {
        var ok = RerunControlCommand.TryParseJson(
            "{\"type\":\"set_pose\",\"position\":[1,2,3],\"rotationEuler\":[4,5,6]}",
            out var command,
            out var error);

        Assert.True(ok, error);
        Assert.Equal(RerunControlCommandType.SetPose, command.Type);
        Assert.True(command.HasPosition);
        Assert.Equal(1f, command.Position.X);
        Assert.Equal(2f, command.Position.Y);
        Assert.Equal(3f, command.Position.Z);
        Assert.True(command.HasRotationEuler);
        Assert.Equal(4f, command.RotationEuler.X);
        Assert.Equal(5f, command.RotationEuler.Y);
        Assert.Equal(6f, command.RotationEuler.Z);
    }

    [Fact]
    public void Command_parser_rejects_unknown_type()
    {
        var ok = RerunControlCommand.TryParseJson(
            "{\"type\":\"teleport\"}",
            out _,
            out var error);

        Assert.False(ok);
        Assert.Contains("Unsupported command", error);
    }

    [Fact]
    public void State_serializes_actual_control_url_and_last_command()
    {
        var state = new RerunControlState
        {
            Position = new RerunControlVector3(1f, 2f, 3f),
            RotationEuler = new RerunControlVector3(4f, 5f, 6f),
            Scale = 1.5f,
            Color = new RerunControlColor(0.1f, 0.2f, 0.3f, 1f),
            CommandCount = 7,
            LastCommand = "set_color",
            ControlUrl = "http://127.0.0.1:49152/"
        };

        var json = state.ToJson();

        Assert.Contains("\"position\":[1", json);
        Assert.Contains("\"rotationEuler\":[4", json);
        Assert.Contains("\"scale\":1.5", json);
        Assert.Contains("\"commandCount\":7", json);
        Assert.Contains("\"lastCommand\":\"set_color\"", json);
        Assert.Contains("\"controlUrl\":\"http://127.0.0.1:49152/\"", json);
    }

    [Fact]
    public void State_serializes_supported_actions_and_writable_parameters()
    {
        var state = new RerunControlState
        {
            Position = new RerunControlVector3(0f, 1f, 2f),
            RotationEuler = new RerunControlVector3(3f, 4f, 5f),
            Scale = 1.25f,
            Color = new RerunControlColor(0.1f, 0.2f, 0.3f, 1f),
            CommandCount = 2,
            LastCommand = "set_scale",
            ControlUrl = "http://127.0.0.1:18765/"
        };

        var json = state.ToJson();

        Assert.Contains("\"actions\":[", json);
        Assert.Contains("\"id\":\"reset_pose\"", json);
        Assert.Contains("\"id\":\"set_color_green\"", json);
        Assert.Contains("\"id\":\"scale_up\"", json);
        Assert.Contains("\"parameters\":[", json);
        Assert.Contains("\"name\":\"cube.color\"", json);
        Assert.Contains("\"type\":\"color\"", json);
        Assert.Contains("\"name\":\"cube.scale\"", json);
        Assert.Contains("\"type\":\"float\"", json);
        Assert.Contains("\"writable\":true", json);
    }

    [Fact]
    public async Task Control_server_serves_state_and_applies_commands()
    {
        var state = new RerunControlState
        {
            Position = new RerunControlVector3(0f, 1f, 2f),
            RotationEuler = new RerunControlVector3(0f, 0f, 0f),
            Scale = 1f,
            Color = new RerunControlColor(0f, 1f, 0f, 1f),
            CommandCount = 0,
            LastCommand = "",
            ControlUrl = ""
        };

        using var server = new RerunControlServer(
            () => state,
            command =>
            {
                state.CommandCount++;
                state.LastCommand = RerunControlCommandNames.ToWireName(command.Type);
                return RerunControlCommandResult.Success();
            });
        server.Start(0);
        state.ControlUrl = server.ControlUrl;

        using var http = new HttpClient();
        var stateJson = await http.GetStringAsync(server.ControlUrl + "state");
        Assert.Contains("\"controlUrl\":\"" + server.ControlUrl + "\"", stateJson);

        var response = await http.PostAsync(
            server.ControlUrl + "command",
            new StringContent("{\"type\":\"reset_pose\"}", Encoding.UTF8, "application/json"));

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(1, state.CommandCount);
        Assert.Equal("reset_pose", state.LastCommand);
    }

    [Fact]
    public async Task Control_server_page_renders_state_driven_controls()
    {
        var state = new RerunControlState
        {
            Position = new RerunControlVector3(0f, 0f, 0f),
            RotationEuler = new RerunControlVector3(0f, 0f, 0f),
            Scale = 1f,
            Color = new RerunControlColor(0f, 1f, 0f, 1f),
            ControlUrl = ""
        };

        using var server = new RerunControlServer(
            () => state,
            _ => RerunControlCommandResult.Success());
        server.Start(0);
        state.ControlUrl = server.ControlUrl;

        using var http = new HttpClient();
        var html = await http.GetStringAsync(server.ControlUrl);

        Assert.Contains("renderActions", html);
        Assert.Contains("renderParameters", html);
        Assert.Contains("state.actions", html);
        Assert.Contains("state.parameters", html);
        Assert.Contains("scale_up", html);
        Assert.Contains("set_color_red", html);
    }

    [Fact]
    public void Control_server_classifies_client_disconnects_as_benign()
    {
        Assert.True(RerunControlServer.IsBenignClientDisconnect(
            new IOException("Unable to write data to the transport connection.")));

        Assert.True(RerunControlServer.IsBenignClientDisconnect(
            new IOException("outer", new SocketException((int)SocketError.ConnectionReset))));

        Assert.False(RerunControlServer.IsBenignClientDisconnect(
            new InvalidOperationException("handler failed")));
    }
}
