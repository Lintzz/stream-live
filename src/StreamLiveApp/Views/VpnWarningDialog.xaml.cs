using System.Windows;
using System.Windows.Input;
using StreamLiveApp.Services;

namespace StreamLiveApp
{
    /// <summary>
    /// Aviso mostrado na abertura quando a Radmin VPN está fechada. Oferece abri-la em vez de
    /// só reclamar: o caminho normal é o usuário ter esquecido de subir a VPN.
    /// </summary>
    public partial class VpnWarningDialog : Window
    {
        private VpnWarningDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Com o Radmin instalado o modal oferece abrir; sem ele vira só aviso — não adianta
        /// um botão que nunca vai funcionar.
        /// </summary>
        public static VpnWarningDialog Create(string? executablePath)
        {
            var dialog = new VpnWarningDialog();

            if (executablePath == null)
            {
                dialog.BtnOpen.Visibility = Visibility.Collapsed;
                dialog.TxtNotInstalled.Visibility = Visibility.Visible;
                dialog.BtnIgnore.Content = "Entendi";
            }

            return dialog;
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            if (!VpnStatusService.TryStart())
            {
                // Falha comum: o usuário recusou o UAC. Mantém o modal aberto para tentar de novo.
                TxtError.Text = "Não foi possível abrir a Radmin VPN. Abra-a manualmente.";
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnIgnore_Click(object sender, RoutedEventArgs e)
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
            if (e.Key == Key.Enter && BtnOpen.Visibility == Visibility.Visible) BtnOpen_Click(sender, e);
            else if (e.Key == Key.Escape) BtnIgnore_Click(sender, e);
        }
    }
}
