using StreamLiveApp;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// Intra-refresh no lugar do keyframe sob demanda (2026-10-06). O log detalhado de uma live real
/// mostrou 177 congelamentos de ~1,1 s: perda de pacote → viewer retém a imagem e pede keyframe →
/// o keyframe (~127 KB de uma vez) sai justo com a rede cheia e se perde também. Com
/// intra-refresh o encoder renova a imagem aos poucos, numa faixa que varre o quadro, e o
/// decoder se recupera sozinho, sem rajada e sem precisar parar a imagem.
///
/// Tudo aqui roda com o x264 e o decoder do FFmpeg de verdade: opção com nome errado no
/// x264-params é ignorada em silêncio, e só o fluxo real mostra se o decoder se recupera.
/// </summary>
public class IntraRefreshTests
{
    private const int Width = 640;
    private const int Height = 360;

    public IntraRefreshTests() => StreamManager.EnsureMediaInitialized();

    /// <summary>Textura com detalhe deslocando a cada quadro: um quadro perdido estraga o que vem depois.</summary>
    private static List<byte[]> MovingFrames(int count)
    {
        var rng = new Random(11);
        int texW = Width + 128, texH = Height + 128;
        var texture = new byte[texW * texH * 4];
        // Blocos de cor em vez de ruído puro: comprime como conteúdo de verdade.
        for (int by = 0; by < texH; by += 8)
        for (int bx = 0; bx < texW; bx += 8)
        {
            byte r = (byte)rng.Next(256), g = (byte)rng.Next(256), b = (byte)rng.Next(256);
            for (int y = by; y < Math.Min(by + 8, texH); y++)
            for (int x = bx; x < Math.Min(bx + 8, texW); x++)
            {
                int o = (y * texW + x) * 4;
                texture[o] = b; texture[o + 1] = g; texture[o + 2] = r; texture[o + 3] = 255;
            }
        }

        var frames = new List<byte[]>(count);
        for (int f = 0; f < count; f++)
        {
            var frame = new byte[Width * Height * 4];
            int dx = (f * 2) % 128, dy = f % 128;
            for (int y = 0; y < Height; y++)
                Buffer.BlockCopy(texture, ((y + dy) * texW + dx) * 4, frame, y * Width * 4, Width * 4);
            frames.Add(frame);
        }
        return frames;
    }

    private static List<byte[]> Encode(List<byte[]> frames)
    {
        using var encoder = StreamManager.CreateH264Encoder();
        var encoded = new List<byte[]>();
        foreach (var frame in frames)
        {
            StreamManager.PrepareEncoder(encoder, Width, Height);
            var e = encoder.EncodeVideo(Width, Height, frame, VideoPixelFormatsEnum.Bgra, VideoCodecsEnum.H264);
            Assert.True(e != null && e.Length > 0, "o encoder deixou de entregar um quadro");
            encoded.Add(e!);
        }
        return encoded;
    }

    /// <summary>Decodifica pulando os quadros em <paramref name="drop"/>; devolve um quadro (ou null) por entrada.</summary>
    private static List<byte[]?> Decode(List<byte[]> encoded, ISet<int> drop)
    {
        using var decoder = StreamManager.CreateH264Encoder();
        var output = new List<byte[]?>();
        for (int i = 0; i < encoded.Count; i++)
        {
            if (drop.Contains(i)) { output.Add(null); continue; }
            var samples = decoder.DecodeVideo(encoded[i], VideoPixelFormatsEnum.Bgr, VideoCodecsEnum.H264)?.ToList();
            output.Add(samples != null && samples.Count > 0 ? samples[0].Sample : null);
        }
        return output;
    }

    private static double MeanAbsDiff(byte[] a, byte[] b)
    {
        long sum = 0;
        for (int i = 0; i < a.Length; i++) sum += Math.Abs(a[i] - b[i]);
        return (double)sum / a.Length;
    }

    [Fact]
    public void OnlyTheFirstFrameIsAKeyFrameAndNoFrameIsABurst()
    {
        int period = StreamManager.IntraRefreshPeriodFrames;
        var encoded = Encode(MovingFrames(period * 5));

        Assert.True(StreamManager.ContainsIdrSlice(encoded[0]));
        Assert.DoesNotContain(encoded.Skip(1), StreamManager.ContainsIdrSlice);

        // Sem IDR no meio, nenhum quadro é uma rajada: o maior fica perto dos outros. Com o
        // keyframe periódico, o IDR era várias vezes a mediana. O primeiro ciclo fica de fora:
        // logo depois do IDR inicial o controle de taxa ainda está subindo (medido: 5–22 KB no
        // 1º ciclo, 3–7 KB do 2º em diante).
        var sizes = encoded.Skip(period).Select(e => e.Length).OrderBy(x => x).ToList();
        int median = sizes[sizes.Count / 2];
        Assert.True(sizes[^1] <= median * 3,
            $"maior quadro {sizes[^1] / 1024} KB contra mediana {median / 1024} KB");
    }

    [Fact]
    public void EachRefreshCycleIsAnnouncedWithARecoveryPoint()
    {
        int period = StreamManager.IntraRefreshPeriodFrames;
        var encoded = Encode(MovingFrames(period * 5));

        var marked = Enumerable.Range(0, encoded.Count).Where(i => StreamManager.ContainsRecoveryPointSei(encoded[i])).ToList();

        // O viewer usa este aviso para saber que a host se recupera sozinha — sem ele, cairia
        // no comportamento antigo (reter a imagem e pedir keyframe a cada perda).
        Assert.True(marked.Count >= 2, $"recovery point em {string.Join(",", marked)}");
        Assert.All(marked.Zip(marked.Skip(1), (a, b) => b - a), gap => Assert.Equal(period, gap));
    }

    [Fact]
    public void DecoderHealsByItselfAfterALostFrameWithoutStopping()
    {
        int period = StreamManager.IntraRefreshPeriodFrames;
        var frames = MovingFrames(period * 5);
        var encoded = Encode(frames);
        int lost = period + period / 3; // no meio de uma varredura

        var clean = Decode(encoded, new HashSet<int>());
        var damaged = Decode(encoded, new HashSet<int> { lost });

        // O decoder não para: todo quadro depois da perda sai — nada de imagem retida.
        for (int i = lost + 1; i < encoded.Count; i++)
            Assert.True(damaged[i] != null, $"o decoder parou de entregar no quadro {i}");

        // A perda estraga mesmo a imagem logo depois (senão o teste não provaria nada)...
        Assert.True(MeanAbsDiff(clean[lost + 1]!, damaged[lost + 1]!) > 1.0,
            "o quadro perdido não fez diferença — conteúdo parado demais para o teste");

        // ...e volta a ser idêntica ao fluxo limpo. O x264 garante a cura na varredura que
        // COMEÇA depois da perda: perda no meio de um ciclo só some no fim do seguinte (medido:
        // período 60, perda no quadro 80, diferença 33 → 0,0 no quadro 161; período 30, perda no 40, 0,0 no 89). Pior caso, dois
        // períodos — por isso o período é curto.
        int healed = lost + 2 * period;
        double diff = MeanAbsDiff(clean[healed]!, damaged[healed]!);
        Assert.True(diff < 0.5, $"diferença média {diff:F2} no quadro {healed}, uma varredura depois da perda");
    }

    [Theory]
    [InlineData(true, false)]   // host com intra-refresh: segue mostrando, sem pedir keyframe
    [InlineData(false, true)]   // host antiga (até a 2.3.0): retém a imagem e pede keyframe, como antes
    public void LossResponseDependsOnWhetherTheHostRefreshes(bool hostRefreshes, bool holdAndRequest)
    {
        Assert.Equal(holdAndRequest, StreamManager.ShouldHoldForKeyFrameOnLoss(hostRefreshes));
    }

    [Fact]
    public void RecoveryPointIsNotConfusedWithOtherSei()
    {
        // SEI só com "user data unregistered" (tipo 5), como o texto de opções do x264.
        var userData = new byte[] { 0, 0, 0, 1, 0x06, 0x05, 0x02, 0xAA, 0xBB, 0x80 };
        Assert.False(StreamManager.ContainsRecoveryPointSei(userData));

        // Tipo 5 seguido de recovery point (tipo 6) na mesma NAL.
        var both = new byte[] { 0, 0, 0, 1, 0x06, 0x05, 0x02, 0xAA, 0xBB, 0x06, 0x01, 0x84, 0x80 };
        Assert.True(StreamManager.ContainsRecoveryPointSei(both));

        Assert.False(StreamManager.ContainsRecoveryPointSei(new byte[] { 0, 0, 0, 1, 0x41, 0x9A }));
        Assert.False(StreamManager.ContainsRecoveryPointSei(Array.Empty<byte>()));
    }


}
