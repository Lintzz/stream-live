using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client;

namespace StreamLiveApp
{
    public class SignalingClient
    {
        internal const int MaxReconnectAttempts = 8;

        /// <summary>Teto da espera entre tentativas. Passar disso só faz a live demorar a voltar.</summary>
        internal const int MaxBackoffSeconds = 15;

        /// <summary>Variação aplicada à espera (±20%), para dois viewers não voltarem no mesmo instante.</summary>
        internal const double JitterFraction = 0.2;

        private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(3);

        // Um socket TCP meio-aberto (Wi-Fi caindo, VPN Radmin oscilando) continua "conectado"
        // para o Windows por minutos. O PING já saía a cada 3s, mas ninguém conferia se o PONG
        // voltava — então a live ficava congelada sem nada na tela até o TCP desistir sozinho.
        // Três pings sem qualquer resposta é o bastante para tratar como queda.
        private static readonly TimeSpan SilenceTimeout = TimeSpan.FromSeconds(9);

        private WebsocketClient? _client;
        private bool _intentionalStop = false;
        private int _reconnectAttempts = 0;
        private CancellationTokenSource? _reconnectCts;
        private System.Threading.Timer? _pingTimer;
        private byte[]? _encryptionKey;

        // Tick do último sinal de vida recebido (qualquer mensagem serve, não só o PONG).
        private long _lastActivityTicks = DateTime.UtcNow.Ticks;

        // Impede que o watchdog e o DisconnectionHappened subam dois loops de reconexão.
        private int _reconnecting;

        // A sessão chegou a ficar de pé antes desta queda? Se sim, a contagem de tentativas
        // recomeça do zero: sem isso, a segunda queda de uma sessão longa já nascia perto do
        // teto e desistia quase de imediato.
        private bool _hadHealthySession;

        public event Action<string> OnMessageReceived = delegate {};
        public event Action<byte[]> OnBinaryReceived = delegate {};
        public event Action<bool> OnConnected = delegate {}; // bool = isReconnect
        public event Action OnDisconnected = delegate {};
        public event Action<int, int> OnReconnecting = delegate {}; // tentativa atual, total
        public event Action OnReconnectFailed = delegate {};
        public event Action<int> OnLatencyUpdated = delegate {}; // milliseconds

        /// <summary>Liga a cifra da sala, com o salt que veio no desafio do host.</summary>
        public void EnableEncryption(string password, string saltB64)
        {
            _encryptionKey = CryptoHelper.DeriveRoomKeys(password, saltB64).Enc;
        }

        /// <summary>
        /// Esquece a chave da sala. A criptografia vale para <em>aquele</em> socket autenticado:
        /// depois de uma queda, o host não reconhece mais este cliente e volta a exigir o AUTH,
        /// que precisa sair em texto claro.
        /// </summary>
        public void DisableEncryption()
        {
            _encryptionKey = null;
        }

        public async Task StartAsync(string ipAddress, int port = 8080)
        {
            _url = new Uri($"ws://{ipAddress}:{port}");
            _client = CreateSocket(_url);

            _pingTimer = new System.Threading.Timer(PingTick, null, PingInterval, PingInterval);

            MarkActivity();
            await _client.Start();
        }

        private Uri? _url;

        // Já houve uma conexão de pé? Cada tentativa usa um WebsocketClient novo, e para ele a
        // conexão é sempre a "Initial" — quem diz se foi reconexão somos nós.
        private bool _everConnected;

        /// <summary>
        /// Um WebsocketClient novo, com os eventos ligados. A reconexão cria um por tentativa:
        /// com a reconexão da biblioteca desligada, a queda marca o cliente como parado, e o
        /// <c>Reconnect()</c> de um cliente parado volta sem tentar nada — o laço contava
        /// "Reconectando... (1/8)" até "(8/8)" sem nenhuma tentativa real.
        ///
        /// Os eventos de um cliente que já foi trocado são ignorados: o Dispose dele ainda
        /// avisa a desconexão, e isso não pode abrir outro laço nem apagar a chave da sala nova.
        /// </summary>
        private WebsocketClient CreateSocket(Uri url)
        {
            var socket = new WebsocketClient(url)
            {
                IsReconnectionEnabled = false,
                // Sem isto a biblioteca tenta de novo sozinha, um minuto depois de uma falha
                // ao conectar — em paralelo com o nosso laço.
                ErrorReconnectTimeout = null
            };

            socket.MessageReceived.Subscribe(msg =>
            {
                if (!ReferenceEquals(socket, _client)) return;
                MarkActivity();

                if (msg.MessageType == System.Net.WebSockets.WebSocketMessageType.Text && msg.Text != null)
                {
                    HandleTextMessage(msg.Text);
                }
                else if (msg.MessageType == System.Net.WebSockets.WebSocketMessageType.Binary && msg.Binary != null)
                {
                    var data = ResolveIncomingBinary(msg.Binary, _encryptionKey);
                    if (data != null) OnBinaryReceived?.Invoke(data);
                }
            });

            socket.ReconnectionHappened.Subscribe(info =>
            {
                if (!ReferenceEquals(socket, _client)) return;
                Debug.WriteLine($"[Client] Reconnection happened, type: {info.Type}");
                _reconnectCts?.Cancel();
                _reconnectAttempts = 0;
                _hadHealthySession = true;
                Interlocked.Exchange(ref _reconnecting, 0);
                MarkActivity();
                bool isReconnect = _everConnected || info.Type != ReconnectionType.Initial;
                _everConnected = true;
                OnConnected?.Invoke(isReconnect);
            });

            socket.DisconnectionHappened.Subscribe(info =>
            {
                if (!ReferenceEquals(socket, _client)) return;
                Debug.WriteLine($"[Client] Disconnected: {info.Type}");

                // A chave só vale enquanto o socket autenticado existir.
                DisableEncryption();
                OnDisconnected?.Invoke();

                if (_intentionalStop || info.Type == DisconnectionType.ByUser)
                {
                    return;
                }

                BeginReconnect();
            });

            return socket;
        }

        private void MarkActivity() => Interlocked.Exchange(ref _lastActivityTicks, DateTime.UtcNow.Ticks);

        private TimeSpan SilenceElapsed
            => DateTime.UtcNow - new DateTime(Interlocked.Read(ref _lastActivityTicks), DateTimeKind.Utc);

        private void HandleTextMessage(string text)
        {
            Debug.WriteLine($"[Client] Message received: {text.Substring(0, Math.Min(text.Length, 50))}...");

            var controlMsg = SignalingMessage.Deserialize(text);
            if (controlMsg != null && controlMsg.Type == "PONG")
            {
                if (long.TryParse(controlMsg.Data, out var sentTicks))
                {
                    var rttMs = (int)((DateTime.UtcNow.Ticks - sentTicks) / TimeSpan.TicksPerMillisecond);
                    OnLatencyUpdated?.Invoke(Math.Max(rttMs, 0));
                }
                return;
            }

            var plain = ResolveIncomingText(text, _encryptionKey);
            if (plain != null) OnMessageReceived?.Invoke(plain);
        }

        // O que o host manda em claro mesmo numa sala com senha: o handshake (vem antes de
        // existir chave, ou recusa uma) e as respostas de status e de ping, que servem a
        // conexões não autenticadas.
        private static readonly HashSet<string> ClearControlTypes = new(StringComparer.Ordinal)
        {
            "AUTH_REQUIRED", "AUTH_OK", "AUTH_FAIL", "AUTH_LOCKED", "STATUS_RESPONSE", "PONG"
        };

        /// <summary>
        /// Texto recebido, já decifrado; null quando deve ser descartado. Antes, o que não
        /// decifrava seguia como dado bruto — o que anulava a autenticação do AES-GCM: alguém
        /// no meio do caminho dentro da VPN injetava sinalização falsa numa sala com senha.
        /// Esse atalho existia para a live seguinte sem senha chegar; agora a chave é
        /// esquecida no STREAM_STOPPED (ViewerSession), e ele não faz mais falta.
        /// </summary>
        internal static string? ResolveIncomingText(string text, byte[]? key)
        {
            if (key == null) return text;

            var decrypted = CryptoHelper.TryDecryptText(text, key);
            if (decrypted != null) return decrypted;

            var control = SignalingMessage.Deserialize(text);
            return control?.Type != null && ClearControlTypes.Contains(control.Type) ? text : null;
        }

        /// <summary>Áudio recebido; com a chave ativa, só o que decifra. Null = descartar.</summary>
        internal static byte[]? ResolveIncomingBinary(byte[] data, byte[]? key)
            => key == null ? data : CryptoHelper.TryDecryptBytes(data, key);

        private void PingTick(object? state)
        {
            try
            {
                var client = _client;
                if (client == null || !client.IsRunning) return;
                if (_intentionalStop || Volatile.Read(ref _reconnecting) != 0) return;

                if (SilenceElapsed > SilenceTimeout)
                {
                    // Zera o relógio antes de sair: senão o watchdog dispara de novo a cada
                    // tick enquanto a reconexão ainda está em andamento.
                    MarkActivity();
                    Debug.WriteLine("[Client] Sem resposta do host; forçando reconexão.");
                    BeginReconnect();
                    return;
                }

                var ping = new SignalingMessage { Type = "PING", Data = DateTime.UtcNow.Ticks.ToString() };
                client.Send(SignalingMessage.Serialize(ping));
            }
            catch { }
        }

        private void BeginReconnect()
        {
            if (_intentionalStop) return;
            if (Interlocked.Exchange(ref _reconnecting, 1) != 0) return;

            if (_hadHealthySession)
            {
                _hadHealthySession = false;
                _reconnectAttempts = 0;
            }

            _ = TryReconnectLoop();
        }

        /// <summary>
        /// Espera antes da tentativa <paramref name="attempt"/> (contada a partir de 1). A
        /// primeira é imediata — a maioria das quedas na VPN dura menos de um segundo, e o
        /// delay fixo de 5s que havia aqui antes penalizava justamente o caso comum.
        /// </summary>
        internal static TimeSpan ComputeBackoffBase(int attempt)
        {
            if (attempt <= 1) return TimeSpan.Zero;

            var shift = Math.Min(attempt - 2, 20);
            var seconds = Math.Min(MaxBackoffSeconds, 1 << shift);
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>Espalha a espera em ±<see cref="JitterFraction"/>. <paramref name="roll"/> vem de [0,1).</summary>
        internal static TimeSpan ApplyJitter(TimeSpan baseDelay, double roll)
        {
            if (baseDelay <= TimeSpan.Zero) return TimeSpan.Zero;

            var factor = 1.0 - JitterFraction + (2 * JitterFraction * Math.Clamp(roll, 0.0, 1.0));
            return TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * factor);
        }

        private async Task TryReconnectLoop()
        {
            _reconnectCts?.Cancel();
            _reconnectCts = new CancellationTokenSource();
            var token = _reconnectCts.Token;

            try
            {
                while (_reconnectAttempts < MaxReconnectAttempts && !token.IsCancellationRequested)
                {
                    _reconnectAttempts++;
                    OnReconnecting?.Invoke(_reconnectAttempts, MaxReconnectAttempts);

                    var delay = ApplyJitter(ComputeBackoffBase(_reconnectAttempts), Random.Shared.NextDouble());
                    if (delay > TimeSpan.Zero)
                    {
                        try
                        {
                            await Task.Delay(delay, token);
                        }
                        catch (TaskCanceledException)
                        {
                            return;
                        }
                    }

                    if (token.IsCancellationRequested) return;

                    if (_url == null || _intentionalStop || _stopped) return;

                    // Troca primeiro e descarta depois: o aviso de desconexão do socket velho
                    // chega com _client já apontando para o novo e é ignorado.
                    var old = _client;
                    var fresh = CreateSocket(_url);
                    _client = fresh;
                    try { old?.Dispose(); } catch { }

                    try
                    {
                        await fresh.StartOrFail();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Client] Reconnect attempt {_reconnectAttempts} failed: {ex.Message}");
                    }

                    // O Stop() pode ter chegado durante a tentativa: o socket que ele descartou
                    // pode ter sido o anterior, então este não pode sobrar aberto.
                    if (_stopped)
                    {
                        try { fresh.Dispose(); } catch { }
                        return;
                    }

                    if (fresh.IsRunning)
                    {
                        MarkActivity();
                        return; // o ReconnectionHappened do socket novo zera o estado
                    }
                }

                if (!token.IsCancellationRequested)
                {
                    OnReconnectFailed?.Invoke();
                }
            }
            finally
            {
                Interlocked.Exchange(ref _reconnecting, 0);
            }
        }

        /// <summary>
        /// Impede novas tentativas de reconexão. Usado quando o host encerra a live de propósito:
        /// sem isso o viewer fica tentando voltar para uma stream que acabou.
        /// </summary>
        public void SuppressReconnect()
        {
            _intentionalStop = true;
            _reconnectCts?.Cancel();
        }

        /// <summary>Reabilita a reconexão automática (host voltou a transmitir).</summary>
        public void AllowReconnect()
        {
            _intentionalStop = false;
            _reconnectAttempts = 0;
            MarkActivity();
        }

        public void SendMessage(string message)
        {
            if (_client == null || !_client.IsRunning) return;

            if (_encryptionKey != null)
            {
                message = CryptoHelper.EncryptText(message, _encryptionKey);
            }
            _client.Send(message);
        }

        /// <summary>
        /// Envia sem criptografar. Só o AUTH usa este caminho: ele é a mensagem que <em>abre</em>
        /// a sessão cifrada, então precisa ser legível para o host — que ainda não sabe quem é
        /// este socket. Mandá-lo cifrado (o que acontecia depois de uma reconexão, porque a chave
        /// sobrevivia à queda) prendia viewer e host num ping-pong AUTH_REQUIRED ↔ AUTH sem fim.
        /// </summary>
        public void SendPlain(string message)
        {
            if (_client == null || !_client.IsRunning) return;
            _client.Send(message);
        }

        // Só o Stop(): diferente do _intentionalStop, que o host liga e desliga (STREAM_STOPPED).
        private volatile bool _stopped;

        public void Stop()
        {
            _stopped = true;
            _intentionalStop = true;
            _reconnectCts?.Cancel();
            _pingTimer?.Dispose();
            _client?.Dispose();
        }
    }
}
