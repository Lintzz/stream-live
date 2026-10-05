using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using StreamLiveApp.Services;

namespace StreamLiveApp
{
    /// <summary>Estado do Radmin como a aba de configurações mostra.</summary>
    public enum RadminState
    {
        Checking,
        NotInstalled,
        ServiceStopped,
        Off,
        On,
        Connected,
        Error
    }

    /// <param name="Address">IP 26.x do adaptador, quando o Radmin está ligado.</param>
    /// <param name="Network">A rede pedida, quando já se sabe que você está nela.</param>
    public sealed record RadminStatus(RadminState State, string? Address = null, string? Network = null);

    public sealed record RadminActionResult(bool Success, string Message, RadminStatus? Status = null);

    /// <summary>O que a UI usa do Radmin. Existe para o modo demonstração trocar por um falso.</summary>
    public interface IRadminController
    {
        Task<RadminStatus> GetStatusAsync(string? targetNetwork, CancellationToken ct);
        Task<RadminActionResult> ConnectAsync(string networkName, string password, CancellationToken ct);
        Task<RadminActionResult> DisconnectAsync(CancellationToken ct);
        Task<RadminActionResult> StartServiceAsync(CancellationToken ct);

        /// <summary>Limpeza do fechamento. Bloqueia no máximo <paramref name="budget"/>.</summary>
        void CleanupOnExit(bool disconnect, bool closeProcess, TimeSpan budget);
    }

    /// <summary>
    /// Entra numa rede do Radmin VPN pela tela do Stream Live. Detecta instalação, serviço e
    /// adaptador por conta própria; a conversa com a janela do Radmin fica toda no
    /// <see cref="RadminUiAutomation"/>.
    ///
    /// Regra do fechamento: só desfaz o que ESTE app fez nesta sessão. Se o Radmin já estava
    /// aberto, ou você já estava conectado antes de abrir o Stream Live, nada disso é tocado
    /// ao sair — derrubar a VPN que o usuário já usava (num jogo, por exemplo) seria pior do
    /// que deixar o Radmin aberto.
    ///
    /// Nada aqui sai da máquina, e nem a senha nem o nome da rede vão para o log: o
    /// diagnóstico é anexado em pedido de ajuda.
    /// </summary>
    public sealed class RadminController : IRadminController
    {
        /// <summary>
        /// Serviço do Radmin VPN 2.x. Ele sobe com o Windows e segura a VPN; a janela
        /// (RvRvpnGui) é só a frente — fechar a janela não desconecta.
        /// </summary>
        internal const string ServiceName = "RvControlSvc";

        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(45);
        private static readonly TimeSpan JoinAnswerTimeout = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan AdapterUpTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan ServiceStartTimeout = TimeSpan.FromSeconds(20);

        // A automação de UI não pode correr duas vezes ao mesmo tempo: dois cliques em
        // Conectar abririam dois menus no Radmin.
        private readonly SemaphoreSlim _uiLock = new(1, 1);

        private bool _connectedByApp;
        private bool _processStartedByApp;

        public async Task<RadminStatus> GetStatusAsync(string? targetNetwork, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                var exe = VpnStatusService.FindExecutable();
                var service = QueryService();
                var address = FindRadminAddress();

                // Só olha a janela se ela já está na tela: na bandeja, trazê-la só para ler a
                // lista faria o Radmin piscar toda vez que o app abre.
                RadminUiState? ui = null;
                if (exe != null && service == ServiceControllerStatus.Running)
                {
                    try { ui = new RadminUiAutomation(exe).TryReadStateWithoutShowing(); }
                    catch (Exception ex) { DiagnosticLog.Warn("Radmin", $"Leitura da janela falhou ({ex.GetType().Name})"); }
                }

                var state = DecideState(exe != null, service, ui?.PowerOn, address != null,
                    ui != null && !string.IsNullOrWhiteSpace(targetNetwork) && ContainsNetwork(ui.Networks, targetNetwork));
                return new RadminStatus(state, address, state == RadminState.Connected ? targetNetwork?.Trim() : null);
            }, ct).ConfigureAwait(false);
        }

        public async Task<RadminActionResult> ConnectAsync(string networkName, string password, CancellationToken ct)
        {
            networkName = networkName.Trim();
            if (networkName.Length == 0) return Fail("Digite o nome da rede.");
            if (password.Length == 0) return Fail("Digite a senha da rede.");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(ConnectTimeout);

            await _uiLock.WaitAsync(timeout.Token).ConfigureAwait(false);
            try
            {
                return await Task.Run(() => Connect(networkName, password, timeout.Token), timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                DiagnosticLog.Warn("Radmin", "Conectar estourou o tempo");
                return Fail(DescribeJoinFailure(new RadminJoinResult(RadminJoinOutcome.TimedOut)));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DiagnosticLog.Error("Radmin", "Conectar falhou", ex);
                return Fail(DescribeJoinFailure(new RadminJoinResult(RadminJoinOutcome.UiNotFound)));
            }
            finally
            {
                _uiLock.Release();
            }
        }

        private RadminActionResult Connect(string networkName, string password, CancellationToken ct)
        {
            var exe = VpnStatusService.FindExecutable();
            var service = QueryService();
            if (exe == null || service == null) return Fail(DescribeState(RadminState.NotInstalled));
            if (service != ServiceControllerStatus.Running) return Fail(DescribeState(RadminState.ServiceStopped));
            if (!NetworkInterface.GetIsNetworkAvailable())
                return Fail("Sem conexão com a internet. Confira a rede do computador e tente de novo.");

            bool hadGui = RadminUiAutomation.GuiProcessIds().Length > 0;
            var ui = new RadminUiAutomation(exe);

            using var window = ui.Open(ct);
            NoteIfWeStartedTheGui(hadGui);
            if (window == null) return Fail(DescribeJoinFailure(new RadminJoinResult(RadminJoinOutcome.UiNotFound)));

            var state = ui.ReadState(window);
            switch (DecideConnectStep(state, networkName))
            {
                case ConnectStep.AlreadyConnected:
                    // Já estava nela antes: não é conexão "deste app", então o fechamento não a desfaz.
                    DiagnosticLog.Info("Radmin", "Já estava na rede pedida; nada a fazer");
                    return Ok("Você já está nessa rede.", networkName);

                case ConnectStep.PowerOnThenRecheck:
                    if (!ui.SetPower(window, true)) return Fail(DescribeJoinFailure(new RadminJoinResult(RadminJoinOutcome.UiNotFound)));
                    _connectedByApp = true;
                    WaitForAdapter(ct);
                    if (ContainsNetwork(ui.ReadState(window).Networks, networkName))
                        return Ok("Radmin ligado. Você já fazia parte dessa rede.", networkName);
                    break;
            }

            var result = ui.Join(window, networkName, password, JoinAnswerTimeout, ct);
            DiagnosticLog.Info("Radmin", $"Entrar na rede: {result.Outcome}" +
                (result.RadminMessage != null ? $" (Radmin: \"{result.RadminMessage}\")" : "") +
                (result.FailedStep != null ? $" (parou em: {result.FailedStep})" : ""));

            if (result.Outcome != RadminJoinOutcome.Joined) return Fail(DescribeJoinFailure(result));

            _connectedByApp = true;
            return Ok("Conectado à rede.", networkName);
        }

        public async Task<RadminActionResult> DisconnectAsync(CancellationToken ct)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));

            await _uiLock.WaitAsync(timeout.Token).ConfigureAwait(false);
            try
            {
                return await Task.Run(() =>
                {
                    var exe = VpnStatusService.FindExecutable();
                    if (exe == null) return Fail(DescribeState(RadminState.NotInstalled));

                    bool hadGui = RadminUiAutomation.GuiProcessIds().Length > 0;
                    var ui = new RadminUiAutomation(exe);
                    bool done;
                    using (var window = ui.Open(timeout.Token))
                    {
                        NoteIfWeStartedTheGui(hadGui);
                        done = window != null && ui.SetPower(window, false);
                    }
                    if (!done)
                        return Fail("Não foi possível desligar pelo app. Desligue no próprio Radmin (botão de energia ao lado do seu nome).");

                    _connectedByApp = false;
                    DiagnosticLog.Info("Radmin", "Desligado pelo app");
                    return new RadminActionResult(true, "Radmin desligado. As redes continuam salvas nele.", new RadminStatus(RadminState.Off));
                }, timeout.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                DiagnosticLog.Error("Radmin", "Desligar falhou", ex);
                return Fail("Não foi possível desligar pelo app. Desligue no próprio Radmin.");
            }
            finally
            {
                _uiLock.Release();
            }
        }

        public async Task<RadminActionResult> StartServiceAsync(CancellationToken ct)
        {
            try
            {
                // O app não roda como administrador (e não deve: o UAC apareceria em toda
                // abertura). Iniciar serviço exige admin, então só esta ação pede elevação, e
                // só quando o usuário clica.
                using (var sc = Process.Start(new ProcessStartInfo("sc.exe", $"start {ServiceName}")
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                }))
                {
                    if (sc != null) await sc.WaitForExitAsync(ct).ConfigureAwait(false);
                }

                var running = await Task.Run(() =>
                {
                    using var service = new ServiceController(ServiceName);
                    service.WaitForStatus(ServiceControllerStatus.Running, ServiceStartTimeout);
                    return true;
                }, ct).ConfigureAwait(false);

                DiagnosticLog.Info("Radmin", "Serviço iniciado pelo app");
                return new RadminActionResult(running, "Serviço da Radmin VPN iniciado.");
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // 1223 = ERROR_CANCELLED: o usuário disse "Não" no UAC.
                return Fail("Sem a permissão de administrador o serviço não pode ser iniciado. Clique de novo e aceite o aviso do Windows.");
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                return Fail("O serviço da Radmin VPN não subiu. Reinicie o computador ou reinstale o Radmin.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DiagnosticLog.Error("Radmin", "Iniciar o serviço falhou", ex);
                return Fail("Não foi possível iniciar o serviço da Radmin VPN. Reinicie o computador ou reinstale o Radmin.");
            }
        }

        public void CleanupOnExit(bool disconnect, bool closeProcess, TimeSpan budget)
        {
            var (doDisconnect, doClose) = DecideExitCleanup(disconnect, closeProcess, _connectedByApp, _processStartedByApp);
            if (!doDisconnect && !doClose) return;

            // Corre fora da thread de UI e com teto: o fechamento do app não pode ficar refém de
            // um Radmin travado. Estourou o tempo, o app fecha mesmo assim.
            var work = Task.Run(() =>
            {
                var exe = VpnStatusService.FindExecutable();
                if (exe == null) return;

                if (doDisconnect)
                {
                    try
                    {
                        var ui = new RadminUiAutomation(exe);
                        using var window = ui.Open(CancellationToken.None);
                        if (window != null) ui.SetPower(window, false);
                    }
                    catch (Exception ex) { DiagnosticLog.Warn("Radmin", $"Desligar ao fechar falhou ({ex.GetType().Name})"); }
                }

                if (doClose)
                {
                    foreach (var pid in RadminUiAutomation.GuiProcessIds())
                    {
                        try
                        {
                            if (new RadminUiAutomation(exe).ExitGracefully(pid)) continue;
                            using var process = Process.GetProcessById(pid);
                            process.Kill();
                        }
                        catch (Exception ex) { DiagnosticLog.Warn("Radmin", $"Fechar o Radmin falhou ({ex.GetType().Name})"); }
                    }
                }
            });

            try
            {
                if (!work.Wait(budget)) DiagnosticLog.Warn("Radmin", "Limpeza do Radmin estourou o tempo; o app fechou sem ela");
            }
            catch (Exception ex)
            {
                DiagnosticLog.Warn("Radmin", $"Limpeza do Radmin falhou ({ex.GetType().Name})");
            }
        }

        // ---------------------------------------------------------------- detecção

        private void NoteIfWeStartedTheGui(bool hadGuiBefore)
        {
            if (!hadGuiBefore && RadminUiAutomation.GuiProcessIds().Length > 0) _processStartedByApp = true;
        }

        /// <summary>Status do serviço, ou null se ele não existe (Radmin não instalado).</summary>
        private static ServiceControllerStatus? QueryService()
        {
            try
            {
                using var service = new ServiceController(ServiceName);
                return service.Status;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static void WaitForAdapter(CancellationToken ct)
        {
            var deadline = DateTime.UtcNow + AdapterUpTimeout;
            while (DateTime.UtcNow < deadline && FindRadminAddress() == null)
            {
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(500);
            }
        }

        /// <summary>IP 26.x do adaptador do Radmin, se ele estiver ativo.</summary>
        internal static string? FindRadminAddress()
        {
            try
            {
                return PickRadminAddress(NetworkInterface.GetAllNetworkInterfaces().Select(nic => new AdapterInfo(
                    nic.Description,
                    nic.OperationalStatus == OperationalStatus.Up,
                    nic.GetIPProperties().UnicastAddresses.Select(a => a.Address).ToList())));
            }
            catch (NetworkInformationException)
            {
                return null;
            }
        }

        internal sealed record AdapterInfo(string Description, bool IsUp, IReadOnlyList<IPAddress> Addresses);

        // ---------------------------------------------------------------- regras puras

        /// <summary>
        /// O adaptador virtual é "Famatech Radmin VPN Ethernet Adapter". O IP 26.x é da conta,
        /// não da rede: ele prova que o Radmin está ligado, não que você está numa rede
        /// específica — isso só a lista de redes da janela diz.
        /// </summary>
        internal static string? PickRadminAddress(IEnumerable<AdapterInfo> adapters)
        {
            foreach (var adapter in adapters)
            {
                if (!adapter.IsUp || !adapter.Description.Contains("Radmin", StringComparison.OrdinalIgnoreCase)) continue;
                var ip = adapter.Addresses.FirstOrDefault(a =>
                    a.AddressFamily == AddressFamily.InterNetwork && a.GetAddressBytes()[0] == 26);
                if (ip != null) return ip.ToString();
            }
            return null;
        }

        internal static RadminState DecideState(bool installed, ServiceControllerStatus? service, bool? powerOn,
            bool adapterUp, bool inTargetNetwork)
        {
            if (!installed || service == null) return RadminState.NotInstalled;
            if (service != ServiceControllerStatus.Running) return RadminState.ServiceStopped;
            if (inTargetNetwork && powerOn != false) return RadminState.Connected;
            if (powerOn == false) return RadminState.Off;
            // Janela escondida (powerOn desconhecido): o adaptador decide.
            return adapterUp || powerOn == true ? RadminState.On : RadminState.Off;
        }

        internal enum ConnectStep { AlreadyConnected, PowerOnThenRecheck, Join }

        internal static ConnectStep DecideConnectStep(RadminUiState state, string networkName)
        {
            if (!state.PowerOn) return ConnectStep.PowerOnThenRecheck;
            return ContainsNetwork(state.Networks, networkName) ? ConnectStep.AlreadyConnected : ConnectStep.Join;
        }

        internal static bool ContainsNetwork(IEnumerable<string> networks, string networkName)
            => networks.Any(n => string.Equals(n.Trim(), networkName.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <summary>Só desfaz o que este app fez: ver o resumo da classe.</summary>
        internal static (bool Disconnect, bool Close) DecideExitCleanup(bool optionDisconnect, bool optionClose,
            bool connectedByApp, bool processStartedByApp)
            => (optionDisconnect && connectedByApp, optionClose && processStartedByApp);

        internal static string DescribeState(RadminState state) => state switch
        {
            RadminState.NotInstalled => "Radmin VPN não encontrado. Instale pelo site oficial (radmin-vpn.com) e abra o Stream Live de novo.",
            RadminState.ServiceStopped => "O serviço da Radmin VPN está parado. Clique em Iniciar serviço — o Windows vai pedir permissão de administrador.",
            RadminState.Off => "Radmin desligado.",
            RadminState.On => "Radmin ligado.",
            RadminState.Connected => "Conectado.",
            RadminState.Checking => "Verificando o Radmin…",
            _ => "Não foi possível verificar o Radmin."
        };

        /// <summary>Mensagem para a tela: o que aconteceu e o que fazer, sem texto de exceção.</summary>
        internal static string DescribeJoinFailure(RadminJoinResult result) => result.Outcome switch
        {
            RadminJoinOutcome.InvalidCredentials => "Nome da rede ou senha incorretos. Confira com quem criou a rede (o Radmin não diz qual dos dois está errado).",
            RadminJoinOutcome.ServerUnreachable => "O Radmin não conseguiu falar com o servidor dele. Confira sua internet e tente de novo.",
            RadminJoinOutcome.NetworkClosed => "Essa rede está fechada para novos membros. Peça a quem criou a rede para abri-la.",
            RadminJoinOutcome.ServiceNotRunning => DescribeState(RadminState.ServiceStopped),
            RadminJoinOutcome.RejectedOther => $"O Radmin recusou a entrada: \"{result.RadminMessage}\".",
            RadminJoinOutcome.TimedOut => "O Radmin não respondeu a tempo. Tente de novo; se continuar, entre na rede uma vez pelo próprio Radmin (Rede > Conectar à rede).",
            _ => "Não foi possível controlar a janela do Radmin. Entre na rede uma vez pelo próprio Radmin (Rede > Conectar à rede) — depois disso o app só confere a conexão."
        };

        private static RadminActionResult Ok(string message, string network)
            => new(true, message, new RadminStatus(RadminState.Connected, FindRadminAddress(), network));

        private static RadminActionResult Fail(string message) => new(false, message);
    }

    /// <summary>
    /// Radmin de mentira para o <c>--demo</c>: os prints não podem depender (nem mexer) no
    /// Radmin de quem grava.
    /// </summary>
    public sealed class DemoRadminController : IRadminController
    {
        private bool _connected = true;

        public Task<RadminStatus> GetStatusAsync(string? targetNetwork, CancellationToken ct)
            => Task.FromResult(_connected
                ? new RadminStatus(RadminState.Connected, "26.10.0.10", string.IsNullOrWhiteSpace(targetNetwork) ? "amigos-da-live" : targetNetwork)
                : new RadminStatus(RadminState.Off));

        public async Task<RadminActionResult> ConnectAsync(string networkName, string password, CancellationToken ct)
        {
            await Task.Delay(1200, ct);
            _connected = true;
            return new RadminActionResult(true, "Conectado à rede.", new RadminStatus(RadminState.Connected, "26.10.0.10", networkName.Trim()));
        }

        public Task<RadminActionResult> DisconnectAsync(CancellationToken ct)
        {
            _connected = false;
            return Task.FromResult(new RadminActionResult(true, "Radmin desligado. As redes continuam salvas nele.", new RadminStatus(RadminState.Off)));
        }

        public Task<RadminActionResult> StartServiceAsync(CancellationToken ct)
            => Task.FromResult(new RadminActionResult(true, "Serviço da Radmin VPN iniciado."));

        public void CleanupOnExit(bool disconnect, bool closeProcess, TimeSpan budget) { }
    }
}
