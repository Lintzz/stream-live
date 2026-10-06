using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace StreamLiveApp
{
    /// <summary>
    /// Diagnóstico detalhado da live, segundo a segundo, em <c>diagnostico-detalhado.log</c>.
    ///
    /// Existe por causa das "travadinhas" do começo de uma live (2026-10-06): o log normal só
    /// dizia "N pacotes perdidos nos últimos 10s" e não respondia o que importava — por qual
    /// caminho de rede o vídeo passou (Radmin, IPv6 público, Teredo), se o congelamento foi
    /// espera de keyframe ou rede parada, e em que segundo exato.
    ///
    /// Só entra no binário compilado com <c>-p:DiagDetalhado=true</c> (símbolo
    /// <c>DIAG_DETALHADO</c>): todo ponto de entrada é <see cref="ConditionalAttribute"/>, então
    /// na build pública o compilador apaga as chamadas — e a avaliação dos argumentos junto.
    /// O dono quis assim: o instalador publicado não grava nada disto.
    /// <c>DetailedDiagnosticsTests.PublicBuildDoesNotCompileItIn</c> trava isso.
    /// </summary>
    public static class DetailedDiagnostics
    {
        private const string Symbol = "DIAG_DETALHADO";
        private const string FileName = "diagnostico-detalhado.log";
        private const string BackupFileName = "diagnostico-detalhado.1.log";
        private const long MaxBytes = 20 * 1024 * 1024;

        /// <summary>Abaixo disso a troca de quadro é normal (a 30 fps, 2 quadros pulados já são ~100 ms).</summary>
        internal const int FreezeThresholdMs = 250;

        /// <summary>Esta build grava o diagnóstico detalhado?</summary>
        internal static bool IsCompiledIn =>
#if DIAG_DETALHADO
            true;
#else
            false;
#endif

        public static string FilePath => AppPaths.GetFilePath(FileName);

        private static readonly object Gate = new();
        private static readonly Dictionary<string, Channel> Channels = new(StringComparer.Ordinal);
        private static System.Threading.Timer? _timer;
        private static bool _rotationChecked;

        /// <summary>Contadores de uma live (assistida ou a sua), zerados a cada segundo.</summary>
        private sealed class Channel
        {
            public readonly object Lock = new();
            public readonly bool IsHost;
            public Channel(bool isHost) { IsHost = isHost; }

            // viewer
            public int Packets, PacketBytes, Lost, Late, Frames, FrameBytes, Shown, Idr, IdrBytes;
            public int DecodeFailures, DecodeCount, KeyFrameRequests, KeyFrameRequestsSuppressed, AudioFrames;
            public double DecodeMsTotal, DecodeMsMax;
            public long LastPacketTicks, MaxPacketGapTicks;
            public long LastShownTicks, MaxPacketGapSinceShownTicks;
            public bool HoldingNow, HeldSinceShown;
            public long HoldStartTicks;
            public int LostSinceShown;
            public int RttMs = -1;
            public Func<string>? Sampler;

            // host
            public int Captured, Encoded, SkippedBusy, EncodedBytes, HostIdr, HostIdrBytes, RequestsReceived, RequestsServed;
            public double EncodeMsTotal, EncodeMsMax;
            public readonly List<string> IdrReasons = new();
        }

        // ───────────────────────────── Início e marca ─────────────────────────────

        [Conditional(Symbol)]
        public static void Start()
        {
            lock (Gate)
            {
                if (_timer != null) return;
                _timer = new System.Threading.Timer(_ => FlushSecond(), null, 1000, 1000);
            }
            Write("Sessao", $"diagnostico detalhado ligado | versao={AppInfo.Version} | uma linha por segundo por live; " +
                            $"congelamentos acima de {FreezeThresholdMs} ms viram evento; F8 grava uma marca");
        }

        /// <summary>F8 na janela: o dono marca o momento da travadinha em vez de anotar a hora.</summary>
        [Conditional(Symbol)]
        public static void Mark(string note) => Write("MARCA", "===== " + note + " =====");

        [Conditional(Symbol)]
        public static void Event(string label, string message) => Write(label, message);

        // ───────────────────────────── Viewer ─────────────────────────────

        [Conditional(Symbol)]
        public static void ViewerPacket(string label, int bytes, int lostBefore, bool late)
        {
            var c = Get(label, false);
            long now = Stopwatch.GetTimestamp();
            lock (c.Lock)
            {
                if (late) { c.Late++; return; }
                c.Packets++;
                c.PacketBytes += bytes;
                c.Lost += lostBefore;
                c.LostSinceShown += lostBefore;
                if (c.LastPacketTicks != 0)
                {
                    long gap = now - c.LastPacketTicks;
                    if (gap > c.MaxPacketGapTicks) c.MaxPacketGapTicks = gap;
                    if (gap > c.MaxPacketGapSinceShownTicks) c.MaxPacketGapSinceShownTicks = gap;
                }
                c.LastPacketTicks = now;
            }
        }

        [Conditional(Symbol)]
        public static void ViewerFrame(string label, int bytes, bool idr)
        {
            var c = Get(label, false);
            int holdMs = -1;
            lock (c.Lock)
            {
                c.Frames++;
                c.FrameBytes += bytes;
                if (idr)
                {
                    c.Idr++;
                    c.IdrBytes += bytes;
                    if (c.HoldingNow) holdMs = ToMs(Stopwatch.GetTimestamp() - c.HoldStartTicks);
                }
            }
            if (holdMs >= 0) Write(label, $"keyframe chegou ({bytes / 1024} KB) depois de {holdMs} ms com a imagem retida");
        }

        [Conditional(Symbol)]
        public static void ViewerDecoded(string label, double ms, bool ok)
        {
            var c = Get(label, false);
            lock (c.Lock)
            {
                if (!ok) { c.DecodeFailures++; return; }
                c.DecodeCount++;
                c.DecodeMsTotal += ms;
                if (ms > c.DecodeMsMax) c.DecodeMsMax = ms;
            }
        }

        /// <summary>Quadro foi para a tela: fecha a conta de um possível congelamento.</summary>
        [Conditional(Symbol)]
        public static void ViewerShown(string label)
        {
            var c = Get(label, false);
            long now = Stopwatch.GetTimestamp();
            string? freeze = null;
            lock (c.Lock)
            {
                c.Shown++;
                if (c.LastShownTicks != 0)
                {
                    int gapMs = ToMs(now - c.LastShownTicks);
                    if (gapMs >= FreezeThresholdMs)
                    {
                        freeze = $"CONGELOU {gapMs} ms — " +
                                 DescribeFreeze(c.HeldSinceShown, ToMs(c.MaxPacketGapSinceShownTicks), c.LostSinceShown);
                    }
                }
                c.LastShownTicks = now;
                c.MaxPacketGapSinceShownTicks = 0;
                c.LostSinceShown = 0;
                c.HeldSinceShown = c.HoldingNow;
            }
            if (freeze != null) Write(label, freeze);
        }

        [Conditional(Symbol)]
        public static void ViewerHold(string label, bool holding)
        {
            var c = Get(label, false);
            lock (c.Lock)
            {
                if (holding && !c.HoldingNow) c.HoldStartTicks = Stopwatch.GetTimestamp();
                c.HoldingNow = holding;
                if (holding) c.HeldSinceShown = true;
            }
        }

        [Conditional(Symbol)]
        public static void ViewerKeyFrameRequest(string label, bool sent)
        {
            var c = Get(label, false);
            lock (c.Lock)
            {
                if (sent) c.KeyFrameRequests++;
                else c.KeyFrameRequestsSuppressed++;
            }
        }

        [Conditional(Symbol)]
        public static void ViewerAudio(string label)
        {
            var c = Get(label, false);
            lock (c.Lock) c.AudioFrames++;
        }

        [Conditional(Symbol)]
        public static void ViewerRtt(string label, int ms)
        {
            var c = Get(label, false);
            lock (c.Lock) c.RttMs = ms;
        }

        /// <summary>Algo que só o dono da live sabe ler na hora do segundo (ex.: buffer de áudio).</summary>
        [Conditional(Symbol)]
        public static void SetSampler(string label, bool isHost, Func<string> sampler)
        {
            var c = Get(label, isHost);
            lock (c.Lock) c.Sampler = sampler;
        }

        [Conditional(Symbol)]
        public static void Forget(string label)
        {
            lock (Gate) Channels.Remove(label);
        }

        // ───────────────────────────── Host ─────────────────────────────

        [Conditional(Symbol)]
        public static void HostCaptured(string label)
        {
            var c = Get(label, true);
            lock (c.Lock) c.Captured++;
        }

        [Conditional(Symbol)]
        public static void HostSkippedBusy(string label)
        {
            var c = Get(label, true);
            lock (c.Lock) c.SkippedBusy++;
        }

        [Conditional(Symbol)]
        public static void HostEncoded(string label, int bytes, double ms, string? idrReason)
        {
            var c = Get(label, true);
            lock (c.Lock)
            {
                c.Encoded++;
                c.EncodedBytes += bytes;
                c.EncodeMsTotal += ms;
                if (ms > c.EncodeMsMax) c.EncodeMsMax = ms;
                if (idrReason != null)
                {
                    c.HostIdr++;
                    c.HostIdrBytes += bytes;
                    c.IdrReasons.Add(idrReason);
                }
            }
        }

        [Conditional(Symbol)]
        public static void HostKeyFrameRequest(string label, bool served)
        {
            var c = Get(label, true);
            lock (c.Lock)
            {
                c.RequestsReceived++;
                if (served) c.RequestsServed++;
            }
        }

        // ───────────────────────────── Regras puras (testadas) ─────────────────────────────

        /// <summary>
        /// Por onde o vídeo passou, pelo endereço do candidato ICE escolhido. Teredo é o caso que
        /// motivou isto: é um túnel IPv6 do Windows por servidores de terceiros, e perde muito.
        /// </summary>
        internal static string ClassifyAddress(string? address)
        {
            if (string.IsNullOrWhiteSpace(address)) return "desconhecido";
            var a = address.Trim().Trim('[', ']').ToLowerInvariant();

            if (a.StartsWith("26.") || a.StartsWith("fdfd:")) return "Radmin VPN";
            if (a.StartsWith("2001:0:") || a.StartsWith("2001::")) return "Teredo (túnel IPv6 do Windows)";
            if (a == "127.0.0.1" || a == "::1") return "esta máquina";
            if (a.StartsWith("192.168.") || a.StartsWith("10.") || IsPrivate172(a)) return "rede local";
            if (a.StartsWith("fe80:")) return "IPv6 local de enlace";
            if (a.Contains(':')) return "IPv6 público (internet)";
            return "IPv4 público (internet)";
        }

        private static bool IsPrivate172(string a)
        {
            if (!a.StartsWith("172.")) return false;
            var parts = a.Split('.');
            return parts.Length > 1 && int.TryParse(parts[1], out var second) && second >= 16 && second <= 31;
        }

        /// <summary>
        /// Motivo provável de um congelamento, pelo que aconteceu entre os dois quadros
        /// exibidos: imagem retida esperando keyframe (perda de pacote), rede parada (nenhum
        /// pacote chegando) ou nenhum dos dois (decodificação ou o host que parou de mandar).
        /// </summary>
        internal static string DescribeFreeze(bool heldForKeyFrame, int maxPacketGapMs, int lostPackets)
        {
            var parts = new List<string>();
            if (heldForKeyFrame) parts.Add("imagem retida esperando keyframe");
            if (lostPackets > 0) parts.Add($"{lostPackets} pacotes perdidos");
            if (maxPacketGapMs >= FreezeThresholdMs) parts.Add($"rede parada: {maxPacketGapMs} ms sem nenhum pacote chegar");

            if (parts.Count == 0)
                return $"pacotes chegando normalmente (maior intervalo {maxPacketGapMs} ms) — host parou de mandar quadros ou decodificação lenta";
            return string.Join("; ", parts);
        }

        // ───────────────────────────── Escrita ─────────────────────────────

        private static Channel Get(string label, bool isHost)
        {
            lock (Gate)
            {
                if (!Channels.TryGetValue(label, out var c))
                {
                    c = new Channel(isHost);
                    Channels[label] = c;
                }
                return c;
            }
        }

        private static int ToMs(long stopwatchTicks) => (int)(stopwatchTicks * 1000 / Stopwatch.Frequency);

        private static void FlushSecond()
        {
            List<KeyValuePair<string, Channel>> snapshot;
            lock (Gate) snapshot = Channels.ToList();

            foreach (var (label, c) in snapshot)
            {
                string? line;
                lock (c.Lock) line = c.IsHost ? HostLine(c) : ViewerLine(c);
                if (line != null) Write(label, line);
            }
        }

        private static string? ViewerLine(Channel c)
        {
            if (c.Packets == 0 && c.Frames == 0 && c.AudioFrames == 0 && c.Lost == 0) return null;

            var sb = new StringBuilder();
            sb.Append($"pkts={c.Packets} kbps={c.PacketBytes * 8 / 1000} perdidos={c.Lost} atrasados={c.Late}");
            sb.Append($" maiorIntervaloPkt={ToMs(c.MaxPacketGapTicks)}ms");
            sb.Append($" quadros={c.Frames} exibidos={c.Shown}");
            sb.Append($" idr={c.Idr}" + (c.Idr > 0 ? $"({c.IdrBytes / 1024}KB)" : ""));
            sb.Append(c.DecodeCount > 0
                ? string.Format(CultureInfo.InvariantCulture, " decod={0:0.0}/{1:0.0}ms", c.DecodeMsTotal / c.DecodeCount, c.DecodeMsMax)
                : " decod=-");
            if (c.DecodeFailures > 0) sb.Append($" falhasDecod={c.DecodeFailures}");
            sb.Append(c.HoldingNow ? " retendo=SIM" : " retendo=nao");
            if (c.KeyFrameRequests + c.KeyFrameRequestsSuppressed > 0)
                sb.Append($" pedidosKF={c.KeyFrameRequests}(+{c.KeyFrameRequestsSuppressed} segurados)");
            sb.Append($" audio={c.AudioFrames}/s");
            if (c.RttMs >= 0) sb.Append($" ping={c.RttMs}ms");
            AppendSampler(sb, c);

            c.Packets = c.PacketBytes = c.Lost = c.Late = c.Frames = c.FrameBytes = c.Shown = c.Idr = c.IdrBytes = 0;
            c.DecodeFailures = c.DecodeCount = c.KeyFrameRequests = c.KeyFrameRequestsSuppressed = c.AudioFrames = 0;
            c.DecodeMsTotal = c.DecodeMsMax = 0;
            c.MaxPacketGapTicks = 0;
            return sb.ToString();
        }

        private static string? HostLine(Channel c)
        {
            if (c.Captured == 0 && c.Encoded == 0) return null;

            var sb = new StringBuilder();
            sb.Append($"capturados={c.Captured} codificados={c.Encoded} puladosOcupado={c.SkippedBusy}");
            sb.Append(c.Encoded > 0
                ? string.Format(CultureInfo.InvariantCulture, " encode={0:0.0}/{1:0.0}ms", c.EncodeMsTotal / c.Encoded, c.EncodeMsMax)
                : " encode=-");
            sb.Append($" kbps={c.EncodedBytes * 8 / 1000}");
            if (c.HostIdr > 0) sb.Append($" idr={c.HostIdr}({c.HostIdrBytes / 1024}KB: {string.Join(",", c.IdrReasons)})");
            if (c.RequestsReceived > 0) sb.Append($" pedidosKF={c.RequestsReceived}(atendidos {c.RequestsServed})");
            AppendSampler(sb, c);

            c.Captured = c.Encoded = c.SkippedBusy = c.EncodedBytes = c.HostIdr = c.HostIdrBytes = 0;
            c.RequestsReceived = c.RequestsServed = 0;
            c.EncodeMsTotal = c.EncodeMsMax = 0;
            c.IdrReasons.Clear();
            return sb.ToString();
        }

        private static void AppendSampler(StringBuilder sb, Channel c)
        {
            if (c.Sampler == null) return;
            try
            {
                var extra = c.Sampler();
                if (!string.IsNullOrEmpty(extra)) sb.Append(' ').Append(extra);
            }
            catch { }
        }

        private static void Write(string label, string message)
        {
            try
            {
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                var line = $"{stamp} [{label}] {message}{Environment.NewLine}";
                lock (Gate)
                {
                    var path = FilePath;
                    if (!_rotationChecked)
                    {
                        _rotationChecked = true;
                        var info = new FileInfo(path);
                        if (info.Exists && info.Length >= MaxBytes)
                        {
                            var backup = AppPaths.GetFilePath(BackupFileName);
                            if (File.Exists(backup)) File.Delete(backup);
                            File.Move(path, backup);
                        }
                    }
                    File.AppendAllText(path, line);
                }
            }
            catch
            {
                // Mesmo princípio do DiagnosticLog: falha de escrita nunca derruba a live.
            }
        }
    }
}
