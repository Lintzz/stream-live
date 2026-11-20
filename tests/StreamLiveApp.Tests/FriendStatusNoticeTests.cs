using StreamLiveApp.Services;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// O amigo só via você entrar ao vivo (ou abrir o app) na sondagem seguinte, até 5 s depois:
/// "começa a live aí", e o outro ficava esperando a bolinha mudar. Agora quem muda de estado
/// avisa os amigos, e cada um confere o status daquele amigo na hora.
///
/// O aviso é um STATUS_CHECK com <c>Data = "CHANGED"</c>: um app antigo só responde o status
/// e não faz mais nada. Um tipo novo de mensagem cairia no fluxo da live e criaria uma conexão
/// WebRTC à toa na host antiga.
/// </summary>
public class FriendStatusNoticeTests
{
    [Fact]
    public async Task NoticeMakesTheFriendRecheckRightAway()
    {
        var server = new StreamLiveApp.SignalingServer();
        int port = FreePort();
        Assert.True(server.Start("127.0.0.1", port));

        var noticed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.OnFriendStatusChanged += ip => noticed.TrySetResult(ip);

        try
        {
            Assert.True(await FriendStatusService.NotifyChangedAsync("127.0.0.1", port));

            var winner = await Task.WhenAny(noticed.Task, Task.Delay(3000));
            Assert.Same(noticed.Task, winner);
            Assert.Equal("127.0.0.1", noticed.Task.Result);
        }
        finally
        {
            server.Stop();
        }
    }

    /// <summary>
    /// A sondagem comum não pode contar como aviso: A avisado sonda B, B tomaria isso por
    /// aviso e sondaria A de volta, e os dois ficariam se sondando sem parar.
    /// </summary>
    [Fact]
    public async Task OrdinaryStatusCheckIsNotANotice()
    {
        var server = new StreamLiveApp.SignalingServer();
        int port = FreePort();
        Assert.True(server.Start("127.0.0.1", port));

        int notices = 0;
        server.OnFriendStatusChanged += _ => Interlocked.Increment(ref notices);

        try
        {
            Assert.True((await FriendStatusService.CheckAsync("127.0.0.1", port)).IsOnline);
            await Task.Delay(200);

            Assert.Equal(0, notices);
        }
        finally
        {
            server.Stop();
        }
    }

    /// <summary>Com senha na sala o aviso também passa: ele não revela nada da live.</summary>
    [Fact]
    public async Task NoticeReachesAPasswordProtectedRoom()
    {
        var server = new StreamLiveApp.SignalingServer { RoomPassword = "segredo" };
        int port = FreePort();
        Assert.True(server.Start("127.0.0.1", port));

        var noticed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.OnFriendStatusChanged += ip => noticed.TrySetResult(ip);

        try
        {
            Assert.True(await FriendStatusService.NotifyChangedAsync("127.0.0.1", port));
            Assert.Same(noticed.Task, await Task.WhenAny(noticed.Task, Task.Delay(3000)));
        }
        finally
        {
            server.Stop();
        }
    }

    [Fact]
    public async Task NoticeToAnOfflineFriendFailsQuietly()
    {
        Assert.False(await FriendStatusService.NotifyChangedAsync("127.0.0.1", FreePort()));
        Assert.False(await FriendStatusService.NotifyChangedAsync(""));
    }

    private static int FreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
