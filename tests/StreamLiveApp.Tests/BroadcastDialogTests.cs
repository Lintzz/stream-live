using System.Drawing;
using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// O modal de transmitir: a frase que diz quem vai ver a live e as miniaturas das telas.
/// A frase é o único aviso de que "ninguém marcado" sai pública — se ela mentir, a pessoa
/// transmite para quem não queria.
/// </summary>
public class BroadcastDialogTests
{
    [Fact]
    public void NobodyMarkedSaysPublic()
    {
        Assert.StartsWith("Pública", BroadcastDialog.DescribeAudience(Array.Empty<string>()));
    }

    [Theory]
    [InlineData(new[] { "Ana" }, "Privada — só Ana vê esta live.")]
    [InlineData(new[] { "Ana", "Diego" }, "Privada — só Ana e Diego veem esta live.")]
    [InlineData(new[] { "Ana", "Diego", "Bruno" }, "Privada — só Ana, Diego e Bruno veem esta live.")]
    [InlineData(new[] { "Ana", "Diego", "Bruno", "Carla", "Lia" }, "Privada — só Ana, Diego e mais 3 veem esta live.")]
    public void MarkedFriendsAreNamed(string[] names, string expectedStart)
    {
        Assert.StartsWith(expectedStart, BroadcastDialog.DescribeAudience(names));
    }

    [Theory]
    [InlineData(1920, 1080, 480, 270)]   // 16:9 ocupa a caixa toda
    [InlineData(1080, 1920, 152, 270)]   // monitor em pé: limitado pela altura
    [InlineData(3440, 1440, 480, 201)]   // ultrawide: limitado pela largura
    public void ThumbnailKeepsTheScreenProportion(int width, int height, int expectedWidth, int expectedHeight)
    {
        var size = WindowHelper.ThumbnailSize(new Size(width, height), 480, 270);

        Assert.Equal(new Size(expectedWidth, expectedHeight), size);
    }

    [Fact]
    public void InvalidScreenGivesNoThumbnailInsteadOfThrowing()
    {
        Assert.Null(WindowHelper.CaptureThumbnail(Rectangle.Empty, 480, 270));
    }

    [Fact]
    public void DemoThumbnailIsDrawnAtTheRequestedSize()
    {
        var (pixels, width, height) = DemoFeed.RenderStill(DemoScene.Plataforma, 1.0);

        var thumbnail = WindowHelper.ThumbnailFromPixels(pixels, width, height, 480, 270);

        Assert.NotNull(thumbnail);
        Assert.Equal(480, thumbnail!.PixelWidth);
        Assert.Equal(270, thumbnail.PixelHeight);
    }

    [Fact]
    public void PrimaryScreenIsTheOneAtTheOrigin()
    {
        var primary = new CaptureSource { Title = "Tela 1", ScreenBounds = new Rectangle(0, 0, 1920, 1080) };
        var second = new CaptureSource { Title = "Tela 2", ScreenBounds = new Rectangle(-1920, 0, 1920, 1080) };

        Assert.Equal("1920 × 1080 · principal", primary.Detail);
        Assert.Equal("1920 × 1080", second.Detail);
    }
}
