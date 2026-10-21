using System.Threading;
using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// Ponte com o widget da barra de tarefas. Os eventos nomeados são os reais do Windows, com um
/// prefixo próprio por teste para não cruzar com o app aberto na máquina.
/// </summary>
public class WidgetBridgeTests
{
    private static string NewPrefix() => $"StreamLiveTests.{Guid.NewGuid():N}.";

    [Fact]
    public void TransmitSignalFromWidgetReachesTheApp()
    {
        var prefix = NewPrefix();
        using var requested = new ManualResetEventSlim();
        using var bridge = new WidgetBridge(requested.Set, () => { }, prefix);

        // O que o widget faz: abre o evento existente e sinaliza.
        Assert.True(EventWaitHandle.TryOpenExisting(prefix + "Transmitir", out var signal));
        using (signal) signal.Set();

        Assert.True(requested.Wait(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void CloseSignalFromWidgetReachesTheApp()
    {
        var prefix = NewPrefix();
        using var requested = new ManualResetEventSlim();
        using var bridge = new WidgetBridge(() => { }, requested.Set, prefix);

        Assert.True(EventWaitHandle.TryOpenExisting(prefix + "Encerrar", out var signal));
        using (signal) signal.Set();

        Assert.True(requested.Wait(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void LiveStateIsVisibleToTheWidgetAndAnnounced()
    {
        var prefix = NewPrefix();
        using var bridge = new WidgetBridge(() => { }, () => { }, prefix);

        Assert.True(EventWaitHandle.TryOpenExisting(prefix + "AoVivo", out var live));
        Assert.True(EventWaitHandle.TryOpenExisting(prefix + "EstadoMudou", out var changed));
        using (live)
        using (changed)
        {
            // A abertura já avisa (o widget pode estar mostrando uma instância que caiu).
            Assert.True(changed.WaitOne(0));
            Assert.False(live.WaitOne(0));

            bridge.SetLive(true);
            Assert.True(changed.WaitOne(0));
            Assert.True(live.WaitOne(0));

            bridge.SetLive(false);
            Assert.True(changed.WaitOne(0));
            Assert.False(live.WaitOne(0));

            bridge.SetLive(true);
            Assert.True(changed.WaitOne(0));

            // Fechar o app tira o selo na hora.
            bridge.Dispose();
            Assert.True(changed.WaitOne(0));
            Assert.False(live.WaitOne(0));
        }
    }

    [Fact]
    public void SignalsOnlyExistWhileTheAppIsOpen()
    {
        // É por eles que o widget decide entre sinalizar e abrir o app com --transmitir.
        var prefix = NewPrefix();
        var bridge = new WidgetBridge(() => { }, () => { }, prefix);
        Assert.True(EventWaitHandle.TryOpenExisting(prefix + "Transmitir", out var open));
        open!.Dispose();

        bridge.Dispose();
        Assert.False(EventWaitHandle.TryOpenExisting(prefix + "Transmitir", out _));
    }

    [Fact]
    public void StaleRequestFromBeforeTheAppOpenedDoesNotFire()
    {
        var prefix = NewPrefix();

        // Pedido que ficou sinalizado (o widget ainda segura o evento de uma instância anterior).
        using var stale = new EventWaitHandle(true, EventResetMode.AutoReset, prefix + "Encerrar");

        var fired = 0;
        using var bridge = new WidgetBridge(() => { }, () => Interlocked.Increment(ref fired), prefix);

        Thread.Sleep(300);
        Assert.Equal(0, Volatile.Read(ref fired));
    }

    /// <summary>O que o widget faz: abre a memória pelo nome, lê o tamanho e o texto.</summary>
    private static string ReadViewersLikeTheWidget(string prefix)
    {
        using var map = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting(prefix + "Espectadores");
        using var view = map.CreateViewAccessor(0, WidgetBridge.ViewersCapacity);
        var length = view.ReadInt32(0);
        var bytes = new byte[length];
        view.ReadArray(4, bytes, 0, length);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    [Fact]
    public void ViewersReachTheWidgetAndAreClearedWhenTheLiveEnds()
    {
        var prefix = NewPrefix();
        using var bridge = new WidgetBridge(() => { }, () => { }, prefix);
        Assert.True(EventWaitHandle.TryOpenExisting(prefix + "EstadoMudou", out var changed));
        using (changed)
        {
            Assert.Equal("0", ReadViewersLikeTheWidget(prefix));

            bridge.SetLive(true);
            changed.WaitOne(0);
            bridge.SetViewers(new[] { "Ana", "Edu" });
            Assert.True(changed.WaitOne(0));
            Assert.Equal("2\nAna\nEdu", ReadViewersLikeTheWidget(prefix));

            bridge.SetViewers(new[] { "Edu" });
            Assert.Equal("1\nEdu", ReadViewersLikeTheWidget(prefix));

            bridge.SetLive(false);
            Assert.Equal("0", ReadViewersLikeTheWidget(prefix));
        }
    }

    [Fact]
    public void StartingTheLiveKeepsAViewerWhoJoinedFirst()
    {
        // O amigo pode entrar entre o anúncio da live e o SetLive(true).
        var prefix = NewPrefix();
        using var bridge = new WidgetBridge(() => { }, () => { }, prefix);

        bridge.SetViewers(new[] { "Ana" });
        bridge.SetLive(true);

        Assert.Equal("1\nAna", ReadViewersLikeTheWidget(prefix));
    }

    [Fact]
    public void LongViewerListKeepsTheRealCountAndFits()
    {
        var names = Enumerable.Range(0, 500).Select(i => $"Amigo com apelido comprido {i}").ToArray();

        var bytes = WidgetBridge.EncodeViewers(names);

        Assert.True(bytes.Length <= WidgetBridge.ViewersCapacity - 4);
        Assert.StartsWith("500\n", System.Text.Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void LineBreakInANicknameDoesNotBecomeAnotherViewer()
    {
        var text = System.Text.Encoding.UTF8.GetString(WidgetBridge.EncodeViewers(new[] { "Ana\nMaria" }));
        Assert.Equal("1\nAna Maria", text);
    }

    [Theory]
    [InlineData(false, true, false, WidgetTransmitAction.OpenBroadcastDialog)]
    [InlineData(true, true, false, WidgetTransmitAction.BringToFront)]   // já no ar
    [InlineData(false, false, false, WidgetTransmitAction.BringToFront)] // porta 8080 ocupada ou encerrando
    [InlineData(false, true, true, WidgetTransmitAction.BringToFront)]   // modal já aberto
    public void TransmitRequestOnlyOpensTheDialogWhenItCan(bool broadcasting, bool canBroadcast, bool dialogOpen, WidgetTransmitAction expected)
        => Assert.Equal(expected, WidgetBridge.DecideTransmitAction(broadcasting, canBroadcast, dialogOpen));
}
