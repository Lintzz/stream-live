using StreamLiveApp;
using SIPSorceryMedia.Abstractions;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// Teto de bitrate do vídeo. Cada amigo assistindo recebe uma cópia do que o host codifica,
/// então o bitrate multiplica o upload. Medido na auditoria de performance: sem teto, 1080p
/// em movimento saía a 8–12 Mbps por amigo. E o teto só funciona com a taxa certa declarada: o
/// SIPSorcery inicializa o encoder dizendo 30 fps (e recria o encoder a cada keyframe forçado),
/// enquanto a captura entrega 60 — o controle de taxa calculava o orçamento por quadro errado.
/// </summary>
public class EncoderBitrateTests
{
    private const int Width = 1280;
    private const int Height = 720;
    private const int Seconds = 3;

    public EncoderBitrateTests() => StreamManager.EnsureMediaInitialized();

    /// <summary>
    /// Pior caso realista: textura cheia de detalhe deslocando a cada quadro (como um jogo
    /// em movimento). Ruído puro, quadro a quadro, nenhum encoder consegue segurar.
    /// </summary>
    private static IEnumerable<byte[]> MovingDetail(int frames)
    {
        var rng = new Random(42);
        int texW = Width + 64, texH = Height + 64;
        var texture = new byte[texW * texH * 4];
        rng.NextBytes(texture);
        var frame = new byte[Width * Height * 4];
        for (int f = 0; f < frames; f++)
        {
            int dx = (f * 3) % 64, dy = (f * 2) % 64;
            for (int y = 0; y < Height; y++)
                Buffer.BlockCopy(texture, ((y + dy) * texW + dx) * 4, frame, y * Width * 4, Width * 4);
            yield return frame;
        }
    }

    [Fact]
    public void MovingContentStaysUnderTheBitrateCapEvenAcrossForcedKeyFrames()
    {
        using var encoder = StreamManager.CreateH264Encoder();
        int frames = StreamManager.TargetFps * Seconds;
        long bits = 0;
        int i = 0;
        var sizes = new List<int>();

        foreach (var frame in MovingDetail(frames))
        {
            // Keyframe forçado no meio, como o app faz a cada 2 s e quando alguém entra: é
            // aí que o encoder é recriado e voltaria a nascer com a taxa errada.
            if (i == frames / 2) StreamManager.ForceIdr(encoder);

            StreamManager.PrepareEncoder(encoder, Width, Height);
            var encoded = encoder.EncodeVideo(Width, Height, frame, VideoPixelFormatsEnum.Bgra, VideoCodecsEnum.H264);
            if (encoded != null) { bits += encoded.Length * 8L; sizes.Add(encoded.Length); }
            i++;
        }

        // Conta o fluxo entre keyframes: os dois keyframes (o inicial e o forçado) ficam fora.
        // Numa textura aleatória nenhum keyframe sai pequeno — aqui eles têm ~300 KB e, numa
        // janela de 3 s, somariam ~1,6 Mbps de excesso que não existe num jogo de verdade
        // (keyframe de conteúdo real comprime). O que o teste trava é o teto do fluxo: sem
        // teto e com a taxa errada, este mesmo número dava ~121 Mbps.
        var keyFrames = sizes.OrderByDescending(x => x).Take(2).Sum(x => x * 8L);
        double kbps = (bits - keyFrames) / 1000.0 / Seconds;
        Assert.True(kbps <= StreamManager.MaxVideoKbps * 1.10,
            $"{kbps:F0} kbps para um teto de {StreamManager.MaxVideoKbps} kbps; maiores quadros (KB): " +
            string.Join(", ", sizes.OrderByDescending(x => x).Take(4).Select(x => x / 1024)) +
            $"; mediana {sizes.OrderBy(x => x).ElementAt(sizes.Count / 2) / 1024} KB");
    }
}
