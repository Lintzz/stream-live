using StreamLiveApp.Services;
using Xunit;

namespace StreamLiveApp.Tests;

public class AudioExclusionServiceTests
{
    /// <summary>
    /// O ResolvePid aceita qualquer processo, com janela ou sem: o Discord pode estar
    /// rodando minimizado na bandeja, e nem por isso o áudio dele deve entrar na live.
    /// </summary>
    [Fact]
    public void ResolvePid_FindsAProcessEvenWithoutAWindow()
    {
        var self = System.Diagnostics.Process.GetCurrentProcess();

        Assert.Equal(IntPtr.Zero, self.MainWindowHandle);
        Assert.NotEqual(0u, AudioExclusionService.ResolvePid(self.ProcessName));
    }

    [Fact]
    public void ResolvePid_ReturnsZeroForNoSelection()
    {
        Assert.Equal(0u, AudioExclusionService.ResolvePid(null));
        Assert.Equal(0u, AudioExclusionService.ResolvePid(string.Empty));
    }

    [Fact]
    public void ResolvePid_ReturnsZeroWhenProcessIsNotRunning()
    {
        // É este 0 que faz o app cair para "capturar tudo" quando o Discord está fechado.
        Assert.Equal(0u, AudioExclusionService.ResolvePid("ProgramaQueNaoExiste_XYZ"));
    }

    [Fact]
    public void ResolvePid_FindsARunningProcess()
    {
        var self = System.Diagnostics.Process.GetCurrentProcess();

        Assert.NotEqual(0u, AudioExclusionService.ResolvePid(self.ProcessName));
    }
}

public class FriendStatusServiceTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task BlankIpIsOffline(string? ip)
    {
        Assert.Equal(FriendStatus.Offline, await FriendStatusService.CheckAsync(ip!));
    }

    [Fact]
    public async Task NothingListeningIsOffline()
    {
        // Porta livre: ninguém atende, então não pode reportar online.
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        Assert.Equal(FriendStatus.Offline, await FriendStatusService.CheckAsync("127.0.0.1", port));
    }

    [Fact]
    public async Task ReportsOnlineAndIdleForAServerThatIsNotStreaming()
    {
        var server = new StreamLiveApp.SignalingServer { IsStreaming = false };
        int port = FreePort();
        Assert.True(server.Start("127.0.0.1", port));

        try
        {
            var status = await FriendStatusService.CheckAsync("127.0.0.1", port);

            Assert.True(status.IsOnline);
            Assert.False(status.IsStreaming);
        }
        finally
        {
            server.Stop();
        }
    }

    [Fact]
    public async Task ReportsStreamingForAServerThatIsLive()
    {
        var server = new StreamLiveApp.SignalingServer { IsStreaming = true };
        int port = FreePort();
        Assert.True(server.Start("127.0.0.1", port));

        try
        {
            var status = await FriendStatusService.CheckAsync("127.0.0.1", port);

            Assert.True(status.IsOnline);
            Assert.True(status.IsStreaming);
        }
        finally
        {
            server.Stop();
        }
    }

    /// <summary>
    /// Sala com senha continua respondendo o status: é o que mantém a bolinha do amigo
    /// verde sem que você precise saber a senha dele.
    /// </summary>
    [Fact]
    public async Task PasswordProtectedRoomStillReportsItsStatus()
    {
        var server = new StreamLiveApp.SignalingServer { IsStreaming = true, RoomPassword = "segredo" };
        int port = FreePort();
        Assert.True(server.Start("127.0.0.1", port));

        try
        {
            var status = await FriendStatusService.CheckAsync("127.0.0.1", port);

            Assert.True(status.IsOnline);
            Assert.True(status.IsStreaming);
        }
        finally
        {
            server.Stop();
        }
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

public class AppSettingsTests
{
    /// <summary>
    /// Os padrões de fábrica valem na primeira execução, quando não há settings.json.
    /// </summary>
    [Fact]
    public void FactoryDefaultsAreTheSafeOnes()
    {
        var settings = new AppSettings();

        Assert.True(settings.RestrictToFriends);
    }

    [Fact]
    public void SettingsSurviveARoundTripThroughJson()
    {
        var original = new AppSettings { RestrictToFriends = false };

        var json = System.Text.Json.JsonSerializer.Serialize(original);
        var back = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(back);
        Assert.Equal(original.RestrictToFriends, back!.RestrictToFriends);
    }

    /// <summary>
    /// Um settings.json gravado por uma versão anterior traz campos que já não existem (o
    /// modo leve, o GDI forçado, a exclusão de áudio por nome). Eles têm de ser ignorados sem
    /// erro, e o que sobrou tem de cair no padrão de fábrica, não em false — senão atualizar o
    /// app desligaria calado a restrição a amigos.
    /// </summary>
    [Fact]
    public void OlderSettingsFileKeepsTheSafeDefaultsForNewFields()
    {
        var json = """{"ExcludedAudioProcessName":"Discord","LightweightMode":false,"ForceGdiCapture":true}""";

        var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(settings);
        Assert.True(settings!.RestrictToFriends);
    }
}

public class VpnStatusServiceTests
{
    /// <summary>
    /// A ordem importa: o Radmin é 32 bits e mora no Program Files (x86); o segundo candidato
    /// só existe para máquinas fora do padrão.
    /// </summary>
    [Fact]
    public void PickExistingPath_ReturnsTheFirstCandidateThatExists()
    {
        var found = VpnStatusService.PickExistingPath(
            new[] { @"C:\a\RvRvpnGui.exe", @"C:\b\RvRvpnGui.exe" },
            p => p.StartsWith(@"C:\b"));

        Assert.Equal(@"C:\b\RvRvpnGui.exe", found);
    }

    /// <summary>
    /// Null é o sinal de "Radmin não instalado" — é ele que faz o modal esconder o botão de
    /// abrir e virar só um aviso.
    /// </summary>
    [Fact]
    public void PickExistingPath_ReturnsNullWhenNothingExists()
    {
        Assert.Null(VpnStatusService.PickExistingPath(
            new[] { @"C:\a\RvRvpnGui.exe", @"C:\b\RvRvpnGui.exe" }, _ => false));
    }

    /// <summary>
    /// GetFolderPath devolve string vazia quando a pasta especial não existe na máquina; o
    /// Path.Combine então entrega um caminho relativo que não deve nem ser consultado.
    /// </summary>
    [Fact]
    public void PickExistingPath_IgnoresEmptyCandidates()
    {
        var consulted = new System.Collections.Generic.List<string>();

        var found = VpnStatusService.PickExistingPath(
            new string?[] { null, "", "   ", @"C:\c\RvRvpnGui.exe" },
            p => { consulted.Add(p); return true; });

        Assert.Equal(@"C:\c\RvRvpnGui.exe", found);
        Assert.Equal(new[] { @"C:\c\RvRvpnGui.exe" }, consulted);
    }
}
