using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamLiveApp.Services
{
    public enum RadminPowerResult
    {
        /// <summary>Estado desconhecido (Radmin fechado, registro ilegível): nada foi feito.</summary>
        Unknown,
        AlreadyOnline,
        TurnedOn,
        Failed
    }

    /// <summary>
    /// Deixa o Radmin VPN on-line sem mostrar nada na tela.
    ///
    /// O Radmin lembra o último estado: fechar a janela dele desliga a VPN ("Switched Off" no
    /// service.log) e grava PowerOn=0, então na próxima abertura ele sobe off-line e o usuário
    /// tinha de clicar com o botão direito na bandeja e em "Ficar on-line". Não há argumento
    /// de linha de comando para isso, e o registro é só leitura para usuário comum.
    ///
    /// O que funciona (medido em 2026-10-05, Radmin 2.1.1): mandar o clique do mouse direto
    /// para a janela principal escondida, na posição do botão de energia. O Qt entrega o clique
    /// ao botão mesmo com a janela oculta, e nada aparece. Foram descartados, por testes:
    /// UI Automation com a janela escondida (os controles não aparecem na árvore), mostrar a
    /// janela transparente (o Qt tira o estilo de camada) e com região vazia (aparece mesmo
    /// assim) — e o Toggle da automação, que só muda a aparência do botão sem ligar nada.
    ///
    /// Esse clique só acerta se a janela já foi mostrada alguma vez na sessão do Radmin (antes
    /// disso o Qt não calculou onde fica o botão). Aberto com /minimized ele nunca foi, então o
    /// reserva é o "Ficar on-line" do menu da bandeja — que pisca ~0,2 s perto do relógio.
    ///
    /// O clique é um liga/desliga: só é mandado com o registro dizendo "desligado", e o
    /// resultado é conferido no registro.
    /// </summary>
    public static class RadminPowerService
    {
        private const string RegistryKey = @"SOFTWARE\WOW6432Node\Famatech\RadminVPN\1.0";

        // Posição do botão de energia (BPower) na janela de 336 px, a 96 DPI. Se o Radmin
        // mudar a interface, é aqui que se ajusta — a medida sai da árvore de UI Automation
        // com a janela aberta (BoundingRectangle do "...userInfoWidget.BPower").
        private const int ReferenceWidth = 336;
        private const int ReferenceX = 51;
        private const int ReferenceY = 105;

        private static readonly TimeSpan WindowTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan SettleAfterStart = TimeSpan.FromSeconds(1.5);
        private static readonly TimeSpan ConfirmTimeout = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan HiddenClickTimeout = TimeSpan.FromSeconds(1.5);
        private static readonly TimeSpan MenuTimeout = TimeSpan.FromSeconds(2);

        // Mensagem que o ícone da bandeja do Qt recebe do Windows (WM_APP + 101 no
        // qsystemtrayicon_win.cpp); com WM_CONTEXTMENU no lParam o Qt abre o menu do ícone na
        // posição que vai no wParam, igual ao clique com o botão direito.
        private const uint TrayCallbackMessage = 0x8000 + 101;
        private const int WM_CONTEXTMENU = 0x007B;

        /// <param name="justStarted">O app acabou de abrir o Radmin: espera ele terminar de subir antes de clicar.</param>
        public static Task<RadminPowerResult> EnsureOnlineAsync(bool justStarted, CancellationToken ct = default)
            => Task.Run(() => EnsureOnline(justStarted, ct), ct);

        private static RadminPowerResult EnsureOnline(bool justStarted, CancellationToken ct)
        {
            // Logo depois de abrir, o registro ainda pode trazer o estado da sessão anterior;
            // ele só é lido depois que a janela existe.
            var window = WaitForMainWindow(ct);
            if (window == IntPtr.Zero)
            {
                if (justStarted) DiagnosticLog.Warn("Radmin", "Janela do Radmin não apareceu; não deu para conferir se está on-line");
                return justStarted ? RadminPowerResult.Failed : RadminPowerResult.Unknown;
            }

            var power = ReadPowerOn();
            if (power == true) return RadminPowerResult.AlreadyOnline;
            if (!ShouldTurnOn(power)) return RadminPowerResult.Unknown;

            if (justStarted) Thread.Sleep(SettleAfterStart);

            // 1) Clique na janela escondida: não aparece nada. Só funciona se a janela já foi
            //    mostrada alguma vez nesta sessão do Radmin — antes disso o Qt nem calculou onde
            //    fica o botão, e o clique cai no vazio (aberto com /minimized é sempre o caso).
            //    Recém-aberto pelo app, a janela certamente nunca foi mostrada: nem tenta.
            if (ReadPowerOn() != false) return RadminPowerResult.AlreadyOnline;
            if (!justStarted)
            {
                ClickPowerButton(window);
                if (WaitForOnline(HiddenClickTimeout, ct))
                {
                    DiagnosticLog.Info("Radmin", "Radmin estava off-line; ligado pelo app (clique na janela escondida)");
                    return RadminPowerResult.TurnedOn;
                }
            }

            // 2) O "Ficar on-line" do menu da bandeja, como o usuário faria. O menu chega a
            //    aparecer perto do relógio por ~0,2 s: o Qt tira a transparência ao mostrá-lo,
            //    então não há como escondê-lo. Ainda é melhor do que o usuário ter de clicar.
            if (ReadPowerOn() != false) return RadminPowerResult.TurnedOn;
            if (ClickTrayMenuItem(IsGoOnlineItem, ct) && WaitForOnline(ConfirmTimeout, ct))
            {
                DiagnosticLog.Info("Radmin", "Radmin estava off-line; ligado pelo app (menu da bandeja)");
                return RadminPowerResult.TurnedOn;
            }

            if (ReadPowerOn() == true) return RadminPowerResult.TurnedOn;
            DiagnosticLog.Warn("Radmin", "Radmin off-line e nem o clique nem o menu da bandeja o ligaram");
            return RadminPowerResult.Failed;
        }

        /// <summary>
        /// Fecha o Radmin VPN (o usuário marcou "Fechar o Radmin VPN também" ao sair). Pelo
        /// "Sair" do menu da bandeja, que é o fechamento limpo; encerrar o processo é só o
        /// reserva, porque deixa o ícone fantasma na bandeja até o mouse passar por cima.
        /// Bloqueia no máximo <paramref name="budget"/>: o fechamento do app não espera mais.
        /// </summary>
        public static void ExitRadmin(TimeSpan budget)
        {
            var work = Task.Run(() =>
            {
                var deadline = DateTime.UtcNow + budget - TimeSpan.FromMilliseconds(500);
                try
                {
                    if (!ClickTrayMenuItem(IsExitItem, CancellationToken.None))
                        DiagnosticLog.Warn("Radmin", "Não achou o Sair no menu do Radmin; encerrando o processo");

                    while (DateTime.UtcNow < deadline && IsRadminRunning()) Thread.Sleep(100);
                    if (IsRadminRunning())
                    {
                        foreach (var process in Process.GetProcessesByName("RvRvpnGui"))
                        {
                            using (process) process.Kill();
                        }
                    }
                    DiagnosticLog.Info("Radmin", "Radmin VPN fechado junto com o app");
                }
                catch (Exception ex)
                {
                    DiagnosticLog.Warn("Radmin", $"Fechar o Radmin falhou ({ex.GetType().Name})");
                }
            });

            try { work.Wait(budget); } catch { }
        }

        private static bool IsRadminRunning()
        {
            var processes = Process.GetProcessesByName("RvRvpnGui");
            foreach (var p in processes) p.Dispose();
            return processes.Length > 0;
        }

        // ---------------------------------------------------------------- regras puras

        /// <summary>PowerOn do registro do Radmin: 1 on-line, 0 off-line; qualquer outra coisa, desconhecido.</summary>
        internal static bool? ParsePowerOn(object? value) => value switch
        {
            int i when i == 1 => true,
            int i when i == 0 => false,
            _ => null
        };

        internal static bool ShouldTurnOn(bool? powerOn) => powerOn == false;

        /// <summary>
        /// "Ficar on-line" / "Go online" do menu da bandeja. Pelo nome e nunca pela posição: o
        /// menu muda de itens (o "Abrir chat" só aparece on-line), e o vizinho é o "Ficar
        /// off-line". Outro idioma do Radmin não casa, e aí o app só avisa.
        /// </summary>
        /// <summary>"Sair" / "Exit" do menu da bandeja.</summary>
        internal static bool IsExitItem(string name)
        {
            var n = name.Replace("&", "").Trim();
            return n.Equals("Sair", StringComparison.OrdinalIgnoreCase) || n.Equals("Exit", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsGoOnlineItem(string name)
        {
            var n = name.Replace("&", "").ToLowerInvariant();
            return (n.Contains("on-line") || n.Contains("online")) && !n.Contains("off");
        }

        /// <summary>
        /// Ponto do botão em coordenadas da janela. Escala pela largura real porque o Radmin
        /// acompanha a escala do Windows (a 150% a janela tem 504 px).
        /// </summary>
        internal static (int X, int Y) PowerButtonPoint(int windowWidth)
        {
            double scale = windowWidth > 0 ? (double)windowWidth / ReferenceWidth : 1.0;
            return ((int)Math.Round(ReferenceX * scale, MidpointRounding.AwayFromZero),
                    (int)Math.Round(ReferenceY * scale, MidpointRounding.AwayFromZero));
        }

        // ---------------------------------------------------------------- Windows

        private static bool? ReadPowerOn()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RegistryKey);
                return ParsePowerOn(key?.GetValue("PowerOn"));
            }
            catch
            {
                return null;
            }
        }

        private static bool WaitForOnline(TimeSpan timeout, CancellationToken ct)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();
                if (ReadPowerOn() == true) return true;
                Thread.Sleep(100);
            }
            return false;
        }

        private static IntPtr WaitForMainWindow(CancellationToken ct)
        {
            var deadline = DateTime.UtcNow + WindowTimeout;
            while (true)
            {
                var window = FindMainWindow();
                if (window != IntPtr.Zero || DateTime.UtcNow >= deadline) return window;
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Janela principal do Radmin, visível ou escondida na bandeja. A classe leva a versão
        /// do Qt no nome ("Qt51515QWindowIcon"), por isso a comparação é pelo fim.
        /// </summary>
        private static IntPtr FindMainWindow()
            => FindRadminWindow((cls, title) => cls.EndsWith("QWindowIcon", StringComparison.Ordinal) && title == "Radmin VPN");

        /// <summary>Janela invisível que recebe os eventos do ícone da bandeja.</summary>
        private static IntPtr FindTrayWindow()
            => FindRadminWindow((cls, _) => cls.EndsWith("TrayIconMessageWindowClass", StringComparison.Ordinal));

        private static IntPtr FindRadminWindow(Func<string, string, bool> match)
        {
            var pids = new System.Collections.Generic.HashSet<uint>();
            foreach (var process in Process.GetProcessesByName("RvRvpnGui"))
            {
                using (process) pids.Add((uint)process.Id);
            }
            if (pids.Count == 0) return IntPtr.Zero;

            IntPtr found = IntPtr.Zero;
            EnumWindows((hwnd, _) =>
            {
                GetWindowThreadProcessId(hwnd, out var pid);
                if (!pids.Contains(pid)) return true;

                var cls = new StringBuilder(128);
                GetClassName(hwnd, cls, cls.Capacity);
                var title = new StringBuilder(64);
                GetWindowText(hwnd, title, title.Capacity);
                if (match(cls.ToString(), title.ToString()))
                {
                    found = hwnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        /// <summary>
        /// Abre o menu do ícone da bandeja e clica no item pedido pela UI Automation. Se o item
        /// não aparecer (outro idioma, Radmin mudou), fecha o menu sem clicar em nada.
        /// </summary>
        private static bool ClickTrayMenuItem(Func<string, bool> isWanted, CancellationToken ct)
        {
            var tray = FindTrayWindow();
            if (tray == IntPtr.Zero) return false;
            GetWindowThreadProcessId(tray, out var pid);

            // Canto do relógio, onde o menu apareceria num clique de verdade.
            int x = GetSystemMetrics(SM_CXSCREEN) - 40, y = GetSystemMetrics(SM_CYSCREEN) - 40;
            PostMessage(tray, TrayCallbackMessage, new IntPtr((y << 16) | (x & 0xFFFF)), new IntPtr(WM_CONTEXTMENU));

            var menuCondition = new System.Windows.Automation.AndCondition(
                new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.ProcessIdProperty, (int)pid),
                new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.ClassNameProperty, "QMenu"));
            var itemCondition = new System.Windows.Automation.PropertyCondition(
                System.Windows.Automation.AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.MenuItem);

            var deadline = DateTime.UtcNow + MenuTimeout;
            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();
                var menu = System.Windows.Automation.AutomationElement.RootElement.FindFirst(
                    System.Windows.Automation.TreeScope.Children, menuCondition);
                var items = menu?.FindAll(System.Windows.Automation.TreeScope.Children, itemCondition);
                if (items is { Count: > 0 })
                {
                    bool clicked = false;
                    foreach (System.Windows.Automation.AutomationElement item in items)
                    {
                        if (isWanted(item.Current.Name)
                            && item.TryGetCurrentPattern(System.Windows.Automation.InvokePattern.Pattern, out var invoke))
                        {
                            ((System.Windows.Automation.InvokePattern)invoke).Invoke();
                            clicked = true;
                            break;
                        }
                    }

                    // O Invoke da automação executa o item mas NÃO fecha o menu (um clique de
                    // verdade fecharia): ele ficava aberto até o usuário clicar em outro lugar.
                    // Fecha com Esc — o mesmo vale para menu aberto sem o item.
                    CloseVisibleMenu(pid);
                    return clicked;
                }
                Thread.Sleep(25);
            }
            return false;
        }

        /// <summary>Fecha o menu do Radmin que estiver na tela, com Esc, e confere que fechou.</summary>
        private static void CloseVisibleMenu(uint pid)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var menu = FindVisibleMenu(pid);
                if (menu == IntPtr.Zero) return;

                PostMessage(menu, WM_KEYDOWN, new IntPtr(VK_ESCAPE), new IntPtr(0x00010001));
                for (int i = 0; i < 25 && FindVisibleMenu(pid) != IntPtr.Zero; i++) Thread.Sleep(20);
            }

            if (FindVisibleMenu(pid) != IntPtr.Zero)
                DiagnosticLog.Warn("Radmin", "O menu da bandeja do Radmin não fechou com Esc");
        }

        /// <summary>Janela de menu (popup do Qt) do Radmin que está visível agora.</summary>
        private static IntPtr FindVisibleMenu(uint pid)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hwnd, _) =>
            {
                GetWindowThreadProcessId(hwnd, out var owner);
                if (owner != pid || !IsWindowVisible(hwnd)) return true;

                var cls = new StringBuilder(128);
                GetClassName(hwnd, cls, cls.Capacity);
                if (cls.ToString().Contains("QWindowPopup", StringComparison.Ordinal))
                {
                    found = hwnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        private static void ClickPowerButton(IntPtr window)
        {
            GetWindowRect(window, out var rect);
            var (x, y) = PowerButtonPoint(rect.Right - rect.Left);
            var point = new IntPtr((y << 16) | (x & 0xFFFF));

            // Movimento antes do clique: o botão do Qt só aceita o clique depois de saber que o
            // mouse está em cima dele.
            PostMessage(window, WM_MOUSEMOVE, IntPtr.Zero, point);
            Thread.Sleep(30);
            PostMessage(window, WM_LBUTTONDOWN, new IntPtr(MK_LBUTTON), point);
            Thread.Sleep(30);
            PostMessage(window, WM_LBUTTONUP, IntPtr.Zero, point);
        }

        private const uint WM_MOUSEMOVE = 0x0200;
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const int MK_LBUTTON = 0x0001;
        private const uint WM_KEYDOWN = 0x0100;
        private const int VK_ESCAPE = 0x1B;
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hwnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hwnd, StringBuilder name, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
    }
}
