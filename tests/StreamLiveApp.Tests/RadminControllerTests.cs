using System.IO;
using System.Net;
using System.ServiceProcess;
using System.Text;
using Xunit;

namespace StreamLiveApp.Tests;

public class RadminControllerTests
{
    private static RadminController.AdapterInfo Adapter(string description, bool up, params string[] ips)
        => new(description, up, ips.Select(IPAddress.Parse).ToList());

    [Fact]
    public void PickRadminAddress_ReturnsThe26AddressOfTheRadminAdapter()
    {
        var adapters = new[]
        {
            Adapter("Realtek PCIe GbE", true, "192.168.15.14"),
            Adapter("Famatech Radmin VPN Ethernet Adapter", true, "fdfd::1a1e:9036", "26.30.144.54"),
        };

        Assert.Equal("26.30.144.54", RadminController.PickRadminAddress(adapters));
    }

    [Fact]
    public void PickRadminAddress_IgnoresAdapterThatIsDown()
    {
        var adapters = new[] { Adapter("Famatech Radmin VPN Ethernet Adapter", false, "26.30.144.54") };

        Assert.Null(RadminController.PickRadminAddress(adapters));
    }

    [Fact]
    public void PickRadminAddress_Ignores26AddressOnAnotherAdapter()
    {
        // 26.x numa placa que não é a do Radmin não prova nada sobre o Radmin.
        var adapters = new[] { Adapter("Hamachi", true, "26.1.2.3") };

        Assert.Null(RadminController.PickRadminAddress(adapters));
    }

    [Theory]
    [InlineData(false, ServiceControllerStatus.Running, RadminState.NotInstalled)]
    [InlineData(true, ServiceControllerStatus.Stopped, RadminState.ServiceStopped)]
    [InlineData(true, ServiceControllerStatus.StartPending, RadminState.ServiceStopped)]
    public void DecideState_InstallAndServiceComeFirst(bool installed, ServiceControllerStatus service, RadminState expected)
    {
        Assert.Equal(expected, RadminController.DecideState(installed, service, powerOn: true, adapterUp: true, inTargetNetwork: true));
    }

    [Fact]
    public void DecideState_MissingServiceMeansNotInstalled()
    {
        Assert.Equal(RadminState.NotInstalled, RadminController.DecideState(true, null, null, false, false));
    }

    [Fact]
    public void DecideState_PowerOffWinsOverAdapter()
    {
        Assert.Equal(RadminState.Off, RadminController.DecideState(true, ServiceControllerStatus.Running, powerOn: false, adapterUp: true, inTargetNetwork: true));
    }

    [Fact]
    public void DecideState_HiddenWindowFallsBackToAdapter()
    {
        // Radmin na bandeja: o botão de energia não é legível, e só o adaptador diz algo.
        Assert.Equal(RadminState.On, RadminController.DecideState(true, ServiceControllerStatus.Running, null, adapterUp: true, false));
        Assert.Equal(RadminState.Off, RadminController.DecideState(true, ServiceControllerStatus.Running, null, adapterUp: false, false));
    }

    [Fact]
    public void DecideState_ConnectedOnlyWhenTheNetworkIsInTheList()
    {
        Assert.Equal(RadminState.Connected, RadminController.DecideState(true, ServiceControllerStatus.Running, true, true, inTargetNetwork: true));
        Assert.Equal(RadminState.On, RadminController.DecideState(true, ServiceControllerStatus.Running, true, true, inTargetNetwork: false));
    }

    [Fact]
    public void DecideConnectStep_SkipsTheJoinWhenAlreadyInTheNetwork()
    {
        var state = new RadminUiState(true, new[] { "EXBAROES" });

        Assert.Equal(RadminController.ConnectStep.AlreadyConnected, RadminController.DecideConnectStep(state, "  exbaroes "));
        Assert.Equal(RadminController.ConnectStep.Join, RadminController.DecideConnectStep(state, "outra-rede"));
    }

    [Fact]
    public void DecideConnectStep_TurnsOnFirstWhenOff()
    {
        var state = new RadminUiState(false, new[] { "EXBAROES" });

        Assert.Equal(RadminController.ConnectStep.PowerOnThenRecheck, RadminController.DecideConnectStep(state, "EXBAROES"));
    }

    [Theory]
    // Só desfaz o que o app fez: o Radmin que o usuário já usava fica como estava.
    [InlineData(true, true, false, false, false, false)]
    [InlineData(true, true, true, false, true, false)]
    [InlineData(true, true, false, true, false, true)]
    [InlineData(false, false, true, true, false, false)]
    [InlineData(true, true, true, true, true, true)]
    public void DecideExitCleanup_OnlyUndoesWhatTheAppDid(bool optDisconnect, bool optClose, bool connectedByApp,
        bool startedByApp, bool expectDisconnect, bool expectClose)
    {
        var (disconnect, close) = RadminController.DecideExitCleanup(optDisconnect, optClose, connectedByApp, startedByApp);

        Assert.Equal(expectDisconnect, disconnect);
        Assert.Equal(expectClose, close);
    }

    [Fact]
    public async Task ConnectAsync_RejectsEmptyFieldsWithoutTouchingRadmin()
    {
        var controller = new RadminController();

        var noName = await controller.ConnectAsync("   ", "x", default);
        var noPassword = await controller.ConnectAsync("rede", "", default);

        Assert.False(noName.Success);
        Assert.Contains("nome da rede", noName.Message);
        Assert.False(noPassword.Success);
        Assert.Contains("senha", noPassword.Message);
    }

    [Fact]
    public void FailureMessages_TellWhatToDoAndNeverEchoThePassword()
    {
        foreach (var outcome in Enum.GetValues<RadminJoinOutcome>().Where(o => o != RadminJoinOutcome.Joined))
        {
            var message = RadminController.DescribeJoinFailure(new RadminJoinResult(outcome, "texto do radmin", "passo"));
            Assert.False(string.IsNullOrWhiteSpace(message));
            Assert.DoesNotContain("Exception", message);
        }
    }
}

public class RadminUiAutomationTests
{
    [Theory]
    // Textos reais do tooltip do Radmin 2.1 (pt_BR e inglês). O Radmin dá a mesma mensagem
    // para rede inexistente e senha errada — medido com uma rede de nome aleatório.
    [InlineData("Nome de rede ou senha inválidos", RadminJoinOutcome.InvalidCredentials)]
    [InlineData("Invalid network name or password", RadminJoinOutcome.InvalidCredentials)]
    [InlineData("Não é possível se conectar ao servidor ROL", RadminJoinOutcome.ServerUnreachable)]
    [InlineData("Cannot connect to ROL server", RadminJoinOutcome.ServerUnreachable)]
    [InlineData("A rede está fechada para novos membros, é impossível entrar", RadminJoinOutcome.NetworkClosed)]
    [InlineData("Serviço não iniciado", RadminJoinOutcome.ServiceNotRunning)]
    [InlineData("Service not started", RadminJoinOutcome.ServiceNotRunning)]
    [InlineData("Não foi possível conectar-se à rede.", RadminJoinOutcome.RejectedOther)]
    public void ClassifyRadminMessage_MapsKnownTooltips(string message, RadminJoinOutcome expected)
    {
        Assert.Equal(expected, RadminUiAutomation.ClassifyRadminMessage(message));
    }

    [Fact]
    public void PickByNameOrIndex_PrefersKnownNameInEitherLanguage()
    {
        Assert.Equal(1, RadminUiAutomation.PickByNameOrIndex(new[] { "Criar rede", "Conectar à rede" }, RadminUiAutomation.JoinMenuItemNames, 0));
        Assert.Equal(1, RadminUiAutomation.PickByNameOrIndex(new[] { "Create network", "Join network" }, RadminUiAutomation.JoinMenuItemNames, 0));
        Assert.Equal(1, RadminUiAutomation.PickByNameOrIndex(new[] { "&System", "&Network", "&Help" }, RadminUiAutomation.NetworkMenuNames, 0));
    }

    [Fact]
    public void PickByNameOrIndex_FallsBackToPositionForOtherLanguages()
    {
        Assert.Equal(1, RadminUiAutomation.PickByNameOrIndex(new[] { "Système", "Réseau", "Aide" }, RadminUiAutomation.NetworkMenuNames, RadminUiAutomation.NetworkMenuIndex));
        Assert.Equal(-1, RadminUiAutomation.PickByNameOrIndex(new[] { "Système" }, RadminUiAutomation.ExitMenuItemNames, null));
        Assert.Equal(-1, RadminUiAutomation.PickByNameOrIndex(new[] { "A" }, RadminUiAutomation.NetworkMenuNames, 5));
    }

    [Fact]
    public void Selectors_AreTheAutomationIdsMeasuredOnRadmin21()
    {
        // Trava contra edição distraída: estes ids foram lidos da árvore real do Radmin 2.1.1.
        Assert.Equal("MainWindow.DlgJoinNetwork.centralWidget.mainContentWidget.tabWidgetNetSelector.qt_tabwidget_stackedwidget.tab_private.LENetName",
            RadminUiAutomation.JoinNameFieldId);
        Assert.Equal("MainWindow.DlgJoinNetwork.centralWidget.mainContentWidget.joinPushButton", RadminUiAutomation.JoinButtonId);
        Assert.Equal("MainWindow.centralWidget.mainContentWidget.userInfoWidget.BPower", RadminUiAutomation.PowerToggleId);
    }
}

public class RadminCredentialStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"radmin_rede_{Guid.NewGuid():N}.dat");

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var store = new RadminCredentialStore(_path);

        Assert.True(store.Save(new RadminCredential("amigos-da-live", "s3nh@-çá")));
        var loaded = store.Load();

        Assert.Equal("amigos-da-live", loaded?.NetworkName);
        Assert.Equal("s3nh@-çá", loaded?.Password);
    }

    [Fact]
    public void SavedFile_DoesNotContainThePasswordInClear()
    {
        var store = new RadminCredentialStore(_path);
        store.Save(new RadminCredential("amigos-da-live", "senha-bem-visivel"));

        var bytes = File.ReadAllBytes(_path);
        foreach (var encoding in new Encoding[] { Encoding.UTF8, Encoding.Unicode })
        {
            var text = encoding.GetString(bytes);
            Assert.DoesNotContain("senha-bem-visivel", text);
            Assert.DoesNotContain("amigos-da-live", text);
        }
    }

    [Fact]
    public void Forget_RemovesTheFile()
    {
        var store = new RadminCredentialStore(_path);
        store.Save(new RadminCredential("rede", "senha"));

        store.Forget();

        Assert.False(store.HasSaved);
        Assert.Null(store.Load());
    }

    [Fact]
    public void Load_ReturnsNullForCorruptFile()
    {
        // Arquivo vindo de outro perfil do Windows: o DPAPI não abre e o app só pede a rede de novo.
        File.WriteAllBytes(_path, new byte[] { 1, 2, 3, 4 });

        Assert.Null(new RadminCredentialStore(_path).Load());
    }
}
