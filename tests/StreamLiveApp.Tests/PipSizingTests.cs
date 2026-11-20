using System.Windows;
using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// O PiP esticava livre e o vídeo (Stretch=Uniform) sobrava em faixas pretas em volta. Agora o
/// arraste da borda é corrigido para a proporção do vídeo: puxar qualquer lado ou canto
/// redimensiona a janela inteira, sem distorcer e sem faixa preta.
/// </summary>
public class PipSizingTests
{
    private const double Ratio169 = 16.0 / 9.0;

    // Códigos do WM_SIZING (wParam): de que borda o usuário está puxando.
    private const int Left = 1, Right = 2, Top = 3, TopLeft = 4, TopRight = 5,
        Bottom = 6, BottomLeft = 7, BottomRight = 8;

    [Fact]
    public void DraggingTheRightEdgeAdjustsTheHeight()
    {
        var fixedRect = PipWindow.FitToAspect(new Rect(100, 100, 640, 200), Right, Ratio169, 160);

        Assert.Equal(new Rect(100, 100, 640, 360), fixedRect);
    }

    [Fact]
    public void DraggingTheBottomEdgeAdjustsTheWidth()
    {
        var fixedRect = PipWindow.FitToAspect(new Rect(100, 100, 320, 360), Bottom, Ratio169, 160);

        Assert.Equal(new Rect(100, 100, 640, 360), fixedRect);
    }

    [Fact]
    public void DraggingTheTopLeftCornerKeepsTheOppositeCornerInPlace()
    {
        // Puxando o canto de cima à esquerda, o canto de baixo à direita não pode andar.
        var fixedRect = PipWindow.FitToAspect(new Rect(0, 100, 640, 300), TopLeft, Ratio169, 160);

        Assert.Equal(640, fixedRect.Right);
        Assert.Equal(400, fixedRect.Bottom);
        Assert.Equal(Ratio169, fixedRect.Width / fixedRect.Height, 2);
    }

    [Fact]
    public void CornerFollowsTheLargerPull()
    {
        // Canto puxado mais para baixo que para o lado: quem manda é a altura.
        var fixedRect = PipWindow.FitToAspect(new Rect(0, 0, 320, 360), BottomRight, Ratio169, 160);

        Assert.Equal(new Rect(0, 0, 640, 360), fixedRect);
    }

    [Theory]
    [InlineData(Left)]
    [InlineData(Top)]
    [InlineData(TopRight)]
    [InlineData(BottomLeft)]
    public void EveryEdgeEndsInTheVideoRatio(int edge)
    {
        var fixedRect = PipWindow.FitToAspect(new Rect(500, 500, 400, 400), edge, Ratio169, 160);

        Assert.Equal(Ratio169, fixedRect.Width / fixedRect.Height, 2);
    }

    [Fact]
    public void NeverShrinksBelowTheMinimumWidth()
    {
        var fixedRect = PipWindow.FitToAspect(new Rect(0, 0, 40, 20), BottomRight, Ratio169, 160);

        Assert.Equal(160, fixedRect.Width);
        Assert.Equal(90, fixedRect.Height);
    }

    [Theory]
    [InlineData(1920, 1080, 320, 180)]   // 16:9
    [InlineData(2560, 1080, 320, 135)]   // ultrawide
    [InlineData(1080, 1920, 320, 569)]   // monitor em pé
    [InlineData(0, 0, 320, 180)]         // sem vídeo ainda: 16:9 de fábrica
    public void HeightFollowsTheVideo(int videoWidth, int videoHeight, double width, double expectedHeight)
    {
        Assert.Equal(expectedHeight, PipWindow.HeightFor(width, PipWindow.AspectOf(videoWidth, videoHeight)), 0);
    }
}
