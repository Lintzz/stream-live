using System.Net;
using System.Net.Sockets;
using StreamLiveApp;
using StreamLiveApp.Models;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// Quando o host encerra, a sessão do viewer se fecha sozinha (evento <c>LiveEnded</c>, que tira
/// a célula da grade). Antes ela ficava em "Transmissão encerrada": o amigo continuava marcado
/// como assistindo mesmo offline, quem estava em tela cheia ficava preso nela, e quando o host
/// voltava ao ar era preciso fechar e assistir de novo — a conexão parada já tinha caído.
/// </summary>
public class ViewerLiveEndedTests : IDisposable
{
    private readonly int _port = FreePort();
    private readonly SignalingServer _server = new();
    private readonly Friend _friend = new() { Name = "Ana", Ip = "127.0.0.1" };
    private readonly ViewerSession _session;

    public ViewerLiveEndedTests()
    {
        _session = new ViewerSession(_friend) { Port = _port };
    }

    public void Dispose()
    {
        _session.Dispose();
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

    private Task<string> WatchLiveEnded()
    {
        var ended = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _session.LiveEnded += (_, notice) => ended.TrySetResult(notice);
        return ended.Task;
    }

    [Fact]
    public async Task HostStoppingClosesTheSession()
    {
        _server.IsStreaming = true;
        Assert.True(_server.Start("127.0.0.1", _port));
        var ended = WatchLiveEnded();

        await _session.ConnectAsync();
        Assert.True(_friend.IsWatching);

        // Repete o aviso até ele chegar: o viewer só entra no broadcast depois do
        // CLIENT_CONNECTED, que sai logo após a conexão.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!ended.IsCompleted && DateTime.UtcNow < deadline)
        {
            _server.BroadcastMessage("STREAM_STOPPED");
            await Task.WhenAny(ended, Task.Delay(200));
        }

        Assert.True(ended.IsCompleted, "a sessão não se fechou com o STREAM_STOPPED");
        Assert.Equal("Ana encerrou a live", await ended);
        Assert.False(_friend.IsWatching);
        Assert.False(_session.IsConnected);
    }

    [Fact]
    public async Task HostNotLiveClosesTheSession()
    {
        _server.IsStreaming = false;
        Assert.True(_server.Start("127.0.0.1", _port));
        var ended = WatchLiveEnded();

        await _session.ConnectAsync();

        var notice = await ended.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("Ana não está em live", notice);
        Assert.False(_friend.IsWatching);
    }
}
