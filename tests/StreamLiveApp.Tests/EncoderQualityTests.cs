using System.Text;
using StreamLiveApp;
using SIPSorceryMedia.Abstractions;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// Ferramentas do x264 que o preset ultrafast desliga e que o app religa. Sem deblock e sem AQ,
/// movimento vira bloco visível; sem busca de movimento (hex + subme 2), movimento rápido não
/// comprime e bate no teto de bitrate — medido em 1080p: 8,4–9 Mbps no teto contra 4,5 Mbps
/// com a busca, e o pior trecho ~5 dB melhor. O intra-refresh (keyint = período da varredura)
/// troca o IDR — uma rajada de ~127 KB — por uma faixa intra que renova a imagem aos poucos.
///
/// O x264 grava as opções efetivas em texto no primeiro quadro (SEI). É isso que o teste lê:
/// opção com nome errado no x264-params é ignorada em silêncio, e a imagem só pioraria em campo.
/// </summary>
public class EncoderQualityTests
{
    public EncoderQualityTests() => StreamManager.EnsureMediaInitialized();

    private static string EncoderOptions()
    {
        const int w = 320, h = 240;
        using var encoder = StreamManager.CreateH264Encoder();
        var frame = new byte[w * h * VideoCapturer.BytesPerPixel];
        new Random(7).NextBytes(frame);

        StreamManager.PrepareEncoder(encoder, w, h);
        var encoded = encoder.EncodeVideo(w, h, frame, VideoPixelFormatsEnum.Bgra, VideoCodecsEnum.H264);
        Assert.NotNull(encoded);

        var text = Encoding.ASCII.GetString(encoded!);
        int start = text.IndexOf("options:", StringComparison.Ordinal);
        Assert.True(start >= 0, "o x264 não gravou as opções no primeiro quadro");
        int end = text.IndexOf('\0', start);
        return text.Substring(start, (end > start ? end : text.Length) - start);
    }

    [Theory]
    [InlineData("deblock=1:0:0")]
    [InlineData("aq=1:")]
    [InlineData("8x8dct=1")]
    [InlineData("me=hex")]
    [InlineData("subme=2")]
    [InlineData("keyint=30")]
    [InlineData("intra_refresh=1")]
    [InlineData("vbv_maxrate=5000")]
    [InlineData("vbv_bufsize=1000")]
    public void EncoderRunsWithTheMeasuredQualityTools(string option)
    {
        var options = EncoderOptions();
        Assert.True(options.Split(' ').Any(o => o.StartsWith(option, StringComparison.Ordinal)),
            $"esperava {option} em: {options}");
    }
}
