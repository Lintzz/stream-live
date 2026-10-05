using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using Condition = System.Windows.Automation.Condition;

namespace StreamLiveApp
{
    /// <summary>Como terminou uma tentativa de entrar numa rede pela janela do Radmin.</summary>
    public enum RadminJoinOutcome
    {
        Joined,
        InvalidCredentials,
        ServerUnreachable,
        NetworkClosed,
        ServiceNotRunning,
        RejectedOther,
        TimedOut,
        UiNotFound
    }

    /// <param name="FailedStep">Com <see cref="RadminJoinOutcome.UiNotFound"/>: o passo da automação que não achou o
/// controle. Vai para o diagnóstico — é o que diz qual constante ajustar quando o Radmin muda.</param>
public sealed record RadminJoinResult(RadminJoinOutcome Outcome, string? RadminMessage = null, string? FailedStep = null);

    /// <summary>O que a janela do Radmin mostra agora.</summary>
    public sealed record RadminUiState(bool PowerOn, IReadOnlyList<string> Networks);

    /// <summary>
    /// ÚNICA classe que toca a interface do Radmin VPN. O Radmin não tem linha de comando,
    /// protocolo de URL nem API para entrar numa rede (o executável só aceita /minimized), então
    /// a entrada é feita dirigindo a janela dele por UI Automation.
    ///
    /// A interface é Qt Widgets, e o Qt publica o caminho de objectName como AutomationId
    /// ("MainWindow.DlgJoinNetwork..."): esse id é o mesmo em qualquer idioma do Radmin, então
    /// é por ele que tudo é achado. Só os itens de menu (QAction) não têm id — esses vão por
    /// nome, em português e inglês, com a posição como último recurso.
    ///
    /// Se o Radmin mudar a interface numa atualização, o ajuste é nas constantes abaixo.
    /// Medido no Radmin 2.1.1 (2.1.4951.1) em 2026-10-05.
    ///
    /// Todos os métodos bloqueiam: chame de uma thread de fundo, nunca da de UI.
    /// </summary>
    internal sealed class RadminUiAutomation
    {
        internal const string GuiProcessName = "RvRvpnGui";

        // Janela principal e o que se lê nela.
        internal const string MainWindowId = "MainWindow";
        internal const string MainCloseButtonId = "MainWindow.centralWidget.titleWidget.closeToolButton";
        internal const string PowerToggleId = "MainWindow.centralWidget.mainContentWidget.userInfoWidget.BPower";
        internal const string NetworkNameLabelSuffix = ".CNetworkWidget.NetworkWidget.NetworkName";
        internal const string NetworkListId = "MainWindow.centralWidget.mainContentWidget.stackedWidget.pageNetworksTree";
        // Pelo id, nunca pelo tipo: o primeiro MenuBar da árvore é o menu de sistema do
        // Windows (o do ícone da janela), que em pt-BR também se chama "Sistema".
        internal const string MenuBarId = "MainWindow.centralWidget.mainContentWidget.menuWidget.menuBar";

        // Diálogo "Entrar na rede".
        internal const string JoinDialogId = "MainWindow.DlgJoinNetwork";
        private const string JoinPrivateTab = JoinDialogId + ".centralWidget.mainContentWidget.tabWidgetNetSelector.qt_tabwidget_stackedwidget.tab_private";
        internal const string JoinNameFieldId = JoinPrivateTab + ".LENetName";
        internal const string JoinPasswordFieldId = JoinPrivateTab + ".LEPassword";
        internal const string JoinTabBarId = JoinDialogId + ".centralWidget.mainContentWidget.tabWidgetNetSelector.qt_tabwidget_tabbar";
        internal const string JoinButtonId = JoinDialogId + ".centralWidget.mainContentWidget.joinPushButton";
        internal const string JoinCancelButtonId = JoinDialogId + ".centralWidget.mainContentWidget.cancelPushButton";

        // O erro de entrar (senha errada, rede inexistente, sem servidor) aparece como tooltip
        // perto do botão, por uns 8 s, com o diálogo continuando aberto.
        internal const string ErrorTooltipClass = "QTipLabel";

        // Menus por nome (o Qt tira o "&" de atalho do nome publicado). Índice = posição no
        // menu, usada se nenhum nome casar (outro idioma do Radmin).
        internal static readonly string[] NetworkMenuNames = { "Rede", "Network" };
        internal const int NetworkMenuIndex = 1;
        internal static readonly string[] JoinMenuItemNames = { "Conectar à rede", "Entrar na rede", "Join network", "Join Network" };
        internal const int JoinMenuItemIndex = 1;
        internal static readonly string[] SystemMenuNames = { "Sistema", "System" };
        internal const int SystemMenuIndex = 0;
        internal static readonly string[] ExitMenuItemNames = { "Sair", "Exit" };

        private static readonly TimeSpan WindowAppearTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan DialogAppearTimeout = TimeSpan.FromSeconds(4);

        private readonly string? _executablePath;

        public RadminUiAutomation(string? executablePath)
        {
            _executablePath = executablePath;
        }

        /// <summary>
        /// Estado da janela sem trazê-la de volta: com o Radmin escondido na bandeja a janela
        /// some da árvore de automação, e abri-la só para olhar piscaria a tela na abertura do
        /// app. Null quando ela não está acessível agora.
        /// </summary>
        public RadminUiState? TryReadStateWithoutShowing()
        {
            var main = FindMainWindow();
            return main == null ? null : ReadState(main);
        }

        /// <summary>
        /// Traz a janela principal (da bandeja, se for o caso) para uma sequência de operações.
        /// Uma sessão só por sequência: abrir e devolver a cada passo faria o Radmin piscar.
        /// Null quando a janela não apareceu.
        /// </summary>
        public RadminWindowSession? Open(CancellationToken ct) => OpenMainWindow(ct);

        public RadminUiState ReadState(RadminWindowSession window) => ReadState(window.Element);

        /// <summary>Liga ou desliga o Radmin pelo botão de energia dele. False se o botão não foi achado.</summary>
        public bool SetPower(RadminWindowSession window, bool on)
        {
            var toggle = FindById(window.Element, PowerToggleId);
            if (toggle == null || !toggle.TryGetCurrentPattern(TogglePattern.Pattern, out var pattern)) return false;

            var tp = (TogglePattern)pattern;
            if ((tp.Current.ToggleState == ToggleState.On) != on) tp.Toggle();
            return true;
        }

        /// <summary>
        /// Rede → Conectar à rede, preenche nome e senha, Entrar, e espera o resultado. A senha
        /// só passa pelo ValuePattern do campo do Radmin; nada aqui a grava nem a registra.
        /// </summary>
        public RadminJoinResult Join(RadminWindowSession window, string networkName, string password, TimeSpan timeout, CancellationToken ct)
        {
            AutomationElement? dialog = null;
            try
            {
                string menuStep = "diálogo Entrar na rede";
                dialog = FindById(window.Element, JoinDialogId) ?? OpenJoinDialog(window, ct, out menuStep);
                if (dialog == null) return NotFound(menuStep);
                MoveOffScreen(dialog);

                SelectPrivateTab(dialog);
                if (!SetValue(dialog, JoinNameFieldId, networkName)) return NotFound("campo do nome");
                if (!SetValue(dialog, JoinPasswordFieldId, password)) return NotFound("campo da senha");

                var join = FindById(dialog, JoinButtonId);
                if (join == null) return NotFound("botão Entrar");

                // O tooltip de uma tentativa anterior fica uns 8 s na tela; lido agora, ele
                // passaria por resposta desta tentativa.
                var staleDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
                while (FindErrorTooltip(window.ProcessId) != null && DateTime.UtcNow < staleDeadline)
                {
                    ct.ThrowIfCancellationRequested();
                    Thread.Sleep(250);
                }

                if (!Invoke(join)) return NotFound("clique em Entrar");

                var deadline = DateTime.UtcNow + timeout;
                while (DateTime.UtcNow < deadline)
                {
                    ct.ThrowIfCancellationRequested();
                    Thread.Sleep(250);

                    var tooltip = FindErrorTooltip(window.ProcessId);
                    if (tooltip != null) return new RadminJoinResult(ClassifyRadminMessage(tooltip), tooltip);

                    // O diálogo fecha sozinho quando a entrada é aceita.
                    if (FindById(window.Element, JoinDialogId) == null)
                    {
                        dialog = null;
                        return new RadminJoinResult(RadminJoinOutcome.Joined);
                    }
                }

                return new RadminJoinResult(RadminJoinOutcome.TimedOut);
            }
            finally
            {
                // Diálogo esquecido aberto fora da tela seria um modal invisível travando o Radmin.
                if (dialog != null) CloseJoinDialog(window.Element);
            }
        }

        /// <summary>Sistema → Sair. False se não achou o menu (quem chama decide se encerra à força).</summary>
        public bool ExitGracefully(int processId)
        {
            var main = FindMainWindow(processId);
            if (main == null) return false;

            var item = OpenMenuItem(main, SystemMenuNames, SystemMenuIndex, ExitMenuItemNames, fallbackIndex: null, processId, out _);
            return item != null && Invoke(item);
        }

        // ---------------------------------------------------------------- regras puras

        /// <summary>
        /// Traduz o tooltip de erro do Radmin. Os textos vêm das traduções do próprio Radmin
        /// (pt_BR e inglês); o Radmin NÃO diferencia rede inexistente de senha errada — a
        /// mensagem é a mesma para os dois.
        /// </summary>
        internal static RadminJoinOutcome ClassifyRadminMessage(string message)
        {
            var m = message.ToLowerInvariant();
            if ((m.Contains("senha") || m.Contains("password")) && (m.Contains("inválid") || m.Contains("invalid")))
                return RadminJoinOutcome.InvalidCredentials;
            if (m.Contains("rol") && (m.Contains("servidor") || m.Contains("server")))
                return RadminJoinOutcome.ServerUnreachable;
            if (m.Contains("fechada") || m.Contains("closed"))
                return RadminJoinOutcome.NetworkClosed;
            if (m.Contains("serviço não iniciado") || m.Contains("service not started") || m.Contains("service connection lost"))
                return RadminJoinOutcome.ServiceNotRunning;
            return RadminJoinOutcome.RejectedOther;
        }

        /// <summary>Item pelo nome conhecido; sem nenhum, pela posição. Puro para teste.</summary>
        internal static int PickByNameOrIndex(IReadOnlyList<string> names, IReadOnlyCollection<string> knownNames, int? fallbackIndex)
        {
            for (int i = 0; i < names.Count; i++)
            {
                if (knownNames.Any(k => string.Equals(k, names[i].Replace("&", ""), StringComparison.OrdinalIgnoreCase)))
                    return i;
            }
            return fallbackIndex is int idx && idx >= 0 && idx < names.Count ? idx : -1;
        }

        // ---------------------------------------------------------------- janela

        /// <summary>
        /// A janela principal trazida para a automação. Ao descartar, volta como estava: se
        /// estava na bandeja, o X do Radmin a esconde de novo (ele não fecha o programa); se
        /// estava na tela, volta para a mesma posição.
        /// </summary>
        internal sealed class RadminWindowSession : IDisposable
        {
            public AutomationElement Element { get; }
            public int ProcessId { get; }
            private readonly bool _wasHidden;
            private readonly Rect _originalBounds;

            public RadminWindowSession(AutomationElement element, bool wasHidden)
            {
                Element = element;
                ProcessId = element.Current.ProcessId;
                _wasHidden = wasHidden;
                _originalBounds = element.Current.BoundingRectangle;
                MoveOffScreen(element);
            }

            public void Dispose()
            {
                try
                {
                    if (_wasHidden)
                    {
                        var close = FindById(Element, MainCloseButtonId);
                        if (close != null) Invoke(close);
                    }
                    else if (!_originalBounds.IsEmpty && !double.IsInfinity(_originalBounds.X))
                    {
                        MoveTo(Element, (int)_originalBounds.X, (int)_originalBounds.Y);
                    }
                }
                catch (Exception ex)
                {
                    DiagnosticLog.Warn("Radmin", $"Não foi possível devolver a janela do Radmin ({ex.GetType().Name})");
                }
            }
        }

        private RadminWindowSession? OpenMainWindow(CancellationToken ct)
        {
            var main = FindMainWindow();
            if (main != null) return new RadminWindowSession(main, wasHidden: false);

            // Escondida na bandeja (ou o Radmin nem estava aberto): abrir o executável de novo
            // traz a instância que já roda — o Radmin é de instância única. Ela aparece por um
            // instante antes de ir para fora da tela; não há como pedir que abra escondida.
            if (_executablePath == null) return null;
            try
            {
                Process.Start(new ProcessStartInfo(_executablePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DiagnosticLog.Warn("Radmin", $"Não foi possível abrir a janela do Radmin ({ex.GetType().Name})");
                return null;
            }

            var deadline = DateTime.UtcNow + WindowAppearTimeout;
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(200);
                main = FindMainWindow();
                if (main != null) return new RadminWindowSession(main, wasHidden: true);
            }
            return null;
        }

        private static AutomationElement? FindMainWindow(int? processId = null)
        {
            foreach (var pid in processId is int p ? new[] { p } : GuiProcessIds())
            {
                var main = AutomationElement.RootElement.FindFirst(TreeScope.Children, new AndCondition(
                    new PropertyCondition(AutomationElement.ProcessIdProperty, pid),
                    new PropertyCondition(AutomationElement.AutomationIdProperty, MainWindowId)));
                if (main != null) return main;
            }
            return null;
        }

        internal static int[] GuiProcessIds()
        {
            var processes = Process.GetProcessesByName(GuiProcessName);
            try { return processes.Select(p => p.Id).ToArray(); }
            finally { foreach (var p in processes) p.Dispose(); }
        }

        private static RadminUiState ReadState(AutomationElement main)
        {
            bool powerOn = false;
            var toggle = FindById(main, PowerToggleId);
            if (toggle != null && toggle.TryGetCurrentPattern(TogglePattern.Pattern, out var pattern))
                powerOn = ((TogglePattern)pattern).Current.ToggleState == ToggleState.On;

            var networks = new List<string>();
            var list = FindById(main, NetworkListId) ?? main;
            var labels = list.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));
            foreach (AutomationElement label in labels)
            {
                if (label.Current.AutomationId.EndsWith(NetworkNameLabelSuffix, StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(label.Current.Name))
                {
                    networks.Add(label.Current.Name.Trim());
                }
            }

            return new RadminUiState(powerOn, networks);
        }

        // ---------------------------------------------------------------- diálogo e menus

        private static RadminJoinResult NotFound(string step) => new(RadminJoinOutcome.UiNotFound, FailedStep: step);

        private static AutomationElement? OpenJoinDialog(RadminWindowSession window, CancellationToken ct, out string failedStep)
        {
            var item = OpenMenuItem(window.Element, NetworkMenuNames, NetworkMenuIndex, JoinMenuItemNames, JoinMenuItemIndex, window.ProcessId, out failedStep);
            if (item == null) return null;
            failedStep = "clique em Conectar à rede";
            if (!Invoke(item)) return null;
            failedStep = "diálogo Entrar na rede";

            var deadline = DateTime.UtcNow + DialogAppearTimeout;
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(150);
                var dialog = FindById(window.Element, JoinDialogId);
                if (dialog != null) return dialog;
            }
            return null;
        }

        /// <summary>
        /// Abre um menu da barra e devolve o item pedido. O submenu é uma janela solta do
        /// processo (não fica dentro da principal), por isso a busca é pelo processo inteiro.
        /// </summary>
        private static AutomationElement? OpenMenuItem(AutomationElement main, string[] menuNames, int menuIndex,
            string[] itemNames, int? fallbackIndex, int processId, out string failedStep)
        {
            failedStep = "barra de menus";
            var menuBar = FindById(main, MenuBarId);
            if (menuBar == null) return null;

            failedStep = $"menu {menuNames[0]}";

            var topItems = menuBar.FindAll(TreeScope.Children,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem)).Cast<AutomationElement>().ToList();
            int top = PickByNameOrIndex(topItems.Select(i => i.Current.Name).ToList(), menuNames, menuIndex);
            if (top < 0 || !topItems[top].TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expand)) return null;

            ((ExpandCollapsePattern)expand).Expand();
            Thread.Sleep(400);

            failedStep = $"item {itemNames[0]}";

            var subItems = FindAllInProcess(processId,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem))
                .Where(i => TreeWalker.RawViewWalker.GetParent(i)?.Current.ClassName == "QMenu")
                .ToList();

            int index = PickByNameOrIndex(subItems.Select(i => i.Current.Name).ToList(), itemNames, fallbackIndex);
            if (index >= 0) return subItems[index];

            try { ((ExpandCollapsePattern)expand).Collapse(); } catch { }
            return null;
        }

        private static void SelectPrivateTab(AutomationElement dialog)
        {
            // A aba "Rede de jogos" lista redes públicas; nome+senha só existe na primeira.
            var tabBar = FindById(dialog, JoinTabBarId);
            var first = tabBar?.FindFirst(TreeScope.Children,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));
            if (first != null) Invoke(first);
        }

        private static void CloseJoinDialog(AutomationElement main)
        {
            try
            {
                var cancel = FindById(main, JoinCancelButtonId);
                if (cancel != null) Invoke(cancel);
            }
            catch (Exception ex)
            {
                DiagnosticLog.Warn("Radmin", $"Não foi possível fechar o diálogo do Radmin ({ex.GetType().Name})");
            }
        }

        private static string? FindErrorTooltip(int processId)
        {
            var tip = AutomationElement.RootElement.FindFirst(TreeScope.Children, new AndCondition(
                new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
                new PropertyCondition(AutomationElement.ClassNameProperty, ErrorTooltipClass)));
            var text = tip?.Current.Name;
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        // ---------------------------------------------------------------- utilidades

        private static AutomationElement? FindById(AutomationElement scope, string automationId)
        {
            var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
            return scope.FindFirst(TreeScope.Descendants, condition)
                // O diálogo é janela própria, irmã da principal na árvore; procurar só dentro
                // da principal não o acha.
                ?? FindInProcess(scope.Current.ProcessId, condition);
        }

        /// <summary>
        /// Procura nas janelas de topo do processo. Nunca a partir da raiz com Descendants: isso
        /// percorre o desktop inteiro (um navegador aberto são milhares de elementos) e leva
        /// segundos por busca.
        /// </summary>
        private static AutomationElement? FindInProcess(int processId, Condition condition)
            => FindAllInProcess(processId, condition).FirstOrDefault();

        private static IEnumerable<AutomationElement> FindAllInProcess(int processId, Condition condition)
        {
            var windows = AutomationElement.RootElement.FindAll(TreeScope.Children,
                new PropertyCondition(AutomationElement.ProcessIdProperty, processId));
            foreach (AutomationElement window in windows)
            {
                foreach (AutomationElement found in window.FindAll(TreeScope.Element | TreeScope.Descendants, condition))
                    yield return found;
            }
        }

        private static bool SetValue(AutomationElement scope, string automationId, string value)
        {
            var field = FindById(scope, automationId);
            if (field == null || !field.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern)) return false;
            ((ValuePattern)pattern).SetValue(value);
            return true;
        }

        private static bool Invoke(AutomationElement element)
        {
            if (!element.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern)) return false;
            ((InvokePattern)pattern).Invoke();
            return true;
        }

        /// <summary>
        /// Leva a janela para fora da área visível. O TransformPattern não serve: o Qt prende a
        /// janela dentro da tela (pedir -3000 dá 0). O SetWindowPos direto passa, e a automação
        /// continua lendo e preenchendo os campos normalmente lá fora.
        /// </summary>
        private static void MoveOffScreen(AutomationElement window) => MoveTo(window, -32000, -32000);

        private static void MoveTo(AutomationElement window, int x, int y)
        {
            var handle = new IntPtr(window.Current.NativeWindowHandle);
            if (handle != IntPtr.Zero)
                SetWindowPos(handle, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
    }
}
