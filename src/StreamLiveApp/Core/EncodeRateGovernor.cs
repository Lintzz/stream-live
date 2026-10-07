using System;

namespace StreamLiveApp
{
    /// <summary>
    /// Decide se o encoder trabalha a 60 ou a 30 quadros por segundo, segundo a segundo.
    ///
    /// Existe por causa de uma live jogando Valorant (2026-10-06): com o jogo disputando o
    /// processador (e o app de propósito abaixo dele na prioridade), o encoder levava ~24 ms por
    /// quadro e pulava 23 de 53 quadros por segundo — de forma irregular. Pior: ele continuava
    /// declarado a 60, então o controle de taxa dividia o teto em 60 fatias e só 30 saíam. A live
    /// usava metade do teto (2,5 Mbps) e todo movimento virava bloco. A 30 declarados, cada quadro
    /// leva o dobro de bits (medido: 10 → 20 KB por quadro em 1080p), a cadência fica regular e o
    /// encoder sobra processador para o jogo. Fixar em 30 sempre perderia os 60 de quem dá conta.
    ///
    /// Cada troca recria o encoder (um keyframe inteiro, a rajada que o intra-refresh evita), então
    /// a regra exige alguns segundos de confirmação para descer, bem mais para tentar subir, e
    /// dobra a espera quando a tentativa de voltar a 60 falha logo.
    /// </summary>
    internal sealed class EncodeRateGovernor
    {
        public const int FullFps = 60;
        public const int ReducedFps = 30;

        /// <summary>Pulando pelo menos esta fração do capturado, o encoder não está dando conta.</summary>
        internal const double SkipRatioToReduce = 0.25;
        internal const int SecondsToReduce = 3;

        /// <summary>
        /// A 30, tempo médio por quadro que deixaria 60 caber (16,7 ms por quadro) com folga. Na
        /// área de trabalho o mesmo PC codificava 1080p a ~14 ms e pulava ~21% — ainda abaixo do
        /// limite de descida, então 60 se sustenta ali.
        /// </summary>
        internal const double MaxEncodeMsToRestore = 13.0;
        internal const int RestoreAfterSeconds = 20;

        /// <summary>Voltar a 30 em menos que isto depois de subir conta como tentativa falha.</summary>
        internal const int FailedTryWindowSeconds = 30;
        private const int MaxRestoreWaitSeconds = RestoreAfterSeconds * 16;

        /// <summary>Abaixo disso a tela está quase parada (reemissão): pular quadro não diz nada.</summary>
        private const int MinCapturedToJudge = 20;

        public int Fps { get; private set; } = FullFps;

        private int _heavySeconds;
        private int _lightSeconds;
        private int _restoreWait = RestoreAfterSeconds;
        private int _secondsSinceRestore = int.MaxValue;

        /// <summary>Um segundo de live com alguém assistindo. Devolve true quando o alvo mudou.</summary>
        public bool OnSecond(int captured, int skippedBusy, int encoded, double avgEncodeMs)
        {
            if (_secondsSinceRestore != int.MaxValue) _secondsSinceRestore++;

            if (Fps == FullFps)
            {
                // Ficou bastante tempo a 60 depois da última volta: a espera recomeça do mínimo.
                if (_secondsSinceRestore > FailedTryWindowSeconds * 4) _restoreWait = RestoreAfterSeconds;

                bool heavy = captured >= MinCapturedToJudge && skippedBusy >= captured * SkipRatioToReduce;
                _heavySeconds = heavy ? _heavySeconds + 1 : 0;
                if (_heavySeconds < SecondsToReduce) return false;

                if (_secondsSinceRestore <= FailedTryWindowSeconds)
                    _restoreWait = Math.Min(_restoreWait * 2, MaxRestoreWaitSeconds);

                Fps = ReducedFps;
                _heavySeconds = 0;
                _lightSeconds = 0;
                return true;
            }

            bool light = encoded > 0 && avgEncodeMs <= MaxEncodeMsToRestore;
            _lightSeconds = light ? _lightSeconds + 1 : 0;
            if (_lightSeconds < _restoreWait) return false;

            Fps = FullFps;
            _lightSeconds = 0;
            _heavySeconds = 0;
            _secondsSinceRestore = 0;
            return true;
        }

        /// <summary>
        /// Este quadro capturado vai para o encoder? A 30, pega um a cada ~33 ms em vez de deixar
        /// o encoder ocupado pular quadros ao acaso. A folga de 4 ms absorve a variação da captura.
        /// </summary>
        internal static bool ShouldTakeFrame(double msSinceLastTaken, int fps)
            => msSinceLastTaken >= 1000.0 / fps - 4.0;
    }
}
