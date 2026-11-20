using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace StreamLiveApp
{
    public partial class PipWindow : Window
    {
        private const int WM_SIZING = 0x0214;

        // Códigos do wParam do WM_SIZING: de que borda o usuário está puxando.
        private const int WMSZ_LEFT = 1, WMSZ_RIGHT = 2, WMSZ_TOP = 3, WMSZ_TOPLEFT = 4,
            WMSZ_TOPRIGHT = 5, WMSZ_BOTTOM = 6, WMSZ_BOTTOMLEFT = 7, WMSZ_BOTTOMRIGHT = 8;

        /// <summary>Proporção antes do primeiro quadro: quase toda tela transmitida é 16:9.</summary>
        private const double DefaultAspect = 16.0 / 9.0;

        private readonly Action<float> _setVolume;

        /// <summary>
        /// Largura ÷ altura do vídeo. A janela é presa a ela: com o <c>Stretch=Uniform</c>, uma
        /// janela fora da proporção mostrava o vídeo com faixas pretas em volta.
        /// </summary>
        private double _aspect = DefaultAspect;

        public event Action OnRestoreRequested = delegate {};

        public PipWindow(WriteableBitmap bitmap, Action<float> setVolume)
        {
            InitializeComponent();
            _setVolume = setVolume;
            SetBitmap(bitmap);

            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 20;
            Top = workArea.Bottom - Height - 20;

            SourceInitialized += (_, _) => HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WndProc);
        }

        public void SetBitmap(WriteableBitmap bitmap)
        {
            PipVideo.Source = bitmap;

            // O host pode trocar de tela no meio da live (um monitor ultrawide, outro em pé):
            // a janela acompanha a proporção nova, mantendo a largura e o canto de cima.
            double aspect = AspectOf(bitmap?.PixelWidth ?? 0, bitmap?.PixelHeight ?? 0);
            if (Math.Abs(aspect - _aspect) < 0.001 && IsLoaded) return;
            _aspect = aspect;
            Height = Math.Max(MinHeight, HeightFor(Width, _aspect));
        }

        /// <summary>Proporção do vídeo; 16:9 enquanto ainda não chegou quadro.</summary>
        internal static double AspectOf(int videoWidth, int videoHeight) =>
            videoWidth > 0 && videoHeight > 0 ? (double)videoWidth / videoHeight : DefaultAspect;

        internal static double HeightFor(double width, double aspect) => Math.Round(width / aspect);

        /// <summary>
        /// Corrige o retângulo que o Windows propõe durante o arraste para a proporção do vídeo.
        /// Borda lateral: a largura manda e a altura acompanha; borda de cima/baixo: o contrário.
        /// Canto: manda o eixo que foi mais puxado, senão arrastar o canto só para baixo não faria
        /// nada. O lado oposto ao que está sendo puxado fica parado, como em qualquer janela.
        /// </summary>
        internal static Rect FitToAspect(Rect proposed, int edge, double aspect, double minWidth)
        {
            double width = edge switch
            {
                WMSZ_LEFT or WMSZ_RIGHT => proposed.Width,
                WMSZ_TOP or WMSZ_BOTTOM => proposed.Height * aspect,
                _ => Math.Max(proposed.Width, proposed.Height * aspect),
            };
            width = Math.Round(Math.Max(width, minWidth));
            double height = HeightFor(width, aspect);

            bool fromLeft = edge is WMSZ_LEFT or WMSZ_TOPLEFT or WMSZ_BOTTOMLEFT;
            bool fromTop = edge is WMSZ_TOP or WMSZ_TOPLEFT or WMSZ_TOPRIGHT;

            double left = fromLeft ? proposed.Right - width : proposed.Left;
            double top = fromTop ? proposed.Bottom - height : proposed.Top;
            return new Rect(left, top, width, height);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left, Top, Right, Bottom;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg != WM_SIZING) return IntPtr.Zero;

            var native = Marshal.PtrToStructure<NativeRect>(lParam);
            var proposed = new Rect(native.Left, native.Top, native.Right - native.Left, native.Bottom - native.Top);

            // O WM_SIZING chega em pixels físicos e o MinWidth é em unidades do WPF: converte
            // pela escala do monitor, senão em 150% a janela pararia de encolher antes da hora.
            double dpiScale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            var fitted = FitToAspect(proposed, wParam.ToInt32(), _aspect, MinWidth * dpiScale);

            native.Left = (int)fitted.Left;
            native.Top = (int)fitted.Top;
            native.Right = (int)fitted.Right;
            native.Bottom = (int)fitted.Bottom;
            Marshal.StructureToPtr(native, lParam, false);

            handled = true;
            return new IntPtr(1);
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                OnRestoreRequested?.Invoke();
            }
            else
            {
                try { DragMove(); } catch { }
            }
        }

        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            PipControls.Visibility = Visibility.Visible;
        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            PipControls.Visibility = Visibility.Collapsed;
        }

        private void SliderPipVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _setVolume?.Invoke((float)(e.NewValue / 100.0));
        }
    }
}
