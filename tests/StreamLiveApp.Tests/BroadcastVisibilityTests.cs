using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// Desde que a caixa "Live privada" saiu do modal de transmitir, quem decide se a live é
/// privada é a lista de convidados: ninguém marcado = pública, alguém marcado = só para essas
/// pessoas. Se essa regra voltar a depender de outro campo, uma live com convidados pode sair
/// pública — e os não convidados veem e entram.
/// </summary>
public class BroadcastVisibilityTests
{
    private static BroadcastSettings With(params string[] invited) => new()
    {
        Source = new CaptureSource { Title = "Tela 1" },
        InvitedIps = invited
    };

    // Os dois são amigos: o portão da lista de amigos deixa ambos passarem, então quem
    // decide é só a lista de convidados.
    private static SignalingServer FriendsServer()
    {
        var server = new SignalingServer { RestrictToAllowedIps = true };
        server.SetAllowedIps(new[] { "26.10.0.5", "26.10.0.6" });
        return server;
    }

    [Fact]
    public void NoInvitedFriendsMeansPublic()
    {
        Assert.False(With().PrivateLive);
    }

    [Fact]
    public void AnyInvitedFriendMakesItPrivate()
    {
        Assert.True(With("26.10.0.5").PrivateLive);
        Assert.True(With("26.10.0.5", "26.10.0.6").PrivateLive);
    }

    [Fact]
    public async Task InvitedFriendsAreTheOnlyOnesLetIn()
    {
        var server = FriendsServer();
        using var broadcast = new HostBroadcast(server) { InitializeHost = (_, _) => { } };

        await broadcast.StartAsync(With("26.10.0.5"));

        Assert.True(broadcast.IsPrivateLive);
        Assert.Equal(1, broadcast.InvitedCount);
        Assert.True(server.IsIpAllowed("26.10.0.5"));
        Assert.False(server.IsIpAllowed("26.10.0.6"));
    }

    [Fact]
    public async Task WithoutInvitedFriendsEveryoneIsLetIn()
    {
        var server = FriendsServer();
        using var broadcast = new HostBroadcast(server) { InitializeHost = (_, _) => { } };

        await broadcast.StartAsync(With());

        Assert.False(broadcast.IsPrivateLive);
        Assert.True(server.IsIpAllowed("26.10.0.5"));
        Assert.True(server.IsIpAllowed("26.10.0.6"));
    }
}
