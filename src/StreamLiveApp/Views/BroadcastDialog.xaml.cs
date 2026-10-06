using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using StreamLiveApp.Models;

namespace StreamLiveApp
{
    /// <summary>Um amigo na lista "Quem pode ver" do modal de transmitir.</summary>
    public sealed class InvitedFriend : INotifyPropertyChanged
    {
        private bool _isInvited;

        public string Name { get; init; } = string.Empty;
        public string Ip { get; init; } = string.Empty;

        /// <summary>Nome inteiro (o da caixa pode sair cortado com "…") e o IP.</summary>
        public string Hint => $"{Name} · {Ip}";

        public bool IsInvited
        {
            get => _isInvited;
            set
            {
                if (_isInvited == value) return;
                _isInvited = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInvited)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>Uma tela no seletor, com a miniatura que se atualiza enquanto o modal está aberto.</summary>
    public sealed class ScreenChoice : INotifyPropertyChanged
    {
        private BitmapSource? _thumbnail;

        public required CaptureSource Source { get; init; }
        public string Title => Source.Title;
        public string Detail => Source.Detail;

        /// <summary>O leitor de tela não vê a miniatura: o nome diz qual tela é.</summary>
        public string AccessibleName => $"{Source.Title}, {Source.Detail}";

        public BitmapSource? Thumbnail
        {
            get => _thumbnail;
            set
            {
                if (ReferenceEquals(_thumbnail, value)) return;
                bool hadNone = _thumbnail == null;
                _thumbnail = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Thumbnail)));
                if (hadNone != (value == null))
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasNoThumbnail)));
            }
        }

        public bool HasNoThumbnail => _thumbnail == null;

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>
    /// "Transmitir": escolher a tela vendo o que está nela (como no Discord), quem pode ver e a
    /// senha, num modal só. Antes a tela era um ComboBox "Tela 1 / Tela 2" na barra — quem tem
    /// dois monitores precisava saber de cabeça qual era qual — e a live privada era uma caixa
    /// à parte, marcada antes de marcar as pessoas. Agora a lista de convidados decide sozinha:
    /// ninguém marcado = pública (ver <see cref="BroadcastSettings.PrivateLive"/>).
    /// O mesmo modal, só com as telas, serve ao "Trocar tela" durante a live.
    /// </summary>
    public partial class BroadcastDialog : Window
    {
        private const int ThumbnailMaxWidth = 480;
        private const int ThumbnailMaxHeight = 270;

        private readonly ObservableCollection<ScreenChoice> _screens = new();
        private readonly ObservableCollection<InvitedFriend> _invitedFriends = new();
        private readonly DispatcherTimer _thumbnailTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private readonly DateTime _openedAt = DateTime.UtcNow;
        private bool _capturing;
        private bool _syncing;

        public CaptureSource? SelectedSource { get; private set; }
        public string Password { get; private set; } = string.Empty;

        /// <summary>IPs marcados em "Quem pode ver". Vazio = live pública.</summary>
        public IReadOnlyList<string> InvitedIps { get; private set; } = Array.Empty<string>();

        private BroadcastDialog()
        {
            InitializeComponent();
            LstScreens.ItemsSource = _screens;
            LstInvited.ItemsSource = _invitedFriends;

            _thumbnailTimer.Tick += (s, e) => RefreshThumbnails();
            Loaded += (s, e) =>
            {
                RefreshThumbnails();
                _thumbnailTimer.Start();
                FocusScreens();
            };
            // A miniatura só existe com o modal aberto: fechou, a captura para.
            Closed += (s, e) => _thumbnailTimer.Stop();
        }

        /// <summary>Iniciar a live: telas, quem pode ver e senha.</summary>
        public static BroadcastDialog ForStart(
            IEnumerable<Friend>? friends, string currentPassword, string? previousScreenTitle)
        {
            var dialog = new BroadcastDialog();
            dialog.LoadScreens(previousScreenTitle);

            // Toda live começa pública, sem ninguém marcado: lembrar os convidados da vez
            // anterior faria uma live sair privada sem a pessoa perceber.
            foreach (var friend in friends ?? Enumerable.Empty<Friend>())
            {
                var item = new InvitedFriend { Name = friend.Name, Ip = friend.Ip };
                item.PropertyChanged += (s, e) => dialog.UpdateAudience();
                dialog._invitedFriends.Add(item);
            }

            bool hasFriends = dialog._invitedFriends.Count > 0;
            dialog.InvitedScroll.Visibility = hasFriends ? Visibility.Visible : Visibility.Collapsed;
            dialog.AudiencePanel.Visibility = hasFriends ? Visibility.Visible : Visibility.Collapsed;
            dialog.TxtNoFriends.Visibility = hasFriends ? Visibility.Collapsed : Visibility.Visible;
            dialog.SetPasswordText(currentPassword ?? string.Empty);
            dialog.UpdateAudience();
            return dialog;
        }

        /// <summary>Trocar a tela de uma live que já está no ar.</summary>
        public static BroadcastDialog ForChange(string? currentScreenTitle)
        {
            var dialog = new BroadcastDialog();
            dialog.Title = "Trocar tela";
            dialog.TxtTitle.Text = "Trocar tela";
            dialog.BtnConfirm.Content = "Trocar";
            dialog.StartOptionsPanel.Visibility = Visibility.Collapsed;
            dialog.LoadScreens(currentScreenTitle);
            return dialog;
        }

        private void LoadScreens(string? preferredTitle)
        {
            var sources = DemoMode.IsEnabled ? DemoMode.CreateScreens() : WindowHelper.GetCapturableScreens();
            foreach (var source in sources) _screens.Add(new ScreenChoice { Source = source });

            TxtNoScreens.Visibility = _screens.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            LstScreens.Visibility = _screens.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            // Volta na tela da última live; com um monitor só, não há o que escolher.
            var preselected = _screens.FirstOrDefault(s => s.Title == preferredTitle)
                              ?? (_screens.Count == 1 ? _screens[0] : null);
            if (preselected != null) LstScreens.SelectedItem = preselected;
            UpdateConfirm();
        }

        /// <summary>
        /// Foco no card escolhido (ou no primeiro), para as setas já andarem entre as telas.
        /// O ListBox sozinho poria o foco na lista, e não num item.
        /// </summary>
        private void FocusScreens()
        {
            if (_screens.Count == 0) return;

            var target = LstScreens.SelectedItem ?? _screens[0];
            LstScreens.UpdateLayout();
            if (LstScreens.ItemContainerGenerator.ContainerFromItem(target) is UIElement container)
                container.Focus();
        }

        /// <summary>
        /// Uma foto de cada tela por segundo, fora da thread de UI: o CopyFromScreen de duas
        /// telas 1080p leva dezenas de ms. Se a anterior ainda não acabou, pula.
        /// </summary>
        private async void RefreshThumbnails()
        {
            if (_capturing) return;
            _capturing = true;

            try
            {
                var targets = _screens.ToList();
                double t = (DateTime.UtcNow - _openedAt).TotalSeconds;
                var shots = await Task.Run(() => targets.Select((s, i) => Capture(s.Source, i, t)).ToList());

                for (int i = 0; i < targets.Count; i++)
                {
                    // Miniatura que falha mantém a anterior; sem nenhuma, fica o bloco com o nome.
                    if (shots[i] != null) targets[i].Thumbnail = shots[i];
                }
            }
            catch (Exception ex)
            {
                DiagnosticLog.Warn("Transmitir", $"Miniatura das telas falhou: {ex.Message}");
            }
            finally
            {
                _capturing = false;
            }
        }

        private static BitmapSource? Capture(CaptureSource source, int index, double t)
        {
            // No --demo nada da tela de verdade aparece: é dele que saem os prints e o GIF.
            if (DemoMode.IsEnabled)
            {
                var scene = index == 0 ? DemoScene.Plataforma : DemoScene.Espaco;
                var (pixels, width, height) = DemoFeed.RenderStill(scene, t);
                return WindowHelper.ThumbnailFromPixels(pixels, width, height, ThumbnailMaxWidth, ThumbnailMaxHeight);
            }

            return WindowHelper.CaptureThumbnail(source.ScreenBounds, ThumbnailMaxWidth, ThumbnailMaxHeight);
        }

        /// <summary>
        /// A frase embaixo da lista diz o que vai acontecer com quem está marcado agora —
        /// sem ela, "ninguém marcado" não deixa claro que a live sai pública.
        /// </summary>
        internal static string DescribeAudience(IReadOnlyList<string> invitedNames)
        {
            if (invitedNames.Count == 0)
                return "Pública — todos os seus amigos veem. Marque alguém para transmitir só para essas pessoas.";

            string who = invitedNames.Count switch
            {
                1 => invitedNames[0],
                2 => $"{invitedNames[0]} e {invitedNames[1]}",
                3 => $"{invitedNames[0]}, {invitedNames[1]} e {invitedNames[2]}",
                _ => $"{invitedNames[0]}, {invitedNames[1]} e mais {invitedNames.Count - 2}"
            };
            string verb = invitedNames.Count == 1 ? "vê" : "veem";
            return $"Privada — só {who} {verb} esta live. Os outros veem você como offline.";
        }

        private void UpdateAudience()
        {
            var names = _invitedFriends.Where(f => f.IsInvited).Select(f => f.Name).ToList();
            bool isPrivate = names.Count > 0;

            TxtAudience.Text = DescribeAudience(names);
            var color = isPrivate ? System.Windows.Media.Color.FromRgb(0xE8, 0xA3, 0x3D) : System.Windows.Media.Color.FromRgb(0x9A, 0x9A, 0xA4);
            TxtAudience.Foreground = new SolidColorBrush(color);
            AudienceIcon.Foreground = new SolidColorBrush(color);
            AudienceIcon.Text = isPrivate ? "" : "";
        }

        private void UpdateConfirm()
        {
            BtnConfirm.IsEnabled = LstScreens.SelectedItem is ScreenChoice;
            BtnConfirm.ToolTip = BtnConfirm.IsEnabled ? null : "Escolha uma tela";
        }

        private void LstScreens_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateConfirm();
        }

        private void LstScreens_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LstScreens.SelectedItem is ScreenChoice) BtnConfirm_Click(sender, e);
        }

        private void SetPasswordText(string value)
        {
            _syncing = true;
            TxtPassword.Password = value;
            TxtPasswordVisible.Text = value;
            _syncing = false;
        }

        private string CurrentPassword => BtnReveal.IsChecked == true ? TxtPasswordVisible.Text : TxtPassword.Password;

        private void Password_Changed(object sender, RoutedEventArgs e)
        {
            if (_syncing) return;

            _syncing = true;
            if (sender == TxtPassword) TxtPasswordVisible.Text = TxtPassword.Password;
            else TxtPassword.Password = TxtPasswordVisible.Text;
            _syncing = false;
        }

        private void BtnReveal_Changed(object sender, RoutedEventArgs e)
        {
            bool reveal = BtnReveal.IsChecked == true;
            TxtPasswordVisible.Visibility = reveal ? Visibility.Visible : Visibility.Collapsed;
            TxtPassword.Visibility = reveal ? Visibility.Collapsed : Visibility.Visible;

            if (reveal)
            {
                TxtPasswordVisible.Focus();
                TxtPasswordVisible.CaretIndex = TxtPasswordVisible.Text.Length;
            }
            else
            {
                TxtPassword.Focus();
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (!(LstScreens.SelectedItem is ScreenChoice choice)) return;

            SelectedSource = choice.Source;
            Password = CurrentPassword ?? string.Empty;
            InvitedIps = _invitedFriends.Where(f => f.IsInvited).Select(f => f.Ip).ToList();

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Enter num amigo marca ou desmarca, como o Espaço. Confirmar ali subiria a
                // live no meio da escolha de quem pode ver.
                if (Keyboard.FocusedElement is System.Windows.Controls.CheckBox box)
                {
                    box.IsChecked = box.IsChecked != true;
                }
                else
                {
                    // Foco num card de tela que ainda não foi escolhido (o modal abre com o foco
                    // na primeira): Enter escolhe aquela e segue, em vez de não fazer nada.
                    if (Keyboard.FocusedElement is System.Windows.Controls.ListBoxItem screen)
                        screen.IsSelected = true;
                    BtnConfirm_Click(sender, e);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                BtnCancel_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}
