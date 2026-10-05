using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace StreamLiveApp
{
    /// <summary>
    /// ItemsControl cuja árvore de automação sai sempre dos elementos que estão na tela.
    ///
    /// O ItemsControl comum guarda um par de automação por item e não o troca quando os
    /// containers são recriados (Refresh do CollectionView, reordenação). Na lista de amigos isso
    /// acontecia ao abrir uma live: o foco do teclado ia para o card novo, a árvore apontava para
    /// o antigo, e o leitor de tela anunciava o nome da janela em vez de "Diego, ao vivo".
    /// Sem virtualização e com poucos itens, ler os filhos da árvore visual é barato.
    /// </summary>
    public class LiveItemsControl : ItemsControl
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new LiveItemsControlAutomationPeer(this);

        private sealed class LiveItemsControlAutomationPeer : FrameworkElementAutomationPeer
        {
            public LiveItemsControlAutomationPeer(LiveItemsControl owner) : base(owner) { }

            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

            protected override string GetClassNameCore() => nameof(LiveItemsControl);
        }
    }
}
