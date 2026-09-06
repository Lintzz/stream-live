using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

public class SignalingMessageTests
{
    [Fact]
    public void SerializeThenDeserializeKeepsEveryField()
    {
        var original = new SignalingMessage { Type = "offer", Data = "{\"sdp\":\"x\"}", SenderId = "abc" };

        var back = SignalingMessage.Deserialize(SignalingMessage.Serialize(original));

        Assert.NotNull(back);
        Assert.Equal(original.Type, back!.Type);
        Assert.Equal(original.Data, back.Data);
        Assert.Equal(original.SenderId, back.SenderId);
    }

    /// <summary>
    /// Estes dados vêm da rede: entrada inválida devolve null em vez de estourar exceção
    /// dentro do handler do WebSocket.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("não é json")]
    [InlineData("{ isto: está, quebrado")]
    [InlineData("[1,2,3]")]
    public void InvalidJsonBecomesNull(string json)
    {
        Assert.Null(SignalingMessage.Deserialize(json));
    }

    [Fact]
    public void MissingFieldsAreNullNotEmpty()
    {
        var msg = SignalingMessage.Deserialize("{}");

        Assert.NotNull(msg);
        Assert.Null(msg!.Type);
        Assert.Null(msg.Data);
    }
}

public class NormalizeIpTests
{
    /// <summary>
    /// O Fleck entrega IPv4 mapeado em IPv6 e loopback como "::1". Sem normalizar, o IP
    /// nunca casa com o que o usuário salvou na lista de amigos — e a allowlist, que é a
    /// proteção principal do app, recusaria justamente quem deveria entrar.
    /// </summary>
    [Theory]
    [InlineData("::1", "127.0.0.1")]
    [InlineData("::ffff:26.10.0.5", "26.10.0.5")]
    [InlineData("::FFFF:192.168.1.7", "192.168.1.7")]
    [InlineData("26.10.0.5", "26.10.0.5")]
    [InlineData("  26.10.0.5  ", "26.10.0.5")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void NormalizesTheFormsFleckActuallyProduces(string? input, string expected)
    {
        Assert.Equal(expected, SignalingServer.NormalizeIp(input));
    }

    [Fact]
    public void LeavesRealIpv6Alone()
    {
        // Não é IPv4 mapeado: não há o que extrair, então fica como veio.
        Assert.Equal("::ffff:algo", SignalingServer.NormalizeIp("::ffff:algo"));
    }
}

public class SignalingServerLifecycleTests
{
    [Fact]
    public void SecondServerOnTheSamePortFailsInsteadOfThrowing()
    {
        var first = new SignalingServer();
        int port = FreePort();

        Assert.True(first.Start("127.0.0.1", port));
        try
        {
            // É como o app descobre que há outra instância aberta e desabilita o botão de
            // transmitir, em vez de deixar a falha passar despercebida.
            var second = new SignalingServer();
            Assert.False(second.Start("127.0.0.1", port));
            Assert.False(second.IsRunning);
        }
        finally
        {
            first.Stop();
        }
    }

    [Fact]
    public void StopResetsStateSoARestartDoesNotInheritGhosts()
    {
        var server = new SignalingServer();
        int port = FreePort();

        Assert.True(server.Start("127.0.0.1", port));
        server.IsStreaming = true;
        server.Stop();

        Assert.False(server.IsRunning);
        Assert.False(server.IsStreaming);
        Assert.Equal(0, server.ConnectedClientsCount);
        Assert.False(server.HasBroadcastTargets);

        // E a porta tem de estar livre de novo.
        Assert.True(server.Start("127.0.0.1", port));
        server.Stop();
    }

    [Fact]
    public void LocalhostIsAlwaysAllowedEvenBeforeTheFriendListArrives()
    {
        var server = new SignalingServer { RestrictToAllowedIps = true };
        int port = FreePort();

        Assert.True(server.Start("127.0.0.1", port));
        try
        {
            // É assim que o app consulta o próprio status; sem esta exceção uma ordem de
            // inicialização diferente trancaria o usuário para fora de tudo.
            server.SetAllowedIps(new[] { "26.10.0.9" });
            Assert.Equal(0, server.ConnectedClientsCount);
        }
        finally
        {
            server.Stop();
        }
    }

    [Fact]
    public void PrivateLiveHidesTheStreamFromFriendsWhoWereNotInvited()
    {
        var friends = Ips("26.10.0.9", "26.10.0.10");
        var invited = Ips("26.10.0.9");

        // O convidado entra e vê a live...
        Assert.True(SignalingServer.ShouldAcceptConnection("26.10.0.9", true, friends, true, invited));

        // ...e o amigo não convidado nem abre conexão. É a recusa que o faz ver o host como
        // offline, em vez de "online, sem transmitir".
        Assert.False(SignalingServer.ShouldAcceptConnection("26.10.0.10", true, friends, true, invited));
    }

    [Fact]
    public void WithoutAPrivateLiveEveryFriendKeepsSeeingTheStream()
    {
        var friends = Ips("26.10.0.9", "26.10.0.10");

        Assert.True(SignalingServer.ShouldAcceptConnection("26.10.0.9", true, friends, false, Ips()));
        Assert.True(SignalingServer.ShouldAcceptConnection("26.10.0.10", true, friends, false, Ips()));
    }

    /// <summary>
    /// Os dois portões são independentes: ser convidado não fura a lista de amigos, e ter a
    /// restrição a amigos desligada não faz a live privada vazar.
    /// </summary>
    [Fact]
    public void TheTwoGatesDoNotLoosenEachOther()
    {
        Assert.False(SignalingServer.ShouldAcceptConnection(
            "26.10.0.11", true, Ips("26.10.0.9"), true, Ips("26.10.0.11")));

        Assert.False(SignalingServer.ShouldAcceptConnection(
            "26.10.0.11", false, Ips(), true, Ips("26.10.0.9")));
    }

    [Fact]
    public void LocalhostPassesBothGatesEvenWithNobodyInvited()
    {
        // É assim que o app consulta o próprio status — e é o que mantém os testes de
        // handshake e de status funcionando em loopback.
        Assert.True(SignalingServer.ShouldAcceptConnection("127.0.0.1", true, Ips(), true, Ips()));
    }

    [Fact]
    public void EndingAPrivateLiveMakesTheHostVisibleAgain()
    {
        var server = new SignalingServer { RestrictToAllowedIps = true };
        server.SetAllowedIps(new[] { "26.10.0.10" });

        server.SetLiveVisibility(true, new[] { "26.10.0.9" });
        Assert.False(server.IsIpAllowed("26.10.0.10"));

        // O caminho do AnnounceStop: sem isto o host continuaria invisível depois de parar.
        server.SetLiveVisibility(false, null);
        Assert.True(server.IsIpAllowed("26.10.0.10"));
    }

    private static IReadOnlySet<string> Ips(params string[] ips)
        => new HashSet<string>(ips, StringComparer.OrdinalIgnoreCase);

    private static int FreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
