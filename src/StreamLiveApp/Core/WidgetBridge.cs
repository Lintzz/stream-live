using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;

namespace StreamLiveApp
{
    /// <summary>O que fazer com um "Iniciar live" vindo do widget.</summary>
    public enum WidgetTransmitAction
    {
        /// <summary>Abre o modal de transmitir, como o clique no Transmitir.</summary>
        OpenBroadcastDialog,

        /// <summary>Só traz a janela para a frente: já está no ar, ou há um modal aberto.</summary>
        BringToFront,
    }

    /// <summary>
    /// Ponte com o Discord Voice Widget, o widget da barra de tarefas: ele pede para iniciar
    /// ou encerrar a live e mostra quando você está no ar.
    ///
    /// Por eventos nomeados do Windows, o mesmo meio que o widget usa entre as próprias
    /// instâncias, e não por rede: nada escuta porta, e nada roda enquanto ninguém sinaliza
    /// — o Windows avisa o pool de threads quando o evento dispara. O widget pode estar
    /// fechado, desligado nas configurações ou nem instalado: aqui nada muda por isso.
    ///
    /// - <c>Transmitir</c> e <c>Encerrar</c>: o widget sinaliza, o app reage. Só existem com
    ///   o app aberto, e é por eles que o widget sabe se o app está aberto.
    /// - <c>AoVivo</c>: ligado enquanto a live está no ar. Morre junto com o processo, então
    ///   um app que caiu não fica "ao vivo" para o widget.
    /// - <c>EstadoMudou</c>: o app sinaliza a cada início/fim de live, a cada viewer que entra
    ///   ou sai e ao fechar, para o widget atualizar o selo sem ficar consultando.
    /// - <c>Espectadores</c>: memória compartilhada (evento não carrega número) com quem está
    ///   assistindo, no formato de <see cref="EncodeViewers"/>.
    /// </summary>
    public sealed class WidgetBridge : IDisposable
    {
        internal const string DefaultPrefix = "StreamLiveApp.";

        /// <summary>Tamanho da área de espectadores: 4 bytes de tamanho + o texto.</summary>
        internal const int ViewersCapacity = 4096;

        private readonly EventWaitHandle _transmitSignal;
        private readonly EventWaitHandle _closeSignal;
        private readonly EventWaitHandle _liveState;
        private readonly EventWaitHandle _stateChanged;
        private readonly RegisteredWaitHandle _transmitWait;
        private readonly RegisteredWaitHandle _closeWait;
        private readonly MemoryMappedFile _viewers;
        private readonly MemoryMappedViewAccessor _viewersView;
        private bool _disposed;

        /// <param name="onTransmitRequested">Chamado no pool de threads: quem recebe marshala para a UI.</param>
        /// <param name="onCloseRequested">Idem.</param>
        /// <param name="prefix">Trocável só nos testes, para não cruzar com o app aberto na máquina.</param>
        public WidgetBridge(Action onTransmitRequested, Action onCloseRequested, string prefix = DefaultPrefix)
        {
            ArgumentNullException.ThrowIfNull(onTransmitRequested);
            ArgumentNullException.ThrowIfNull(onCloseRequested);

            _transmitSignal = new EventWaitHandle(false, EventResetMode.AutoReset, prefix + "Transmitir");
            _closeSignal = new EventWaitHandle(false, EventResetMode.AutoReset, prefix + "Encerrar");
            _liveState = new EventWaitHandle(false, EventResetMode.ManualReset, prefix + "AoVivo");
            _stateChanged = new EventWaitHandle(false, EventResetMode.AutoReset, prefix + "EstadoMudou");

            // Sem arquivo em disco: vive na memória enquanto este processo segurar o handle.
            _viewers = MemoryMappedFile.CreateOrOpen(prefix + "Espectadores", ViewersCapacity);
            _viewersView = _viewers.CreateViewAccessor(0, ViewersCapacity);
            WriteViewers(Array.Empty<string>());

            // Um pedido que ficou sinalizado de antes (o widget pediu e o app caiu, ou outra
            // instância saiu) não pode disparar sozinho agora.
            _transmitSignal.Reset();
            _closeSignal.Reset();
            _liveState.Reset();

            _transmitWait = ThreadPool.RegisterWaitForSingleObject(
                _transmitSignal, (_, _) => onTransmitRequested(), null, Timeout.Infinite, executeOnlyOnce: false);
            _closeWait = ThreadPool.RegisterWaitForSingleObject(
                _closeSignal, (_, _) => onCloseRequested(), null, Timeout.Infinite, executeOnlyOnce: false);

            // O widget pode ter mostrado "ao vivo" de uma instância anterior que caiu.
            _stateChanged.Set();
        }

        /// <summary>Publica se a live está no ar.</summary>
        public void SetLive(bool live)
        {
            if (_disposed) return;

            if (live) _liveState.Set();
            else _liveState.Reset();

            // Live encerrada não tem público. Ao começar não zera: um amigo pode ter entrado
            // entre o anúncio da live e esta chamada (quem chama publica a lista em seguida).
            if (!live) WriteViewers(Array.Empty<string>());
            _stateChanged.Set();
        }

        /// <summary>Publica quem está assistindo (apelidos, ou IP de quem não está na lista).</summary>
        public void SetViewers(IReadOnlyList<string> names)
        {
            ArgumentNullException.ThrowIfNull(names);
            if (_disposed) return;

            WriteViewers(names);
            _stateChanged.Set();
        }

        private void WriteViewers(IReadOnlyList<string> names)
        {
            var bytes = EncodeViewers(names);

            // Tamanho por último: o widget que lê no meio da escrita vê o texto anterior
            // inteiro (ou um pedaço, corrigido no sinal que vem logo a seguir).
            _viewersView.WriteArray(4, bytes, 0, bytes.Length);
            _viewersView.Write(0, bytes.Length);
        }

        /// <summary>
        /// UTF-8: a contagem na primeira linha e um nome por linha, cortado para caber em
        /// <see cref="ViewersCapacity"/>. A contagem é sempre a real, mesmo com nomes cortados.
        /// </summary>
        internal static byte[] EncodeViewers(IReadOnlyList<string> names)
        {
            var text = new StringBuilder(names.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var budget = ViewersCapacity - 4;

            foreach (var name in names)
            {
                // Quebra de linha no apelido viraria um espectador a mais do outro lado.
                var line = "\n" + name.Replace('\n', ' ').Replace('\r', ' ');
                if (Encoding.UTF8.GetByteCount(text.ToString() + line) > budget) break;
                text.Append(line);
            }

            return Encoding.UTF8.GetBytes(text.ToString());
        }

        /// <summary>
        /// Pura, para o teste: abrir o modal por cima de outro (ou de uma live no ar) faria
        /// dois modais ou uma segunda live; nesses casos o pedido só mostra a janela.
        /// </summary>
        internal static WidgetTransmitAction DecideTransmitAction(bool isBroadcasting, bool canBroadcast, bool hasOpenDialog)
            => isBroadcasting || !canBroadcast || hasOpenDialog
                ? WidgetTransmitAction.BringToFront
                : WidgetTransmitAction.OpenBroadcastDialog;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _transmitWait.Unregister(null);
            _closeWait.Unregister(null);

            // Fechando: o widget tira o selo na hora, sem esperar o processo sumir.
            _liveState.Reset();
            _stateChanged.Set();

            _transmitSignal.Dispose();
            _closeSignal.Dispose();
            _liveState.Dispose();
            _stateChanged.Dispose();
            _viewersView.Dispose();
            _viewers.Dispose();
        }
    }
}
