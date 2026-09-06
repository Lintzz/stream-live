using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using StreamLiveApp.Models;

namespace StreamLiveApp
{
    /// <summary>Um amigo na lista de convidados da live privada.</summary>
    public sealed class InvitedFriend : INotifyPropertyChanged
    {
        private bool _isInvited;

        public string Name { get; init; } = string.Empty;
        public string Ip { get; init; } = string.Empty;

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

    /// <summary>
    /// Serve aos dois lados da senha de sala: o host define uma (opcional) e escolhe se a live
    /// é privada ao iniciar, e o viewer só vê este modal — sem o bloco de privacidade — quando
    /// o host realmente exige senha.
    /// </summary>
    public partial class RoomPasswordDialog : Window
    {
        private bool _syncing;

        public string Password { get; private set; } = string.Empty;

        /// <summary>Host: a live não deve ser anunciada para quem não foi convidado.</summary>
        public bool IsPrivateLive { get; private set; }

        /// <summary>Host: IPs que podem ver e entrar na live privada.</summary>
        public IReadOnlyList<string> InvitedIps { get; private set; } = Array.Empty<string>();

        private readonly ObservableCollection<InvitedFriend> _invitedFriends = new();

        private RoomPasswordDialog()
        {
            InitializeComponent();
            Loaded += (s, e) => TxtPassword.Focus();
        }

        /// <summary>
        /// Host: senha da sala (em branco = sala aberta) e escolha de quem enxerga a live.
        /// As duas coisas são independentes — a senha barra a entrada, a live privada esconde
        /// que a transmissão existe.
        /// </summary>
        public static RoomPasswordDialog ForHost(
            string currentPassword,
            IEnumerable<Friend>? friends,
            bool wasPrivate,
            IEnumerable<string>? previouslyInvitedIps)
        {
            var dialog = new RoomPasswordDialog();
            dialog.TxtTitle.Text = "Iniciar transmissão";
            dialog.TxtSubtitle.Text = "Senha da sala — deixe em branco para que qualquer amigo possa entrar.";
            dialog.BtnConfirm.Content = "Iniciar";
            dialog.SetPasswordText(currentPassword ?? string.Empty);
            dialog.SetUpPrivacy(friends, wasPrivate, previouslyInvitedIps);
            return dialog;
        }

        private void SetUpPrivacy(IEnumerable<Friend>? friends, bool wasPrivate, IEnumerable<string>? previouslyInvitedIps)
        {
            var invited = new HashSet<string>(
                previouslyInvitedIps ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            foreach (var friend in friends ?? Enumerable.Empty<Friend>())
            {
                var item = new InvitedFriend
                {
                    Name = friend.Name,
                    Ip = friend.Ip,
                    IsInvited = invited.Contains(friend.Ip)
                };
                item.PropertyChanged += (s, e) => UpdatePrivacyWarning();
                _invitedFriends.Add(item);
            }

            LstInvited.ItemsSource = _invitedFriends;
            TxtNoFriends.Visibility = _invitedFriends.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ChkPrivateLive.IsChecked = wasPrivate;
            PrivacyPanel.Visibility = Visibility.Visible;
            UpdatePrivacyWarning();
        }

        private void ChkPrivateLive_Changed(object sender, RoutedEventArgs e)
        {
            InvitedScroll.IsEnabled = ChkPrivateLive.IsChecked == true;
            UpdatePrivacyWarning();
        }

        /// <summary>
        /// Live privada sem ninguém marcado é um estado válido — invisível para todo mundo —,
        /// mas quase sempre é engano, então precisa estar dito na tela.
        /// </summary>
        private void UpdatePrivacyWarning()
        {
            if (TxtPrivacyWarning == null) return;

            bool willBeInvisible = ChkPrivateLive.IsChecked == true && !_invitedFriends.Any(f => f.IsInvited);
            TxtPrivacyWarning.Visibility = willBeInvisible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>Viewer: o host pediu senha.</summary>
        public static RoomPasswordDialog ForViewer(string friendName, bool previousAttemptFailed)
        {
            var dialog = new RoomPasswordDialog();
            dialog.TxtTitle.Text = $"Senha da sala de {friendName}";
            dialog.TxtSubtitle.Text = "Esta live é protegida. Peça a senha para quem está transmitindo.";
            dialog.BtnConfirm.Content = "Entrar";

            if (previousAttemptFailed)
            {
                dialog.TxtError.Text = "Senha incorreta. Tente novamente.";
                dialog.TxtError.Visibility = Visibility.Visible;
            }

            return dialog;
        }

        private void SetPasswordText(string value)
        {
            _syncing = true;
            TxtPassword.Password = value;
            TxtPasswordVisible.Text = value;
            _syncing = false;
        }

        private string CurrentText => BtnReveal.IsChecked == true ? TxtPasswordVisible.Text : TxtPassword.Password;

        private void Password_Changed(object sender, RoutedEventArgs e)
        {
            if (_syncing) return;

            _syncing = true;
            if (sender == TxtPassword) TxtPasswordVisible.Text = TxtPassword.Password;
            else TxtPassword.Password = TxtPasswordVisible.Text;
            _syncing = false;

            TxtError.Visibility = Visibility.Collapsed;
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
            Password = CurrentText ?? string.Empty;
            IsPrivateLive = ChkPrivateLive.IsChecked == true;
            InvitedIps = IsPrivateLive
                ? _invitedFriends.Where(f => f.IsInvited).Select(f => f.Ip).ToList()
                : Array.Empty<string>();

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
            if (e.Key == Key.Enter) BtnConfirm_Click(sender, e);
            else if (e.Key == Key.Escape) BtnCancel_Click(sender, e);
        }
    }
}
