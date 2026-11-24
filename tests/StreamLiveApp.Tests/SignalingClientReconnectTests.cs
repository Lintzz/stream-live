using System.Net;
using System.Net.Sockets;
using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// Reconexão automática do viewer, contra um SignalingServer de verdade que cai e volta.
///
/// O laço de tentativas chamava <c>WebsocketClient.Reconnect()</c> com a reconexão da própria
/// biblioteca desligada. Nesse modo, a queda marca o cliente como parado (<c>IsStarted =
/// false</c>), e o <c>Reconnect()</c> de um cliente parado volta sem fazer nada: a tela contava
/// "Reconectando... (1/8)" até "(8/8)" sem nenhuma tentativa real e terminava em "Conexão
/// perdida". Só o botão Reconectar (cliente novo) trazia a live de volta.
/// </summary>
public class SignalingClientReconnectTests : IDisposable
{
    private readonly int _port = FreePort();
    private SignalingServer _server = new();
    private readonly SignalingClient _client = new();

    public SignalingClientReconnectTests()
    {
        _server.IsStreaming = true;
        Assert.True(_server.Start("127.0.0.1", _port), "servidor de sinalização não subiu");
    }

    public void Dispose()
    {
        _client.Stop();
        _server.Stop();
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private void RestartServer()
    {
        _server = new SignalingServer { IsStreaming = true };
        Assert.True(_server.Start("127.0.0.1", _port), "servidor de sinalização não voltou");
    }

    [Theory]
    [InlineData(0)]      // a queda mais comum na VPN: volta antes da primeira tentativa
    [InlineData(2500)]   // fora do ar por algumas tentativas
    public async Task ReconnectsAfterTheHostDropsAndComesBack(int downtimeMs)
    {
        var firstConnect = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gaveUp = false;

        _client.OnConnected += isReconnect =>
        {
            if (isReconnect) reconnected.TrySetResult();
            else firstConnect.TrySetResult();
        };
        _client.OnReconnectFailed += () => gaveUp = true;

        await _client.StartAsync("127.0.0.1", _port);
        await firstConnect.Task.WaitAsync(TimeSpan.FromSeconds(5));

        _server.Stop();
        if (downtimeMs > 0) await Task.Delay(downtimeMs);
        RestartServer();

        var done = await Task.WhenAny(reconnected.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        Assert.False(gaveUp, "desistiu de reconectar com o host de volta no ar");
        Assert.True(done == reconnected.Task, "não reconectou depois que o host voltou");
    }
}
