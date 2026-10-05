using System.Windows;
using System.Windows.Input;

namespace StreamLiveApp
{
    /// <summary>
    /// Confirmação de ação destrutiva, no visual do app. Existe porque o MessageBox do Windows
    /// só oferece "Sim/Não": aqui o botão repete a ação ("Encerrar e fechar"), tem cor de
    /// perigo, e o foco começa em Cancelar — Enter apertado por reflexo não confirma nada.
    /// Para o que dá para desfazer, prefira "Desfazer" (ver a remoção de amigos).
    /// </summary>
    public partial class ConfirmDialog : Window
    {
        private ConfirmDialog()
        {
            InitializeComponent();
            Loaded += (s, e) => BtnCancel.Focus();
        }

        /// <summary>Mostra e devolve true só se a pessoa confirmou.</summary>
        public static bool Ask(Window owner, string title, string message, string confirmText)
        {
            var dialog = new ConfirmDialog { Owner = owner, Title = title };
            dialog.TxtTitle.Text = title;
            dialog.TxtMessage.Text = message;
            dialog.BtnConfirm.Content = confirmText;
            return dialog.ShowDialog() == true;
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
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
            if (e.Key == Key.Escape) BtnCancel_Click(sender, e);
        }
    }
}
