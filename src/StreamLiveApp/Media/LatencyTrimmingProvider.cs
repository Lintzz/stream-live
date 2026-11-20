using System;
using NAudio.Wave;

namespace StreamLiveApp
{
    /// <summary>
    /// Mantém a latência do áudio sob controle descartando o excedente acumulado.
    ///
    /// A causa do atraso é física: o host captura no relógio da placa de som dele e o viewer
    /// toca no relógio da dele. Uma diferença de centésimos de por cento já faz a fila crescer
    /// sem parar, e o áudio vai ficando cada vez mais atrás do vídeo ao longo da sessão.
    ///
    /// Em vez de zerar a fila inteira de tempos em tempos (o que dá um corte audível), aqui o
    /// excedente é jogado fora aos poucos, na própria leitura, sempre que a fila passa do teto.
    /// </summary>
    public sealed class LatencyTrimmingProvider : IWaveProvider
    {
        private readonly BufferedWaveProvider _source;
        private readonly int _maxBytes;
        private readonly int _targetBytes;
        private byte[] _discardBuffer = Array.Empty<byte>();

        // Atraso parado abaixo do teto. Um engasgo do PC (jogo abrindo, CPU cheia) deixa uns
        // 200 ms na fila; abaixo dos 250 do teto nada cortava, e como o áudio entra e sai no
        // mesmo ritmo, a fila ficava ali até o fim da live, com o som atrás do vídeo. A medida
        // é o MENOR tamanho da fila numa janela de 2 s de áudio tocado: rajada de rede faz a
        // fila descer perto de zero a cada ciclo e não é cortada; atraso parado nunca desce.
        // A janela conta áudio tocado, não relógio, para a regra não depender de timer.
        private static readonly TimeSpan StandingWindow = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan StandingSlack = TimeSpan.FromMilliseconds(40);
        private readonly int _windowBytes;
        private readonly int _slackBytes;
        private int _consumedInWindow;
        private int _minBufferedInWindow = int.MaxValue;

        public WaveFormat WaveFormat => _source.WaveFormat;

        public LatencyTrimmingProvider(BufferedWaveProvider source, TimeSpan maxLatency, TimeSpan targetLatency)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));

            _maxBytes = BytesFor(source.WaveFormat, maxLatency);
            _targetBytes = BytesFor(source.WaveFormat, targetLatency);
            _windowBytes = BytesFor(source.WaveFormat, StandingWindow);
            _slackBytes = BytesFor(source.WaveFormat, StandingSlack);
        }

        private static int BytesFor(WaveFormat format, TimeSpan duration)
        {
            var bytes = (int)(format.AverageBytesPerSecond * duration.TotalSeconds);
            return bytes - (bytes % format.BlockAlign); // sempre em amostras inteiras
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            TrimIfBehind();
            TrimStandingDelay(count);
            return _source.Read(buffer, offset, count);
        }

        private void TrimStandingDelay(int aboutToRead)
        {
            int buffered = _source.BufferedBytes;
            if (buffered < _minBufferedInWindow) _minBufferedInWindow = buffered;

            _consumedInWindow += aboutToRead;
            if (_consumedInWindow < _windowBytes) return;

            // Só o que sobrou parado a janela inteira sai, até o alvo: a fila nunca esteve
            // abaixo do mínimo, então cortar isso não abre buraco no som.
            if (_minBufferedInWindow > _targetBytes + _slackBytes)
                Discard(_minBufferedInWindow - _targetBytes);

            _consumedInWindow = 0;
            _minBufferedInWindow = int.MaxValue;
        }

        private void TrimIfBehind()
        {
            int excess = _source.BufferedBytes - _maxBytes;
            if (excess <= 0) return;

            // Corta até o alvo, não até o teto: senão a fila voltaria a estourar no quadro
            // seguinte e o descarte viraria constante.
            Discard(_source.BufferedBytes - _targetBytes);
        }

        private void Discard(int bytes)
        {
            bytes -= bytes % _source.WaveFormat.BlockAlign;
            if (bytes <= 0) return;

            if (_discardBuffer.Length < bytes) _discardBuffer = new byte[bytes];

            _source.Read(_discardBuffer, 0, bytes);
        }
    }
}
