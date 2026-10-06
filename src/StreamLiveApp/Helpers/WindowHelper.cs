using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace StreamLiveApp
{
    public class CaptureSource
    {
        public string Title { get; set; } = string.Empty;
        public System.Drawing.Rectangle ScreenBounds { get; set; }

        /// <summary>
        /// A tela principal do Windows é a que começa em (0,0) nas coordenadas da área de
        /// trabalho virtual — dá para saber sem perguntar ao Windows de novo.
        /// </summary>
        public bool IsPrimary => ScreenBounds.Left == 0 && ScreenBounds.Top == 0;

        /// <summary>"1920 × 1080 · principal", embaixo da miniatura no seletor de tela.</summary>
        public string Detail => IsPrimary
            ? $"{ScreenBounds.Width} × {ScreenBounds.Height} · principal"
            : $"{ScreenBounds.Width} × {ScreenBounds.Height}";

        public override string ToString() => Title;
    }

    public static class WindowHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        /// <summary>
        /// Lista os monitores disponíveis para captura. A transmissão é sempre de tela inteira;
        /// captura de janelas individuais não é suportada.
        /// </summary>
        public static List<CaptureSource> GetCapturableScreens()
        {
            var sources = new List<CaptureSource>();

            int screenIndex = 1;
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
                {
                    sources.Add(new CaptureSource
                    {
                        Title = $"Tela {screenIndex}",
                        ScreenBounds = new System.Drawing.Rectangle(
                            lprcMonitor.left,
                            lprcMonitor.top,
                            lprcMonitor.right - lprcMonitor.left,
                            lprcMonitor.bottom - lprcMonitor.top)
                    });
                    screenIndex++;
                    return true;
                }, IntPtr.Zero);

            return sources;
        }

        /// <summary>
        /// Tamanho da miniatura que cabe em <paramref name="maxWidth"/> × <paramref name="maxHeight"/>
        /// sem distorcer a tela (monitor em pé, ultrawide).
        /// </summary>
        internal static System.Drawing.Size ThumbnailSize(System.Drawing.Size screen, int maxWidth, int maxHeight)
        {
            if (screen.Width <= 0 || screen.Height <= 0 || maxWidth <= 0 || maxHeight <= 0)
                return System.Drawing.Size.Empty;

            double scale = Math.Min((double)maxWidth / screen.Width, (double)maxHeight / screen.Height);
            return new System.Drawing.Size(
                Math.Max(1, (int)Math.Round(screen.Width * scale)),
                Math.Max(1, (int)Math.Round(screen.Height * scale)));
        }

        /// <summary>
        /// Foto reduzida de uma tela, para o seletor de tela. Pelo GDI, como o fallback da
        /// captura: jogo em tela cheia exclusiva sai preto aqui, mas a live (DXGI) pega normal.
        /// Devolve <c>null</c> em vez de lançar — miniatura que falha vira um bloco com o nome
        /// da tela, e não pode derrubar o modal.
        /// </summary>
        internal static System.Windows.Media.Imaging.BitmapSource? CaptureThumbnail(
            System.Drawing.Rectangle bounds, int maxWidth, int maxHeight)
        {
            var size = ThumbnailSize(bounds.Size, maxWidth, maxHeight);
            if (size.IsEmpty) return null;

            try
            {
                using var full = new System.Drawing.Bitmap(bounds.Width, bounds.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                using (var g = System.Drawing.Graphics.FromImage(full))
                {
                    g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
                }

                return Downscale(full, size);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Reduz um quadro BGR32 já pronto (o --demo desenha as telas em vez de fotografar).</summary>
        internal static System.Windows.Media.Imaging.BitmapSource? ThumbnailFromPixels(
            byte[] bgr32, int width, int height, int maxWidth, int maxHeight)
        {
            var size = ThumbnailSize(new System.Drawing.Size(width, height), maxWidth, maxHeight);
            if (size.IsEmpty) return null;

            try
            {
                var handle = GCHandle.Alloc(bgr32, GCHandleType.Pinned);
                try
                {
                    using var full = new System.Drawing.Bitmap(width, height, width * 4,
                        System.Drawing.Imaging.PixelFormat.Format32bppRgb, handle.AddrOfPinnedObject());
                    return Downscale(full, size);
                }
                finally
                {
                    handle.Free();
                }
            }
            catch
            {
                return null;
            }
        }

        private static System.Windows.Media.Imaging.BitmapSource Downscale(System.Drawing.Bitmap full, System.Drawing.Size size)
        {
            using var small = new System.Drawing.Bitmap(size.Width, size.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            using (var g = System.Drawing.Graphics.FromImage(small))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                g.DrawImage(full, 0, 0, size.Width, size.Height);
            }

            var rect = new System.Drawing.Rectangle(0, 0, size.Width, size.Height);
            var data = small.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            try
            {
                var source = System.Windows.Media.Imaging.BitmapSource.Create(
                    size.Width, size.Height, 96, 96, System.Windows.Media.PixelFormats.Bgr32, null,
                    data.Scan0, data.Stride * size.Height, data.Stride);
                // Congelada para poder sair da thread de captura e ir para a de UI.
                source.Freeze();
                return source;
            }
            finally
            {
                small.UnlockBits(data);
            }
        }
    }
}
