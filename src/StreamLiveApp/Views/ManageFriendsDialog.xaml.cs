using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StreamLiveApp.Models;
using StreamLiveApp.Services;

namespace StreamLiveApp
{
    public partial class ManageFriendsDialog : Window
    {
        private readonly ObservableCollection<Friend> _friends;

        public event Action<Friend> FriendAdded = delegate {};

        public ManageFriendsDialog(ObservableCollection<Friend> friends, string? suggestedIp = null)
        {
            InitializeComponent();

            _friends = friends;
            LstFriends.ItemsSource = _friends;
            _friends.CollectionChanged += Friends_CollectionChanged;

            if (!string.IsNullOrWhiteSpace(suggestedIp) && !_friends.Any(f => f.Ip == suggestedIp))
            {
                TxtNewIp.Text = suggestedIp;
            }

            UpdateEmptyState();
            Loaded += (s, e) => TxtNewName.Focus();
        }

        private void Friends_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateEmptyState();

        private void UpdateEmptyState()
        {
            TxtEmpty.Visibility = _friends.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string ip = TxtNewIp.Text?.Trim() ?? string.Empty;
            string name = TxtNewName.Text?.Trim() ?? string.Empty;

            string? error = string.IsNullOrEmpty(ip)
                ? "Informe o IP do amigo — o que aparece para ele na Radmin VPN, ex.: 26.10.0.5."
                : AddIpError(ip);
            if (error != null)
            {
                ShowAddError(error);
                TxtNewIp.Focus();
                TxtNewIp.SelectAll();
                return;
            }

            var friend = new Friend { Name = string.IsNullOrWhiteSpace(name) ? ip : name, Ip = ip };
            _friends.Add(friend);
            Save();

            TxtNewName.Text = string.Empty;
            TxtNewIp.Text = string.Empty;
            TxtNewName.Focus();

            ShowAddError(null);

            FriendAdded?.Invoke(friend);
        }

        /// <summary>O que impede de adicionar este IP, ou null se ele serve.</summary>
        private string? AddIpError(string ip)
        {
            if (!FriendsService.IsValidFriendIp(ip))
                return FriendIpRule.Message;

            var existing = _friends.FirstOrDefault(f => string.Equals(f.Ip, ip, StringComparison.OrdinalIgnoreCase));
            return existing == null ? null : $"Esse IP já está na lista, como {existing.DisplayName}.";
        }

        private void ShowAddError(string? message)
        {
            TxtAddError.Text = message ?? string.Empty;
            TxtAddError.Visibility = message == null ? Visibility.Collapsed : Visibility.Visible;
        }

        // Valida ao sair do campo, não a cada tecla: erro aparecendo enquanto a pessoa ainda
        // digita só atrapalha. Campo vazio não é erro até tentar adicionar.
        private void TxtNewIp_LostFocus(object sender, RoutedEventArgs e)
        {
            string ip = TxtNewIp.Text.Trim();
            if (ip.Length > 0) ShowAddError(AddIpError(ip));
        }

        // Depois que o erro apareceu, ele some assim que o IP fica válido, já durante a digitação.
        private void TxtNewIp_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtAddError.Visibility != Visibility.Visible) return;
            string ip = TxtNewIp.Text.Trim();
            if (ip.Length == 0 || AddIpError(ip) == null) ShowAddError(null);
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Friend friend)
            {
                _friends.Remove(friend);
                Save();
            }
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
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Enter && (TxtNewName.IsKeyboardFocused || TxtNewIp.IsKeyboardFocused))
            {
                BtnAdd_Click(sender, e);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void Save()
        {
            FriendsService.SaveFriends(new List<Friend>(_friends));
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // O IP da lista só chega ao amigo ao sair do campo. Fechando pelo Esc o foco nunca
            // sai, e a última edição se perderia sem isto.
            if (Keyboard.FocusedElement is System.Windows.Controls.TextBox box)
                box.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _friends.CollectionChanged -= Friends_CollectionChanged;
            Save();
            base.OnClosed(e);
        }
    }

    /// <summary>
    /// IP editado direto na lista: inválido não chega ao amigo (nem é salvo nem sondado), e o
    /// campo fica com borda vermelha e a dica no tooltip até ser corrigido.
    /// </summary>
    public sealed class FriendIpRule : ValidationRule
    {
        internal const string Message = "Use o IP da Radmin VPN no formato 26.10.0.5.";

        public override ValidationResult Validate(object? value, CultureInfo cultureInfo)
            => FriendsService.IsValidFriendIp(value as string)
                ? ValidationResult.ValidResult
                : new ValidationResult(false, Message);
    }
}
