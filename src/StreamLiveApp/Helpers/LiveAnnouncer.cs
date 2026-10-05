using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace StreamLiveApp
{
    /// <summary>
    /// Faz o leitor de tela anunciar um texto que muda sozinho (status da live, aviso na barra,
    /// erro de campo). No WPF, marcar <c>AutomationProperties.LiveSetting</c> não basta: alguém
    /// precisa disparar o <c>LiveRegionChanged</c> a cada mudança, senão o Narrador nunca fala.
    /// Uso no XAML: <c>local:LiveAnnouncer.Mode="Polite"</c> (ou "Assertive" para erro).
    /// </summary>
    public static class LiveAnnouncer
    {
        public static readonly DependencyProperty ModeProperty = DependencyProperty.RegisterAttached(
            "Mode", typeof(AutomationLiveSetting), typeof(LiveAnnouncer),
            new PropertyMetadata(AutomationLiveSetting.Off, OnModeChanged));

        public static void SetMode(DependencyObject element, AutomationLiveSetting value) => element.SetValue(ModeProperty, value);
        public static AutomationLiveSetting GetMode(DependencyObject element) => (AutomationLiveSetting)element.GetValue(ModeProperty);

        private static readonly DependencyPropertyDescriptor TextDescriptor =
            DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBlock text) return;

            AutomationProperties.SetLiveSetting(text, (AutomationLiveSetting)e.NewValue);

            text.Loaded -= Subscribe;
            text.Unloaded -= Unsubscribe;
            Unsubscribe(text, EventArgs.Empty);
            if ((AutomationLiveSetting)e.NewValue == AutomationLiveSetting.Off) return;

            // Inscrição só enquanto está na tela: o AddValueChanged segura o elemento, e os
            // quadros de live (StreamTab) entram e saem o tempo todo.
            text.Loaded += Subscribe;
            text.Unloaded += Unsubscribe;
            if (text.IsLoaded) Subscribe(text, EventArgs.Empty);
        }

        private static void Subscribe(object? sender, EventArgs e)
        {
            if (sender is TextBlock text) TextDescriptor.AddValueChanged(text, Announce);
        }

        private static void Unsubscribe(object? sender, EventArgs e)
        {
            if (sender is TextBlock text) TextDescriptor.RemoveValueChanged(text, Announce);
        }

        private static void Announce(object? sender, EventArgs e)
        {
            if (sender is not TextBlock text) return;

            // Depois do layout: é comum o código trocar o texto e só em seguida tornar o
            // elemento visível (erro de campo, aviso flutuante). Anunciado na hora, o evento
            // saía de um elemento ainda recolhido e o leitor de tela o descartava.
            text.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (string.IsNullOrEmpty(text.Text) || !text.IsVisible) return;
                var peer = UIElementAutomationPeer.FromElement(text) ?? UIElementAutomationPeer.CreatePeerForElement(text);
                peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}
