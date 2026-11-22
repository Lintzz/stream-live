using System.Windows;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// A janela abria em 900×550 e o modal de transmitir (~600×650) vazava por baixo dela. Agora
/// ela abre em 1200×760, sem passar da tela em notebook, e o modal é puxado para dentro da
/// tela quando a janela foi diminuída à mão.
/// </summary>
public class WindowSizingTests
{
    [Theory]
    [InlineData(1920, 1040, 1200, 760)]  // 1080p com barra de tarefas: cabe inteiro
    [InlineData(3840, 2120, 1200, 760)]  // 4K: não cresce além do padrão
    [InlineData(1366, 728, 1200, 708)]   // notebook 1366×768: encolhe só a altura, com folga de 20
    [InlineData(1024, 600, 1004, 580)]   // tela bem pequena: cabe nos dois sentidos
    public void InitialSizeFitsTheWorkArea(double areaWidth, double areaHeight, double expectedWidth, double expectedHeight)
    {
        var size = WindowHelper.FitToWorkArea(new Size(1200, 760), new Rect(0, 0, areaWidth, areaHeight));

        Assert.Equal(new Size(expectedWidth, expectedHeight), size);
    }

    [Fact]
    public void DialogInsideTheScreenStaysWhereItIs()
    {
        var position = WindowHelper.ClampToArea(new Rect(300, 200, 620, 650), new Rect(0, 0, 1920, 1040));

        Assert.Equal(new Point(300, 200), position);
    }

    [Fact]
    public void DialogLeakingBelowIsPulledUp()
    {
        // Janela pequena no meio da tela: o modal centralizado nela passava da barra de tarefas.
        var position = WindowHelper.ClampToArea(new Rect(650, 600, 620, 650), new Rect(0, 0, 1920, 1040));

        Assert.Equal(new Point(650, 390), position);
    }

    [Fact]
    public void DialogLeakingOnTheSecondScreenStaysOnIt()
    {
        // Segunda tela à esquerda da principal: coordenadas negativas.
        var position = WindowHelper.ClampToArea(new Rect(-100, -50, 620, 650), new Rect(-1920, 0, 1920, 1040));

        Assert.Equal(new Point(-620, 0), position);
    }

    [Fact]
    public void DialogBiggerThanTheScreenStartsAtItsTopLeft()
    {
        var position = WindowHelper.ClampToArea(new Rect(100, 100, 620, 900), new Rect(0, 0, 1366, 728));

        Assert.Equal(new Point(100, 0), position);
    }

    [Theory]
    [InlineData(WindowState.Maximized, ResizeMode.CanResize, 8, 1.0, 8)]      // 100%: 4 de moldura + 4 de borda extra
    [InlineData(WindowState.Maximized, ResizeMode.CanResize, 11, 1.25, 8.8)]  // 125%: pixels viram DIPs
    [InlineData(WindowState.Normal, ResizeMode.CanResize, 8, 1.0, 0)]         // janela solta não passa da tela
    [InlineData(WindowState.Maximized, ResizeMode.NoResize, 8, 1.0, 0)]       // tela cheia: sem moldura, nada vaza
    public void MaximizedWindowIsPulledInsideTheScreen(WindowState state, ResizeMode resize, int framePixels, double dpiScale, double expected)
    {
        // Maximizada com WindowChrome, a janela passa da tela pela espessura da moldura e o
        // topo da barra de título e das lives ficava cortado.
        var inset = WindowHelper.MaximizedInset(state, resize, framePixels, dpiScale);

        Assert.Equal(new Thickness(expected), inset);
    }

    [Theory]
    [InlineData(0, "AO VIVO")]
    [InlineData(1, "AO VIVO · 1")]
    [InlineData(12, "AO VIVO · 12")]
    public void TitlePillShowsWhoIsWatching(int viewers, string expected)
    {
        Assert.Equal(expected, MainWindow.LivePillText(viewers));
    }
}
