using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using StreamLiveApp.Models;

namespace StreamLiveApp
{
    /// <summary>
    /// Modo demonstração (<c>--demo</c>), usado para gravar o vídeo e os prints do README.
    ///
    /// Existe porque a tela de verdade só mostra algo com amigos reais na VPN — com IPs reais
    /// na lista e o desktop de quem grava dentro da live. Aqui os amigos são fictícios, o
    /// status deles é fixo, as lives são cenas desenhadas pelo próprio app, e nada é gravado
    /// em disco nem sai pela rede: o servidor de sinalização não sobe, então uma instância de
    /// verdade aberta ao lado continua dona da porta 8080.
    /// </summary>
    public static class DemoMode
    {
        public static bool IsEnabled { get; private set; }

        public static void Enable() => IsEnabled = true;

        /// <summary>
        /// Amigos de mentira, cobrindo os quatro estados da lista. Os IPs ficam na faixa da
        /// Radmin (26.x) para a tela parecer a de sempre, mas nenhum deles é contatado.
        /// </summary>
        public static List<Friend> CreateFriends() => new()
        {
            new Friend { Name = "Ana",   Ip = "26.10.0.11", IsOnline = true,  IsStreaming = true },
            new Friend { Name = "Diego", Ip = "26.10.0.14", IsOnline = true,  IsStreaming = true },
            new Friend { Name = "Bruno", Ip = "26.10.0.12", IsOnline = true,  IsStreaming = false },
            new Friend { Name = "Carla", Ip = "26.10.0.13", IsOnline = false, IsStreaming = false },
            new Friend { Name = "Lia",   Ip = "26.10.0.15", IsOnline = false, IsStreaming = false },
        };

        /// <summary>Cena de cada amigo: duas lives abertas lado a lado não podem ser iguais.</summary>
        public static DemoScene SceneFor(Friend friend) =>
            friend.Name == "Diego" ? DemoScene.Espaco : DemoScene.Corrida;
    }

    public enum DemoScene { Corrida, Espaco, Plataforma }

    /// <summary>
    /// Gera quadros animados de uma cena, no formato que cada ponta espera: BGR24 no viewer
    /// (o mesmo que o decoder entrega) e BGR32 no preview do host (o mesmo da captura).
    /// </summary>
    public sealed class DemoFeed : IDisposable
    {
        private const int Width = 960;
        private const int Height = 540;
        private const int FrameIntervalMs = 33;

        private readonly DemoScene _scene;
        private readonly bool _rgb32;
        private readonly System.Threading.Timer _timer;
        private readonly DateTime _start = DateTime.UtcNow;
        private readonly object _lock = new();
        private bool _disposed;

        public event Action<byte[], int, int>? FrameReady;

        public DemoFeed(DemoScene scene, bool rgb32)
        {
            _scene = scene;
            _rgb32 = rgb32;
            _timer = new System.Threading.Timer(_ => Tick(), null, 0, FrameIntervalMs);
        }

        private void Tick()
        {
            // Sem a trava, um quadro lento sobrepõe o seguinte e os dois disputam o Bitmap.
            if (!System.Threading.Monitor.TryEnter(_lock)) return;
            try
            {
                if (_disposed) return;
                var t = (DateTime.UtcNow - _start).TotalSeconds;
                var pixels = Render(_scene, t, _rgb32);
                FrameReady?.Invoke(pixels, Width, Height);
            }
            catch { }
            finally
            {
                System.Threading.Monitor.Exit(_lock);
            }
        }

        private static byte[] Render(DemoScene scene, double t, bool rgb32)
        {
            var format = rgb32 ? PixelFormat.Format32bppRgb : PixelFormat.Format24bppRgb;
            using var bmp = new Bitmap(Width, Height, format);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                switch (scene)
                {
                    case DemoScene.Espaco: DrawSpace(g, t); break;
                    case DemoScene.Plataforma: DrawPlatformer(g, t); break;
                    default: DrawRace(g, t); break;
                }
            }

            var data = bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, format);
            try
            {
                var rowBytes = Width * (rgb32 ? 4 : 3);
                var pixels = new byte[rowBytes * Height];
                for (int y = 0; y < Height; y++)
                {
                    Marshal.Copy(data.Scan0 + y * data.Stride, pixels, y * rowBytes, rowBytes);
                }
                return pixels;
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        // ───────────────────────────── Cenas ─────────────────────────────

        private static void DrawRace(Graphics g, double t)
        {
            int horizon = 250;

            using (var sky = new LinearGradientBrush(new Rectangle(0, 0, Width, horizon + 1),
                       Color.FromArgb(32, 18, 64), Color.FromArgb(255, 120, 90), 90f))
                g.FillRectangle(sky, 0, 0, Width, horizon + 1);

            using (var sun = new SolidBrush(Color.FromArgb(255, 214, 110)))
                g.FillEllipse(sun, Width / 2 - 70, horizon - 95, 140, 140);

            // Montanhas em duas camadas, a de trás mais lenta.
            DrawMountains(g, horizon, t * 8, 70, Color.FromArgb(88, 42, 98));
            DrawMountains(g, horizon, t * 20, 40, Color.FromArgb(52, 24, 70));

            using (var ground = new SolidBrush(Color.FromArgb(24, 16, 36)))
                g.FillRectangle(ground, 0, horizon, Width, Height - horizon);

            // Pista em perspectiva com faixas correndo na direção da câmera.
            var cx = Width / 2f + (float)Math.Sin(t * 0.7) * 40;
            var road = new[]
            {
                new PointF(cx - 18, horizon), new PointF(cx + 18, horizon),
                new PointF(Width / 2f + 420, Height), new PointF(Width / 2f - 420, Height)
            };
            using (var asphalt = new SolidBrush(Color.FromArgb(44, 40, 56))) g.FillPolygon(asphalt, road);
            using (var edge = new Pen(Color.FromArgb(255, 82, 120), 4))
            {
                g.DrawLine(edge, road[0], road[3]);
                g.DrawLine(edge, road[1], road[2]);
            }

            using (var stripe = new SolidBrush(Color.FromArgb(240, 240, 255)))
            {
                for (int i = 0; i < 8; i++)
                {
                    var z = ((i / 8.0) + t * 1.4) % 1.0;
                    var z2 = Math.Min(1.0, z + 0.05);
                    float Y(double d) => horizon + (float)(d * d * (Height - horizon));
                    float X(double d) => cx + (float)((Width / 2f - cx) * d * d);
                    float W(double d) => (float)(2 + d * d * 16);
                    g.FillPolygon(stripe, new[]
                    {
                        new PointF(X(z) - W(z), Y(z)), new PointF(X(z) + W(z), Y(z)),
                        new PointF(X(z2) + W(z2), Y(z2)), new PointF(X(z2) - W(z2), Y(z2))
                    });
                }
            }

            // Carro do jogador.
            var carX = Width / 2f - 60 + (float)Math.Sin(t * 1.3) * 28;
            using (var shadow = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                g.FillEllipse(shadow, carX - 6, 478, 132, 26);
            using (var body = new SolidBrush(Color.FromArgb(0, 148, 242)))
                FillRounded(g, body, carX, 420, 120, 66, 14);
            using (var glass = new SolidBrush(Color.FromArgb(20, 30, 60)))
                FillRounded(g, glass, carX + 20, 428, 80, 22, 6);
            using (var light = new SolidBrush(Color.FromArgb(255, 82, 82)))
            {
                g.FillRectangle(light, carX + 8, 462, 22, 8);
                g.FillRectangle(light, carX + 90, 462, 22, 8);
            }

            var speed = 182 + (int)(Math.Sin(t * 0.9) * 9);
            DrawHud(g, $"{speed} km/h", "VOLTA 2/3", "1º");
        }

        private static void DrawMountains(Graphics g, int horizon, double offset, int height, Color color)
        {
            var points = new List<PointF> { new(0, horizon) };
            for (int x = 0; x <= Width; x += 40)
            {
                var k = (x + offset) * 0.012;
                var y = horizon - height * (0.55 + 0.45 * Math.Sin(k) * Math.Cos(k * 0.37));
                points.Add(new PointF(x, (float)y));
            }
            points.Add(new PointF(Width, horizon));
            using var brush = new SolidBrush(color);
            g.FillPolygon(brush, points.ToArray());
        }

        private static void DrawSpace(Graphics g, double t)
        {
            g.Clear(Color.FromArgb(6, 8, 20));

            // Estrelas em três camadas de paralaxe, geradas de forma determinística.
            for (int layer = 1; layer <= 3; layer++)
            {
                var rng = new Random(layer * 97);
                using var star = new SolidBrush(Color.FromArgb(80 + layer * 55, 220, 230, 255));
                for (int i = 0; i < 70; i++)
                {
                    var x = (float)((rng.NextDouble() * Width - t * 30 * layer) % Width);
                    if (x < 0) x += Width;
                    var y = (float)(rng.NextDouble() * Height);
                    g.FillEllipse(star, x, y, layer, layer);
                }
            }

            // Planeta com anel.
            var px = 690f; var py = 170f;
            using (var planet = new LinearGradientBrush(new RectangleF(px - 110, py - 110, 220, 220),
                       Color.FromArgb(120, 90, 255), Color.FromArgb(30, 10, 80), 45f))
                g.FillEllipse(planet, px - 110, py - 110, 220, 220);
            using (var ring = new Pen(Color.FromArgb(160, 190, 170, 255), 6))
                g.DrawEllipse(ring, px - 180, py - 26, 360, 52);

            // Nave balançando.
            var sx = 260f + (float)Math.Sin(t * 0.8) * 60;
            var sy = 320f + (float)Math.Sin(t * 1.7) * 30;
            using (var flame = new SolidBrush(Color.FromArgb(255, 160, 60)))
                g.FillPolygon(flame, new[]
                {
                    new PointF(sx - 40, sy - 10), new PointF(sx - 70 - (float)(Math.Sin(t * 20) * 10), sy),
                    new PointF(sx - 40, sy + 10)
                });
            using (var hull = new SolidBrush(Color.FromArgb(230, 236, 245)))
                g.FillPolygon(hull, new[]
                {
                    new PointF(sx + 60, sy), new PointF(sx - 40, sy - 30), new PointF(sx - 25, sy),
                    new PointF(sx - 40, sy + 30)
                });
            using (var cockpit = new SolidBrush(Color.FromArgb(0, 148, 242)))
                g.FillEllipse(cockpit, sx + 4, sy - 8, 26, 16);

            // Tiros cruzando a tela.
            using (var laser = new Pen(Color.FromArgb(0, 210, 106), 3))
            {
                for (int i = 0; i < 3; i++)
                {
                    var lx = (float)((t * 600 + i * 320) % (Width + 200)) - 100;
                    g.DrawLine(laser, lx, 120 + i * 140, lx + 40, 120 + i * 140);
                }
            }

            var shield = 82 - (int)((t * 2) % 20);
            DrawHud(g, $"ESCUDO {shield}%", "SETOR 7", "x3");
        }

        private static void DrawPlatformer(Graphics g, double t)
        {
            using (var sky = new LinearGradientBrush(new Rectangle(0, 0, Width, Height),
                       Color.FromArgb(110, 190, 255), Color.FromArgb(200, 235, 255), 90f))
                g.FillRectangle(sky, 0, 0, Width, Height);

            using (var cloud = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
            {
                for (int i = 0; i < 4; i++)
                {
                    var x = (float)((i * 300 - t * 25) % (Width + 300));
                    if (x < -200) x += Width + 300;
                    var y = 60 + (i % 2) * 70;
                    g.FillEllipse(cloud, x, y, 120, 44);
                    g.FillEllipse(cloud, x + 40, y - 20, 90, 54);
                }
            }

            // Chão de blocos rolando.
            int groundY = 430;
            var scroll = (float)((t * 120) % 60);
            using (var grass = new SolidBrush(Color.FromArgb(76, 175, 80)))
            using (var dirt = new SolidBrush(Color.FromArgb(121, 85, 72)))
            using (var line = new Pen(Color.FromArgb(90, 60, 50), 2))
            {
                g.FillRectangle(dirt, 0, groundY, Width, Height - groundY);
                g.FillRectangle(grass, 0, groundY, Width, 18);
                for (float x = -scroll; x < Width; x += 60) g.DrawLine(line, x, groundY + 18, x, Height);
            }

            // Plataformas e moedas passando.
            using (var block = new SolidBrush(Color.FromArgb(255, 167, 38)))
            using (var coin = new SolidBrush(Color.FromArgb(255, 214, 0)))
            {
                for (int i = 0; i < 3; i++)
                {
                    var x = (float)((i * 380 + 500 - t * 120) % (Width + 380));
                    if (x < -380) x += Width + 380;
                    g.FillRectangle(block, x, 300 - i * 20, 150, 26);
                    var bob = (float)Math.Sin(t * 4 + i) * 6;
                    g.FillEllipse(coin, x + 60, 255 - i * 20 + bob, 26, 26);
                }
            }

            // Personagem pulando.
            var jump = (float)Math.Abs(Math.Sin(t * 2.6)) * 120;
            var py = groundY - 64 - jump;
            using (var hero = new SolidBrush(Color.FromArgb(229, 57, 53)))
                FillRounded(g, hero, 220, py, 56, 64, 12);
            using (var eye = new SolidBrush(Color.White))
                g.FillEllipse(eye, 252, py + 14, 14, 16);
            using (var pupil = new SolidBrush(Color.Black))
                g.FillEllipse(pupil, 258, py + 19, 7, 8);

            var coins = 12 + (int)(t / 1.5);
            DrawHud(g, $"★ {coins}", "FASE 1-2", "♥ 3");
        }

        private static void DrawHud(Graphics g, string left, string center, string right)
        {
            using var font = new Font("Segoe UI", 20, FontStyle.Bold, GraphicsUnit.Pixel);
            using var panel = new SolidBrush(Color.FromArgb(140, 0, 0, 0));
            using var text = new SolidBrush(Color.White);

            void Tag(string s, float x, bool alignRight = false, bool center2 = false)
            {
                var size = g.MeasureString(s, font);
                var w = size.Width + 24;
                var px = center2 ? (Width - w) / 2 : alignRight ? Width - w - x : x;
                FillRounded(g, panel, px, 18, w, 38, 10);
                g.DrawString(s, font, text, px + 12, 18 + (38 - size.Height) / 2);
            }

            Tag(left, 18);
            Tag(center, 0, center2: true);
            Tag(right, 18, alignRight: true);
        }

        private static void FillRounded(Graphics g, Brush brush, float x, float y, float w, float h, float r)
        {
            using var path = new GraphicsPath();
            var d = r * 2;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
            }
            _timer.Dispose();
        }
    }
}
