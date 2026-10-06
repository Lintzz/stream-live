using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// Live que falha ao subir (tela ou áudio indisponível, FFmpeg bloqueado pelo antivírus) não
/// pode ficar anunciada: antes, o servidor já respondia "em live" aos amigos e a janela
/// mostrava AO VIVO enquanto nenhum quadro saía — quem entrava via tela preta.
/// </summary>
public class HostBroadcastStartTests
{
    private static BroadcastSettings Settings(bool privateLive = false) => new()
    {
        Source = new CaptureSource { Title = "Tela 1", ScreenBounds = new System.Drawing.Rectangle(0, 0, 640, 360) },
        RoomPassword = "senha-da-sala",
        InvitedIps = privateLive ? new[] { "26.10.0.5" } : Array.Empty<string>()
    };

    [Fact]
    public async Task FailedStartLeavesNothingAnnounced()
    {
        var server = new SignalingServer();
        using var broadcast = new HostBroadcast(server)
        {
            InitializeHost = (_, _) => throw new InvalidOperationException("captura indisponível")
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => broadcast.StartAsync(Settings(privateLive: true)));

        Assert.Equal("captura indisponível", ex.Message);
        Assert.False(server.IsStreaming);
        Assert.False(broadcast.IsBroadcasting);
        Assert.False(broadcast.IsPrivateLive);
        Assert.Equal(string.Empty, server.RoomPassword);
    }

    [Fact]
    public async Task SuccessfulStartAnnouncesTheLive()
    {
        var server = new SignalingServer();
        using var broadcast = new HostBroadcast(server)
        {
            InitializeHost = (_, _) => { }
        };

        await broadcast.StartAsync(Settings());

        Assert.True(server.IsStreaming);
        Assert.True(broadcast.IsBroadcasting);
        Assert.Equal("senha-da-sala", server.RoomPassword);
    }
}
