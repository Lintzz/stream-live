using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIPSorcery.Net;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Encoders;
using SIPSorceryMedia.FFmpeg;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using NAudio.Wave;
using System.Text.Json;
using System.Net;
using System.Linq;

namespace StreamLiveApp
{
    public class StreamManager : IDisposable
    {
        private readonly Dictionary<string, RTCPeerConnection> _peerConnections = new Dictionary<string, RTCPeerConnection>();

        // Nulos até EnsureCapturers() — o caminho de viewer nunca chega a criá-los.
        private VideoCapturer? _videoCapturer;
        private AudioCapturer? _audioCapturer;
        private readonly object _capturerLock = new object();

        private IVideoEncoder? _videoEncoder;
        private readonly object _encoderLock = new object();
        private int _isEncoding = 0;

        // Keyframe por tempo decorrido, não por contagem de frames: a taxa real de captura
        // varia bastante, então "a cada 120 frames" dava um intervalo imprevisível.
        private static readonly TimeSpan KeyFrameInterval = TimeSpan.FromSeconds(2);
        private readonly Stopwatch _keyFrameClock = Stopwatch.StartNew();
        private TimeSpan _lastKeyFrame = TimeSpan.Zero;

        // Logo depois que alguem entra, o keyframe sai com frequencia bem maior. O IDR unico
        // disparado no "connected" costuma se perder — o ICE reporta conexao antes de a midia
        // fluir de verdade — e sem isso o viewer ficava o ciclo inteiro sem imagem.
        private static readonly TimeSpan KeyFrameBurstWindow = TimeSpan.FromSeconds(4);
        private static readonly TimeSpan KeyFrameBurstInterval = TimeSpan.FromMilliseconds(400);
        private TimeSpan _burstUntil = TimeSpan.MinValue;

        // Piso entre keyframes atendidos sob demanda: com varios viewers pedindo ao mesmo
        // tempo, sem isto o encoder mandaria so IDR e a banda explodiria.
        private static readonly TimeSpan MinForcedKeyFrameGap = TimeSpan.FromMilliseconds(300);
        private TimeSpan _lastForcedKeyFrame = TimeSpan.MinValue;

        // Viewer: o video chega mas nada decodifica ate vir um keyframe. Se isso durar,
        // pedimos um ao host em vez de esperar o proximo ciclo.
        private volatile bool _videoArriving;
        private volatile bool _videoDecodedEver;
        private System.Threading.Timer? _keyFrameRequestTimer;
        private TimeSpan _lastKeyFrameRequest = TimeSpan.MinValue;

        // ───────────────────────────── Áudio ─────────────────────────────

        // O áudio vai como PCM cru pelo WebSocket de sinalização, com um byte de marcação na
        // frente. Chegou a passar por Opus na trilha do WebRTC (v1.0.18 a v1.0.21), mas
        // nunca funcionou em campo e foi revertido — este é o caminho comprovado.
        //
        // O custo é conhecido: ~1,4 Mbps por viewer e sem sincronia com o vídeo, que viaja
        // por outro transporte. O atraso acumulado é contido pelo LatencyTrimmingProvider.
        private const int AudioSampleRate = 44100;

        // Contadores expostos na sobreposição de estatísticas. Sem eles, "o som não funciona"
        // não distingue captura, transporte e reprodução.
        private int _audioFramesSent;
        private int _audioFramesDecoded;
        private int _audioFailures;

        // Quadros de áudio capturados que não tinham para quem ir. Separado dos enviados
        // porque é a diferença entre "não está capturando" e "está capturando e ninguém
        // recebe" — os dois chegam como "não vem som" no relato do usuário.
        private int _audioFramesDropped;

        private int _encodeFailures;
        private string? _audioFailureReason;
        private readonly object _audioLock = new object();

        // Signaling events
        public event Action<string, string>? OnLocalSdpReady; // clientId, sdp (JSON SignalingMessage)

        // Media events
        public event Action<byte[], int, int, int>? OnVideoFrameDecoded; // payload, width, height, stride
        public event Action<byte[], int, int, int>? OnLocalVideoFrameReady; // raw local pixels

        public event Action<string>? OnAudioCaptureError;

        /// <summary>
        /// Texto livre de diagnóstico do caminho WebRTC (progresso do SDP, erro do decoder, o
        /// enum do SIPSorcery). Não serve para decidir nada — quem precisa reagir usa o
        /// <see cref="OnPeerStateChanged"/>.
        /// </summary>
        public event Action<string>? OnConnectionStateChanged;

        /// <summary>
        /// Estado da conexão WebRTC, tipado. Existe porque o viewer precisava distinguir
        /// "conectando" de "morreu": failed/disconnected chegavam só como texto e ninguém
        /// tratava, então a live congelava para sempre no último quadro.
        /// </summary>
        public event Action<RTCPeerConnectionState>? OnPeerStateChanged;

        public event Action<int, double>? OnHostStatsUpdated; // fps, kbps
        public event Action<int>? OnViewerFpsUpdated; // fps

        /// <summary>Quadros de audio por segundo — enviados no host, decodificados no viewer.</summary>
        public event Action<int>? OnAudioStatsUpdated;

        /// <summary>
        /// Aviso de que a live está no ar mas não está entregando (texto), ou <c>null</c>
        /// quando volta ao normal. Ver <see cref="AvaliarSaudeDaLive"/>.
        /// </summary>
        public event Action<string?>? OnHostHealthChanged;

        // Resumo no log a cada 10s. A cada segundo encheria o arquivo sem acrescentar nada:
        // o que se quer ver é a tendência, não o instante.
        private const int StatsLogIntervalTicks = 10;
        private int _statsTicks;

        /// <summary>Tempo de tolerância antes de avisar o host de que a live não entrega.</summary>
        private static readonly TimeSpan HealthGracePeriod = TimeSpan.FromSeconds(15);
        private readonly Stopwatch _broadcastClock = new();
        private string? _avisoAtual;

        /// <summary>Audio PCM para difusao pelo WebSocket (somente no modo legado).</summary>
        public event Action<byte[]>? OnBinaryDataReady;

        /// <summary>
        /// Consultado antes de empacotar cada quadro de audio. Sem viewer ouvindo, o pacote
        /// era montado (uma alocacao por quadro, ~50x/s) so para ser descartado no fim da
        /// linha — e transmitir para uma sala vazia e o estado normal enquanto os amigos
        /// ainda nao entraram.
        /// </summary>
        public Func<bool>? HasAudioListeners { get; set; }

        private System.Threading.Timer? _hostStatsTimer;
        private System.Threading.Timer? _viewerStatsTimer;
        private int _statsEncodedFrames = 0;
        private long _statsEncodedBytes = 0;
        private int _statsDecodedFrames = 0;

        private WaveOutEvent? _waveOut;
        private BufferedWaveProvider? _waveProvider;
        private LatencyTrimmingProvider? _latencyTrimmer;
        private NAudio.Wave.SampleProviders.VolumeSampleProvider? _volumeProvider;
        private bool _isHost = false;

        // Teto de atraso do áudio. Passou disso, o excedente é descartado até o alvo — é o
        // que impede a live de ir ficando dessincronizada ao longo da sessão.
        private static readonly TimeSpan MaxAudioLatency = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan TargetAudioLatency = TimeSpan.FromMilliseconds(80);

        private static int _mediaInitialized;

        /// <summary>
        /// Grava no arquivo específico e espelha no <see cref="DiagnosticLog"/>. O espelho é o
        /// que importa: separados, esses arquivos não se ordenam entre si nem com o resto, e
        /// era impossível dizer se o encoder quebrou antes ou depois de o viewer entrar.
        /// </summary>
        private static void WriteLog(string filename, string content)
        {
            try
            {
                System.IO.File.AppendAllText(AppPaths.GetFilePath(filename), DateTime.Now.ToString() + ": " + content + "\n");
            }
            catch { }
        }

        /// <summary>
        /// Inicialização global de mídia — uma vez por processo. Antes rodava no construtor,
        /// e como existe um StreamManager por live aberta, o FFmpeg era reinicializado (e o
        /// diretório corrente do processo trocado) a cada sessão.
        /// </summary>
        public static void EnsureMediaInitialized()
        {
            if (System.Threading.Interlocked.Exchange(ref _mediaInitialized, 1) != 0) return;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (System.IO.Directory.Exists(baseDir))
            {
                Environment.CurrentDirectory = baseDir;
            }

            try
            {
                // O libPath é obrigatório aqui. Sem ele o RegisterFFmpegBinaries procura o
                // layout "FFmpeg\bin\x64", que este projeto não usa — as DLLs são achatadas na
                // raiz da saída pelo <Link> do csproj —, e lançava "Unable to find FFMPEG
                // binaries" em toda sessão de toda máquina. O vídeo funcionava mesmo assim
                // (o FFmpegVideoEncoder resolve pelo loader do Windows, ao lado do exe), então
                // o que esse erro produzia era só caça ao antivírus num problema inexistente.
                SIPSorceryMedia.FFmpeg.FFmpegInit.Initialise(libPath: baseDir);
                DiagnosticLog.Info("Video", "FFmpeg inicializado.");
            }
            catch (Exception ex)
            {
                WriteLog("ffmpeg_error.log", "Init Error: " + ex.ToString());

                // Sem FFmpeg não há codec: a live sobe "no ar" e nunca sai um byte de vídeo.
                // As causas em campo são DLL bloqueada pelo antivírus/SmartScreen e runtime
                // do VC++ ausente — e nenhuma delas aparecia na tela do host.
                DiagnosticLog.Error("Video",
                    "FFmpeg NAO inicializou: nenhuma imagem sera transmitida (DLLs bloqueadas ou runtime ausente)", ex);
            }
        }

        public StreamManager()
        {
            EnsureMediaInitialized();

            // O encoder serve aos dois lados — o host codifica, o viewer decodifica com ele.
            // Os capturadores, não: quem só assiste nunca captura nada. Ver EnsureCapturers.
            InitEncoder();
        }

        /// <summary>
        /// Cria os capturadores na primeira vez que alguém precisa deles — o que só acontece
        /// no caminho de host.
        ///
        /// Antes eles nasciam no construtor, e como existe um StreamManager por live aberta,
        /// cada aba de viewer abria um <c>WasapiLoopbackCapture</c> e um
        /// <c>MediaFoundationResampler</c> (objetos COM de verdade, criados já no construtor
        /// do AudioCapturer) para nunca usar. Com quatro lives na grade eram quatro
        /// dispositivos de áudio segurados à toa.
        /// </summary>
        private void EnsureCapturers()
        {
            lock (_capturerLock)
            {
                if (_videoCapturer != null) return;

                _videoCapturer = new VideoCapturer();
                _audioCapturer = new AudioCapturer();

                WireCapturers(_videoCapturer, _audioCapturer);
            }
        }

        private void WireCapturers(VideoCapturer videoCapturer, AudioCapturer audioCapturer)
        {
            // Connect raw video from capturer to encoder
            videoCapturer.OnVideoSourceRawSample += (duration, width, height, sample, format) =>
            {
                OnLocalVideoFrameReady?.Invoke(sample, width, height, width * VideoCapturer.BytesPerPixel);

                if (System.Threading.Interlocked.CompareExchange(ref _isEncoding, 1, 0) != 0)
                {
                    return; // Skip encoding if previous frame is still processing
                }

                Task.Run(() =>
                {
                    try
                    {
                        byte[]? encoded = null;
                        lock (_encoderLock)
                        {
                            if (_videoEncoder != null)
                            {
                                try
                                {
                                    var now = _keyFrameClock.Elapsed;
                                    var interval = now <= _burstUntil ? KeyFrameBurstInterval : KeyFrameInterval;
                                    if (now - _lastKeyFrame >= interval)
                                    {
                                        _lastKeyFrame = now;
                                        _videoEncoder.ForceKeyFrame();
                                    }
                                    encoded = _videoEncoder.EncodeVideo(width, height, sample, format, VideoCodecsEnum.H264);
                                }
                                catch (Exception encodeEx)
                                {
                                    WriteLog("ffmpeg_encode_runtime_error.log", "Encode Run Error: " + encodeEx.ToString());

                                    // Um encoder quebrado falha em todo quadro: o host continua
                                    // vendo o próprio preview (que vem antes daqui) e acha que
                                    // está transmitindo. Só a primeira falha vai ao diagnóstico;
                                    // o contador do resumo periódico conta o volume.
                                    System.Threading.Interlocked.Increment(ref _encodeFailures);
                                    DiagnosticLog.Once("Video", "encode",
                                        "Falha ao codificar o video; a live segue sem imagem: " + encodeEx);
                                }
                            }
                        }

                        if (encoded != null && encoded.Length > 0)
                        {
                            System.Threading.Interlocked.Increment(ref _statsEncodedFrames);
                            System.Threading.Interlocked.Add(ref _statsEncodedBytes, encoded.Length);

                            foreach (var pc in SnapshotConnectedPeers())
                            {
                                // Uma falha por peer não pode interromper o envio aos outros,
                                // mas engolida por completo ela deixava um viewer sem imagem
                                // sem indicação nenhuma nem no host nem nele.
                                try { pc.SendVideo(duration, encoded); }
                                catch (Exception sendEx)
                                {
                                    DiagnosticLog.Once("Video", "envio-" + pc.SessionID,
                                        $"Falha ao enviar video para um viewer: {sendEx.GetBaseException().Message}");
                                }
                            }
                        }
                    }
                    finally
                    {
                        System.Threading.Interlocked.Exchange(ref _isEncoding, 0);
                    }
                });
            };

            audioCapturer.OnAudioFrameReady += OnCapturedPcm;

            audioCapturer.OnCaptureError += (error) =>
            {
                OnAudioCaptureError?.Invoke(error);
            };
        }

        /// <summary>
        /// Empacota o PCM capturado e entrega para difusão pelo WebSocket. Um byte de
        /// marcação na frente distingue áudio de qualquer outro dado binário.
        /// </summary>
        private void OnCapturedPcm(byte[] pcm)
        {
            if (pcm == null || pcm.Length == 0) return;
            if (HasAudioListeners != null && !HasAudioListeners())
            {
                System.Threading.Interlocked.Increment(ref _audioFramesDropped);
                return;
            }

            // O buffer NAO e reaproveitado de proposito: sem senha de sala ele segue direto
            // para o Send do Fleck, que e assincrono. Reciclar o array por baixo de um envio
            // em voo trocaria os bytes no meio do caminho.
            var packet = new byte[pcm.Length + 1];
            packet[0] = 1; // 1 = áudio
            Buffer.BlockCopy(pcm, 0, packet, 1, pcm.Length);

            OnBinaryDataReady?.Invoke(packet);
            System.Threading.Interlocked.Increment(ref _audioFramesSent);
        }

        /// <summary>
        /// Registra a primeira falha de audio e avisa a interface. Antes tudo aqui era
        /// engolido por um catch vazio, entao um problema no audio nao deixava rastro nenhum.
        /// </summary>
        private void ReportAudioFailure(string acao, Exception ex)
        {
            System.Threading.Interlocked.Increment(ref _audioFailures);

            if (_audioFailureReason != null) return;
            _audioFailureReason = $"Falha ao {acao} audio: {ex.Message}";

            WriteLog("audio_error.log", $"[{acao}] {ex}");
            DiagnosticLog.Error("Audio", $"Falha ao {acao} audio", ex);
            OnAudioCaptureError?.Invoke(_audioFailureReason);
        }

        private List<RTCPeerConnection> SnapshotConnectedPeers()
        {
            lock (_peerConnections)
            {
                return _peerConnections.Values
                    .Where(pc => pc.connectionState == RTCPeerConnectionState.connected)
                    .ToList();
            }
        }

        /// <summary>
        /// Cria a saída de áudio na primeira amostra recebida, no formato que veio do host.
        /// Criar sob demanda evita abrir um dispositivo de áudio para uma live que talvez
        /// nunca traga som.
        /// </summary>
        private void EnsureAudioOutput(WaveFormat format)
        {
            if (_waveOut != null) return;

            lock (_audioLock)
            {
                if (_waveOut != null) return;

                try
                {
                    _waveProvider = new BufferedWaveProvider(format)
                    {
                        DiscardOnBufferOverflow = true,
                        BufferDuration = TimeSpan.FromMilliseconds(800)
                    };

                    _latencyTrimmer = new LatencyTrimmingProvider(_waveProvider, MaxAudioLatency, TargetAudioLatency);

                    _volumeProvider = new NAudio.Wave.SampleProviders.VolumeSampleProvider(_latencyTrimmer.ToSampleProvider())
                    {
                        Volume = _pendingVolume
                    };

                    _waveOut = new WaveOutEvent();
                    _waveOut.Init(_volumeProvider);
                    _waveOut.Play();
                }
                catch (Exception ex)
                {
                    // Abrir o dispositivo de saida pode falhar (sem placa, driver ocupado).
                    // Antes a excecao subia ate o handler do WebSocket e sumia no log global:
                    // a live continuava, muda, sem nada explicando por que.
                    _waveProvider = null;
                    _latencyTrimmer = null;
                    _volumeProvider = null;
                    _waveOut = null;

                    ReportAudioFailure("abrir a saida de", ex);
                }
            }
        }

        /// <summary>Áudio PCM recebido pelo WebSocket.</summary>
        public void ProcessReceivedBinary(byte[] data)
        {
            if (data == null || data.Length < 2 || data[0] != 1) return;

            EnsureAudioOutput(new WaveFormat(AudioSampleRate, 16, AudioCapturer.Channels));

            var provider = _waveProvider;
            if (provider == null) return;

            try
            {
                provider.AddSamples(data, 1, data.Length - 1);
                System.Threading.Interlocked.Increment(ref _audioFramesDecoded);
            }
            catch (Exception ex)
            {
                ReportAudioFailure("reproduzir o", ex);
            }
        }

        private float _pendingVolume = 1.0f;

        public void SetVolume(float volume)
        {
            // Guardado tambem quando a saida ainda nao existe: ela e criada sob demanda, e
            // sem isto o volume ajustado antes do primeiro audio se perdia.
            _pendingVolume = volume;

            if (_volumeProvider != null)
            {
                _volumeProvider.Volume = volume;
            }
        }

        // Todo ajuste de captura materializa os capturadores: quem chama qualquer um destes
        // está montando uma transmissão. O viewer não chama nenhum, e é assim que ele escapa
        // de abrir dispositivo de áudio e vídeo que nunca usaria.

        /// <summary>
        /// Caminho de captura em uso — "DXGI" ou "GDI". Leitura pura: não materializa nada,
        /// devolve "—" enquanto não há captura montada.
        /// </summary>
        public string ActiveCaptureMode => _videoCapturer?.ActiveCaptureMode ?? "—";

        public void SetResolution(int width, int height)
        {
            EnsureCapturers();
            _videoCapturer!.SetResolution(width, height);
        }

        public void SetTargetSource(CaptureSource source)
        {
            EnsureCapturers();
            _videoCapturer!.SetTargetSource(source);
        }

        /// <summary>
        /// Define qual processo fica FORA da captura de áudio (0 = capturar tudo). Na prática
        /// é sempre o Discord, resolvido pela UI antes de subir a live: sem isso a mesa se
        /// escuta em eco.
        /// </summary>
        public void SetExcludedAudioProcess(uint processId)
        {
            EnsureCapturers();
            _audioCapturer!.SetTargetProcess(processId);
        }

        private void InitEncoder()
        {
            lock (_encoderLock)
            {
                if (_videoEncoder == null)
                {
                    // A versão atual do SIPSorcery para .NET 8 não expõe API para selecionar NVENC/AMF nativamente.
                    // Portanto, usamos o libx264 com perfil 'ultrafast' e 'zerolatency' para garantir baixíssimo uso de CPU,
                    // simulando a performance de uma GPU. Já houve um 'veryfast' opcional aqui: a diferença de
                    // imagem não se via em campo e o custo de CPU sim, então o ultrafast virou fixo.
                    var x264Options = new Dictionary<string, string>
                    {
                        { "preset", "ultrafast" },
                        { "tune", "zerolatency" }
                    };

                    _videoEncoder = new FFmpegVideoEncoder(x264Options);
                }
            }
        }

        public void ForceKeyFrame() => StartKeyFrameBurst();

        /// <summary>Abre uma janela em que os keyframes saem com frequencia bem maior.</summary>
        private void StartKeyFrameBurst()
        {
            var now = _keyFrameClock.Elapsed;
            _burstUntil = now + KeyFrameBurstWindow;

            lock (_encoderLock)
            {
                try { _videoEncoder?.ForceKeyFrame(); } catch { }
            }
            _lastKeyFrame = now;
            _lastForcedKeyFrame = now;
        }

        /// <summary>
        /// Atende ao pedido de keyframe de um viewer, com um piso de tempo entre atendimentos
        /// para que varios viewers pedindo junto nao virem uma enxurrada de IDR.
        /// </summary>
        private void ServeKeyFrameRequest()
        {
            var now = _keyFrameClock.Elapsed;
            if (_lastForcedKeyFrame != TimeSpan.MinValue && now - _lastForcedKeyFrame < MinForcedKeyFrameGap) return;

            _lastForcedKeyFrame = now;
            _lastKeyFrame = now;
            lock (_encoderLock)
            {
                try { _videoEncoder?.ForceKeyFrame(); } catch { }
            }
        }

        public Task InitializeHost()
        {
            InitEncoder();
            EnsureCapturers();
            _isHost = true;
            _videoCapturer!.StartVideo();
            _audioCapturer!.StartAudio();

            _broadcastClock.Restart();

            _hostStatsTimer = new System.Threading.Timer(_ =>
            {
                var fps = System.Threading.Interlocked.Exchange(ref _statsEncodedFrames, 0);
                var bytes = System.Threading.Interlocked.Exchange(ref _statsEncodedBytes, 0);
                var audio = System.Threading.Interlocked.Exchange(ref _audioFramesSent, 0);
                var semDestino = System.Threading.Interlocked.Exchange(ref _audioFramesDropped, 0);
                OnHostStatsUpdated?.Invoke(fps, bytes * 8.0 / 1000.0);
                OnAudioStatsUpdated?.Invoke(audio);

                try { AvaliarSaudeDaLive(fps, bytes, audio, semDestino); } catch { }
            }, null, 1000, 1000);

            DiagnosticLog.Session("papel=host");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Resumo no diagnóstico e aviso na tela do host quando a live está claramente furada.
        ///
        /// Existe porque o preview do host vem de <see cref="OnLocalVideoFrameReady"/>, que é
        /// disparado ANTES do encoder e da rede: o host vê a própria tela, a sobreposição
        /// mostra fps e kbps saudáveis, e mesmo assim ninguém do outro lado recebe nada. A
        /// contagem de peers na mesma linha é o que desfaz esse engano — os quadros contados
        /// são os codificados, não os entregues.
        /// </summary>
        private void AvaliarSaudeDaLive(int fps, long bytes, int audio, int audioSemDestino)
        {
            var decorrido = _broadcastClock.Elapsed;
            var (conectados, total) = ContarPeers();

            if (++_statsTicks % StatsLogIntervalTicks == 0)
            {
                DiagnosticLog.Info("Live",
                    $"video={fps}fps {bytes * 8.0 / 1000.0:F0}kbps falhasEncode={_encodeFailures} | " +
                    $"audio={audio}/s semDestino={audioSemDestino}/s falhas={_audioFailures} | " +
                    $"peers={conectados}/{total} | captura={ActiveCaptureMode}");
            }

            // A janela de carência cobre o handshake inteiro (ICE, senha, primeiro keyframe);
            // avisar antes disso transformaria o começo normal de toda live num alerta.
            if (decorrido < HealthGracePeriod) return;

            string? aviso = null;
            if (total > 0 && conectados == 0)
            {
                // Dizia "verifique o firewall do Windows". Era um palpite exibido como
                // diagnóstico, e mandou gente caçar regra de firewall enquanto a causa real
                // estava nos candidatos ICE. Agora o aviso relata o que aconteceu — o log
                // (categoria WebRTC/*) é que carrega os endereços tentados.
                aviso = total == 1
                    ? "A conexão de vídeo com quem está assistindo não fechou — só o áudio está indo."
                    : $"A conexão de vídeo não fechou com nenhum dos {total} espectadores — só o áudio está indo.";
            }
            else if (fps == 0)
            {
                aviso = "Sua tela não está sendo capturada — nada de imagem está saindo daqui.";
            }
            else if (conectados > 0 && audio == 0)
            {
                aviso = "Seu áudio não está sendo enviado.";
            }

            if (aviso == _avisoAtual) return;
            _avisoAtual = aviso;

            if (aviso != null) DiagnosticLog.Warn("Live", "Aviso ao host: " + aviso);
            OnHostHealthChanged?.Invoke(aviso);
        }

        private (int conectados, int total) ContarPeers()
        {
            lock (_peerConnections)
            {
                int conectados = 0;
                foreach (var pc in _peerConnections.Values)
                {
                    if (pc.connectionState == RTCPeerConnectionState.connected) conectados++;
                }
                return (conectados, _peerConnections.Count);
            }
        }

        public async Task InitializeClient()
        {
            InitEncoder();
            _isHost = false;

            // Client creates a single connection (to the host)
            await CreatePeerConnection("host");

            // Enquanto chegar video sem nada decodificar, insiste no pedido de keyframe.
            _keyFrameRequestTimer = new System.Threading.Timer(_ =>
            {
                if (!_videoArriving || _videoDecodedEver) return;

                var request = new SignalingMessage { Type = "REQUEST_KEYFRAME", SenderId = "client" };
                OnLocalSdpReady?.Invoke("host", SignalingMessage.Serialize(request));
            }, null, 600, 600);

            _viewerStatsTimer = new System.Threading.Timer(_ =>
            {
                var fps = System.Threading.Interlocked.Exchange(ref _statsDecodedFrames, 0);
                var audio = System.Threading.Interlocked.Exchange(ref _audioFramesDecoded, 0);
                OnViewerFpsUpdated?.Invoke(fps);
                OnAudioStatsUpdated?.Invoke(audio);
            }, null, 1000, 1000);


        }

        /// <summary>
        /// Pede um keyframe ao host e rearma o timer que insiste no pedido.
        ///
        /// <c>_videoDecodedEver</c> só ia de false para true: depois do primeiro quadro
        /// decodificado na vida da sessão, o timer acima ficava inerte para sempre. Se uma
        /// rajada de perda destruísse a referência, o viewer não pedia nada e ficava com
        /// macrobloco na tela até o keyframe periódico do host — até 2 segundos, toda vez.
        /// </summary>
        public void RequestKeyFrame()
        {
            if (_isHost) return;

            _videoDecodedEver = false;

            // Mesmo piso do lado do host: um viewer em rede ruim não pode virar uma metralhadora
            // de pedidos, porque cada IDR atendido custa banda para todo mundo na live.
            var now = _keyFrameClock.Elapsed;
            if (_lastKeyFrameRequest != TimeSpan.MinValue && now - _lastKeyFrameRequest < MinForcedKeyFrameGap) return;
            _lastKeyFrameRequest = now;

            var request = new SignalingMessage { Type = "REQUEST_KEYFRAME", SenderId = "client" };
            OnLocalSdpReady?.Invoke("host", SignalingMessage.Serialize(request));
        }

        private string IceCategory => _isHost ? "WebRTC/host" : "WebRTC/viewer";

        /// <summary>
        /// Candidatos ICE reunidos por peer, guardados até o desfecho da conexão.
        ///
        /// Existe porque o log registrava o estado do peer e mais nada: dava para ver que o ICE
        /// falhou, nunca <em>com que endereços</em> ele tentou. Um caso de "o som vai e a imagem
        /// não" levou dois dias de caça ao firewall porque nenhum dos dois lados sabia dizer se
        /// o IP da VPN tinha sequer entrado na lista de candidatos.
        /// </summary>
        private sealed class PeerIceLog
        {
            // Um cadeado só para os dois lados: quem escreve são as tarefas de coleta do
            // SIPSorcery e a thread que processa a sinalização, ao mesmo tempo.
            private readonly object _gate = new object();
            private readonly List<string> _local = new List<string>();
            private readonly List<string> _remote = new List<string>();
            private bool _flushed;

            public void AddLocal(string descricao)
            {
                lock (_gate) { if (!_local.Contains(descricao)) _local.Add(descricao); }
            }

            public void AddRemote(string descricao)
            {
                lock (_gate) { if (!_remote.Contains(descricao)) _remote.Add(descricao); }
            }

            /// <summary>
            /// Despeja as duas listas numa linha só, uma única vez por peer. O laço de
            /// recuperação do viewer refaz a conexão a cada 18 s; uma linha por evento encheria
            /// o arquivo com a mesma informação.
            /// </summary>
            public void Flush(string categoria, string clientId)
            {
                string locais, remotos;
                lock (_gate)
                {
                    if (_flushed) return;
                    _flushed = true;

                    locais = _local.Count > 0 ? string.Join(", ", _local) : "nenhum";
                    remotos = _remote.Count > 0 ? string.Join(", ", _remote) : "nenhum";
                }

                DiagnosticLog.Info(categoria, $"peer {clientId}: candidatos locais=[{locais}] remotos=[{remotos}]");
            }
        }

        private readonly Dictionary<string, PeerIceLog> _iceLogs = new Dictionary<string, PeerIceLog>();

        private PeerIceLog TrackIce(string clientId)
        {
            lock (_peerConnections)
            {
                var log = new PeerIceLog();
                _iceLogs[clientId] = log;
                return log;
            }
        }

        private PeerIceLog? PeekIce(string clientId)
        {
            lock (_peerConnections)
            {
                return _iceLogs.TryGetValue(clientId, out var log) ? log : null;
            }
        }

        private static string DescribeCandidate(RTCIceCandidate? candidate)
        {
            if (candidate == null) return "?";
            return $"{candidate.type} {candidate.protocol} {FormatEndpoint(candidate.address, candidate.port.ToString())}";
        }

        /// <summary>
        /// Endereço e porta num só token. O colchete no IPv6 não é enfeite: com todas as
        /// interfaces no ICE saem candidatos como <c>2804:1b3:a9c1:148d:e1c9:35fb:f03a:babd</c>,
        /// e sem ele a linha do log terminava em <c>:babd:57802</c> — impossível dizer onde
        /// acaba o endereço e começa a porta, que é exatamente o que se vai ler ali.
        /// </summary>
        private static string FormatEndpoint(string? address, string port)
            => address != null && address.Contains(':') ? $"[{address}]:{port}" : $"{address}:{port}";

        private static string DescribeCandidate(RTCIceCandidateInit? candidate)
            => DescribeCandidateAttribute(candidate?.candidate);

        /// <summary>
        /// Reduz o atributo SDP de um candidato ao que interessa no log — tipo, transporte,
        /// endereço e porta. O que chega do outro lado é a linha crua
        /// <c>candidate:&lt;foundation&gt; &lt;componente&gt; &lt;transporte&gt; &lt;prioridade&gt; &lt;ip&gt; &lt;porta&gt; typ &lt;tipo&gt; ...</c>,
        /// e é o <b>endereço</b> dela que diz se o amigo anunciou o IP da VPN ou só o da LAN
        /// dele — a diferença entre uma live que fecha e uma que morre em 16 s.
        /// </summary>
        internal static string DescribeCandidateAttribute(string? sdpAttribute)
        {
            if (string.IsNullOrWhiteSpace(sdpAttribute)) return "?";

            var campos = sdpAttribute.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (campos.Length < 6) return sdpAttribute.Trim();

            var transporte = campos[2];
            var endereco = campos[4];
            var porta = campos[5];

            var tipo = "?";
            for (int i = 6; i + 1 < campos.Length; i++)
            {
                if (campos[i] == "typ") { tipo = campos[i + 1]; break; }
            }

            return $"{tipo} {transporte} {FormatEndpoint(endereco, porta)}";
        }

        /// <summary>
        /// Cria a conexão WebRTC do peer. Era <c>async void</c>: exceções em createOffer se
        /// perdiam no TaskScheduler e o chamador não tinha como esperar o offer sair.
        /// </summary>
        public async Task CreatePeerConnection(string clientId)
        {
            // Sem configuração o SIPSorcery gera candidatos ICE só com os endereços da placa
            // que o Windows usa para sair à internet — a Ethernet/Wi-Fi de casa. O 26.x da
            // Radmin, que é o ÚNICO endereço em que dois amigos se alcançam, ficava de fora, e
            // o ICE só fechava quando o outro lado, por sorte, anunciava o dele: a live
            // funcionava com uns amigos e nunca com outros, sempre morrendo nos 16s do
            // FAILED_TIMEOUT_PERIOD. Incluir todas as interfaces é seguro aqui — os endereços
            // locais "vazam" para amigos autenticados dentro da própria VPN.
            var pc = new RTCPeerConnection(new RTCConfiguration
            {
                X_ICEIncludeAllInterfaceAddresses = true
            });

            // Both host and client need to know they support H264
            var videoFormat = new SDPAudioVideoMediaFormat(new VideoFormat(VideoCodecsEnum.H264, 96));
            var videoTrack = new MediaStreamTrack(SDPMediaTypesEnum.video, false, new List<SDPAudioVideoMediaFormat> { videoFormat });
            pc.addTrack(videoTrack);

            if (!_isHost)
            {
                bool firstFrame = true;
                pc.OnVideoFrameReceived += (IPEndPoint rep, uint timestamp, byte[] payload, VideoFormat format) =>
                {
                    _videoArriving = true;
                    if (firstFrame)
                    {
                        firstFrame = false;
                        OnConnectionStateChanged?.Invoke("Aguardando keyframe...");
                    }
                    List<SIPSorceryMedia.Abstractions.VideoSample>? samples = null;
                    lock (_encoderLock)
                    {
                        if (_videoEncoder != null)
                        {
                            try
                            {
                                samples = _videoEncoder.DecodeVideo(payload, VideoPixelFormatsEnum.Bgr, VideoCodecsEnum.H264).ToList();
                            }
                            catch (Exception ex)
                            {
                                OnConnectionStateChanged?.Invoke($"Decode Error: {ex.Message}");
                            }
                        }
                    }
                    if (samples != null && samples.Any())
                    {
                        var sample = samples.First();
                        if (sample.Sample != null)
                        {
                            _videoDecodedEver = true;
                            System.Threading.Interlocked.Increment(ref _statsDecodedFrames);
                            OnVideoFrameDecoded?.Invoke(sample.Sample, (int)sample.Width, (int)sample.Height, (int)(sample.Width * 3));
                        }
                    }
                };

            }

            var iceLog = TrackIce(clientId);

            pc.onicecandidate += (candidate) =>
            {
                if (candidate == null) return;
                iceLog.AddLocal(DescribeCandidate(candidate));
                var msg = new SignalingMessage { Type = "ice", Data = candidate.toJSON(), SenderId = clientId };
                OnLocalSdpReady?.Invoke(clientId, SignalingMessage.Serialize(msg));
            };

            // O ICE tem estado próprio e ele é o que interessa quando nada aparece na tela: o
            // connectionState só vira failed depois, e junta no mesmo balde "não achei par
            // nenhum" e "achei, mas o DTLS morreu".
            pc.oniceconnectionstatechange += (state) =>
                DiagnosticLog.Info(IceCategory, $"peer {clientId}: ICE {state}");

            pc.onicecandidateerror += (candidate, error) =>
                DiagnosticLog.Warn(IceCategory,
                    $"peer {clientId}: falha ao reunir candidato {DescribeCandidate(candidate)}: {error}");

            pc.onconnectionstatechange += (state) =>
            {
                // O estado do peer só existia como texto efêmero na sobreposição de
                // estatísticas, que vem desligada. É esta linha que distingue "o ICE nunca
                // fechou" de "fechou e mesmo assim não vem imagem" — e, junto com a linha de
                // candidatos abaixo, com que endereços ele tentou.
                DiagnosticLog.Info(IceCategory, $"peer {clientId}: {state}");

                // O desfecho é o momento certo para despejar os candidatos: aqui os dois lados
                // já trocaram tudo que tinham. Antes disso a lista sai pela metade, e a cada
                // evento sairia repetida — o laço de recuperação refaz este peer a cada 18s.
                if (state == RTCPeerConnectionState.connected || state == RTCPeerConnectionState.failed)
                {
                    iceLog.Flush(IceCategory, clientId);
                }

                OnConnectionStateChanged?.Invoke(state.ToString());
                OnPeerStateChanged?.Invoke(state);
                if (state == RTCPeerConnectionState.connected && _isHost)
                {
                    StartKeyFrameBurst();
                }
            };

            lock (_peerConnections)
            {
                _peerConnections[clientId] = pc;
            }

            if (_isHost)
            {
                OnConnectionStateChanged?.Invoke("Creating Offer...");
                var offer = pc.createOffer(null);
                await pc.setLocalDescription(offer);
                var msg = new SignalingMessage { Type = "offer", Data = offer.toJSON(), SenderId = clientId };
                OnConnectionStateChanged?.Invoke("Sending Offer...");
                OnLocalSdpReady?.Invoke(clientId, SignalingMessage.Serialize(msg));
            }
        }

        public async Task HandleSignalingMessage(string clientId, string jsonMsg)
        {
            var msg = SignalingMessage.Deserialize(jsonMsg);
            if (msg == null) return;

            if (msg.Type == "STREAM_STOPPED")
            {
                return;
            }

            // Equivalente ao PLI do WebRTC: o viewer avisa que esta sem imagem e o host manda
            // um keyframe na hora, em vez de o viewer esperar o proximo ciclo.
            if (msg.Type == "REQUEST_KEYFRAME")
            {
                if (_isHost) ServeKeyFrameRequest();
                return;
            }

            // Um CLIENT_CONNECTED é sempre um pedido de negociação nova — o viewer acabou de
            // entrar, ou a mídia dele morreu e ele quer recomeçar. Antes isto caía na busca
            // abaixo, encontrava a conexão antiga e não gerava offer nenhum: o viewer que
            // tentava se recuperar ficava esperando para sempre por um vídeo que nunca vinha.
            if (msg.Type == "CLIENT_CONNECTED")
            {
                if (!_isHost) return;
                RemoveClient(clientId);
                await CreatePeerConnection(clientId);
                return;
            }

            RTCPeerConnection? pc;
            bool needsCreate = false;
            lock (_peerConnections)
            {
                if (!_peerConnections.TryGetValue(clientId, out pc))
                {
                    if (!_isHost) return;
                    needsCreate = true;
                }
            }

            // Fora do lock: CreatePeerConnection tem awaits e não deve segurar o cadeado.
            if (needsCreate)
            {
                await CreatePeerConnection(clientId);
                lock (_peerConnections)
                {
                    if (!_peerConnections.TryGetValue(clientId, out pc)) return;
                }
            }

            if (pc == null) return;

            if (msg.Type == "offer")
            {
                OnConnectionStateChanged?.Invoke("Received Offer");
                if (RTCSessionDescriptionInit.TryParse(msg.Data, out var init))
                {
                    OnConnectionStateChanged?.Invoke("Parsed Offer, Setting Remote...");
                    var result = pc.setRemoteDescription(init);
                    if (result == SetDescriptionResultEnum.OK)
                    {
                        OnConnectionStateChanged?.Invoke("Creating Answer...");
                        var answer = pc.createAnswer(null);
                        await pc.setLocalDescription(answer);
                        var answerMsg = new SignalingMessage { Type = "answer", Data = answer.toJSON(), SenderId = clientId };
                        OnConnectionStateChanged?.Invoke("Sending Answer...");
                        OnLocalSdpReady?.Invoke(clientId, SignalingMessage.Serialize(answerMsg));
                    }
                    else
                    {
                        OnConnectionStateChanged?.Invoke($"Failed to set Remote Offer: {result}");
                    }
                }
                else
                {
                    OnConnectionStateChanged?.Invoke("Failed to parse Offer");
                }
            }
            else if (msg.Type == "answer")
            {
                OnConnectionStateChanged?.Invoke("Received Answer");
                if (RTCSessionDescriptionInit.TryParse(msg.Data, out var init))
                {
                    OnConnectionStateChanged?.Invoke("Setting Remote Answer...");
                    pc.setRemoteDescription(init);
                }
                else
                {
                    OnConnectionStateChanged?.Invoke("Failed to parse Answer");
                }
            }
            else if (msg.Type == "ice")
            {
                OnConnectionStateChanged?.Invoke("Received ICE");
                if (RTCIceCandidateInit.TryParse(msg.Data, out var candidate))
                {
                    PeekIce(clientId)?.AddRemote(DescribeCandidate(candidate));
                    pc.addIceCandidate(candidate);
                }
            }
        }

        public void RemoveClient(string clientId)
        {
            lock (_peerConnections)
            {
                if (_peerConnections.TryGetValue(clientId, out var pc))
                {
                    try { pc.Close("Client disconnected"); } catch { }
                    _peerConnections.Remove(clientId);
                }
                _iceLogs.Remove(clientId);
            }
        }

        public void Stop()
        {
            _hostStatsTimer?.Dispose();
            _hostStatsTimer = null;
            _viewerStatsTimer?.Dispose();
            _viewerStatsTimer = null;
            _keyFrameRequestTimer?.Dispose();
            _keyFrameRequestTimer = null;

            // Dispose, e não só Close: o AudioCapturer segura um WasapiLoopbackCapture e um
            // MediaFoundationResampler que CloseAudio não libera. Como o ViewerSession cria um
            // StreamManager novo a cada conexão, reconexão e STREAM_STARTED, fechar sem
            // liberar acumulava esses objetos ao longo da sessão.
            lock (_capturerLock)
            {
                try { _videoCapturer?.Dispose(); } catch { }
                _videoCapturer = null;
                try { _audioCapturer?.Dispose(); } catch { }
                _audioCapturer = null;
            }

            lock (_audioLock)
            {
                try { _waveOut?.Stop(); } catch { }
                try { _waveOut?.Dispose(); } catch { }
                _waveOut = null;
                _volumeProvider = null;
                _latencyTrimmer = null;
                _waveProvider = null;
            }

            lock (_peerConnections)
            {
                foreach (var pc in _peerConnections.Values)
                {
                    try { pc.Close("Closed by user"); } catch { }
                }
                _peerConnections.Clear();
            }
            lock (_encoderLock)
            {
                _videoEncoder?.Dispose();
                _videoEncoder = null;
            }
        }

        public void Dispose() => Stop();
    }
}
