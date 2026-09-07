using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace StreamLiveApp
{
    /// <summary>
    /// Linha do tempo única do app, em <c>diagnostico.log</c>.
    ///
    /// Existe porque "o som não vem" e "a imagem não vem" eram indiagnosticáveis à distância:
    /// o app só gravava exceções, e todo o rastro do handshake, do fallback DXGI→GDI e da
    /// escolha do caminho de captura de áudio saía por <c>Debug.WriteLine</c> — que não existe
    /// na build Release que os amigos rodam. Um host transmitindo mudo não deixava uma linha
    /// em disco.
    ///
    /// Os logs antigos (error.log, audio_error.log, os dois do ffmpeg) continuam onde estavam;
    /// o que passa por aqui é espelhado neles quando for o caso, para haver uma ordem única.
    /// </summary>
    public static class DiagnosticLog
    {
        private const string FileName = "diagnostico.log";
        private const string BackupFileName = "diagnostico.1.log";

        /// <summary>
        /// Acima disso o arquivo vira <c>diagnostico.1.log</c> e recomeça. Os logs antigos não
        /// tinham teto nenhum: numa máquina com meses de uso, o relatório que o amigo manda
        /// seria grande demais para servir para alguma coisa.
        /// </summary>
        private const long MaxBytes = 2 * 1024 * 1024;

        private static readonly object Gate = new();

        /// <summary>Chaves já registradas por <see cref="Once"/> — ver o método.</summary>
        private static readonly HashSet<string> SeenKeys = new(StringComparer.Ordinal);

        private static bool _rotationChecked;

        public static string FilePath => AppPaths.GetFilePath(FileName);

        public static void Info(string area, string message) => Write("INFO ", area, message);

        public static void Warn(string area, string message) => Write("WARN ", area, message);

        public static void Error(string area, string message, Exception? ex = null)
            => Write("ERRO ", area, ex == null ? message : $"{message} :: {ex}");

        /// <summary>
        /// Registra só a primeira ocorrência de <paramref name="key"/>. Serve para os pontos
        /// que falham em rajada — captura a 60 Hz, envio de vídeo por peer —, onde logar tudo
        /// encheria o arquivo em segundos e escondaria o resto.
        /// </summary>
        public static void Once(string area, string key, string message)
        {
            lock (Gate)
            {
                if (!SeenKeys.Add(key)) return;
            }

            Warn(area, message);
        }

        private static void Write(string level, string area, string message)
        {
            try
            {
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                var line = $"{stamp} [{level}] [{area}] {message}{Environment.NewLine}";

                lock (Gate)
                {
                    var path = FilePath;
                    RotateIfNeededLocked(path);
                    File.AppendAllText(path, line);
                }
            }
            catch
            {
                // Log é apoio, nunca requisito: uma falha de escrita não pode derrubar a
                // transmissão. Mesmo comportamento dos WriteLog que existiam antes daqui.
            }
        }

        /// <summary>
        /// Roda uma vez por sessão: o arquivo só cresce a partir do que já estava lá, então
        /// checar o tamanho a cada linha seria um stat de disco por evento.
        /// </summary>
        private static void RotateIfNeededLocked(string path)
        {
            if (_rotationChecked) return;
            _rotationChecked = true;

            try
            {
                var info = new FileInfo(path);
                if (!info.Exists || info.Length < MaxBytes) return;

                var backup = AppPaths.GetFilePath(BackupFileName);
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(path, backup);
            }
            catch { }
        }

        /// <summary>
        /// Cabeçalho de sessão: tudo que o app sabe sobre a máquina e nunca registrava.
        ///
        /// O <b>build</b> do Windows é o campo mais importante da lista — é ele que separa
        /// Windows 10 de 11 e decide sozinho se <see cref="ProcessAudioCapturer.IsSupported"/>
        /// passa. Sem ele, "ele usa Win10" é palpite.
        /// </summary>
        public static void Session(string? contexto = null)
        {
            var sb = new StringBuilder();
            sb.Append("versao=").Append(AppInfo.Version);
            sb.Append(" | windows=").Append(DescribeWindows());
            sb.Append(" | processo=").Append(RuntimeInformation.ProcessArchitecture);
            sb.Append(" | telas=").Append(DescribeScreens());
            sb.Append(" | gpu=").Append(DesktopDuplicationGrabber.DescribeAdapters());
            sb.Append(" | saidaAudio=").Append(DescribeDefaultAudioDevice());
            sb.Append(" | capturaPorProcesso=")
              .Append(ProcessAudioCapturer.IsSupported() ? "suportada" : "NAO suportada");

            if (!string.IsNullOrEmpty(contexto)) sb.Append(" | ").Append(contexto);

            Info("Sessao", sb.ToString());
        }

        private static string DescribeWindows()
        {
            try
            {
                var v = Environment.OSVersion.Version;
                return $"{v.Major}.{v.Minor} build {v.Build}.{v.Revision} ({RuntimeInformation.OSArchitecture})";
            }
            catch (Exception ex) { return "?: " + ex.Message; }
        }

        private static string DescribeScreens()
        {
            try
            {
                var screens = System.Windows.Forms.Screen.AllScreens;
                var parts = new List<string>(screens.Length);
                foreach (var screen in screens)
                {
                    var b = screen.Bounds;
                    parts.Add($"{b.Width}x{b.Height}@{b.X},{b.Y}{(screen.Primary ? "*" : string.Empty)}");
                }
                return string.Join(" ", parts);
            }
            catch (Exception ex) { return "?: " + ex.Message; }
        }

        /// <summary>
        /// Dispositivo de saída padrão — é dele que o <c>WasapiLoopbackCapture</c> tira o som
        /// da transmissão. Sem saída padrão não existe loopback, e a live sobe muda.
        /// </summary>
        private static string DescribeDefaultAudioDevice()
        {
            try
            {
                using var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                using var device = enumerator.GetDefaultAudioEndpoint(
                    NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.Role.Console);
                return $"{device.FriendlyName} [{device.State}]";
            }
            catch (Exception ex)
            {
                return "NENHUM: " + ex.Message;
            }
        }

        /// <summary>
        /// Arquivos que entram no relatório de diagnóstico. É uma lista fechada de propósito:
        /// <c>friends.json</c> e <c>settings.json</c> ficam na mesma pasta e carregam os IPs
        /// dos amigos e a senha da sala — nada disso pode sair da máquina do usuário.
        /// </summary>
        public static readonly string[] ReportFiles =
        {
            FileName,
            BackupFileName,
            "error.log",
            "audio_error.log",
            "ffmpeg_error.log",
            "ffmpeg_encode_runtime_error.log"
        };
    }
}
