using StreamLiveApp;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// O viewer detecta pacote de vídeo perdido pela sequência RTP, pede keyframe e segura o
/// último quadro bom até a fatia IDR chegar. Sem isso, o quadro furado era decodificado com
/// ocultação de erro e o borrão se arrastava até o keyframe periódico.
/// </summary>
public class VideoLossRecoveryTests
{
    public VideoLossRecoveryTests() => StreamManager.EnsureMediaInitialized();

    [Theory]
    [InlineData(10, 10, 0)]
    [InlineData(10, 13, 3)]
    [InlineData(10, 9, -1)]
    [InlineData(65535, 0, 1)]   // a volta do contador de 16 bits não é perda
    [InlineData(65534, 1, 3)]
    [InlineData(0, 65535, -1)]  // atrasado do outro lado da volta
    public void SequenceDeltaHandlesWrapAround(int esperado, int recebido, int delta)
    {
        Assert.Equal(delta, StreamManager.SequenceDelta((ushort)esperado, (ushort)recebido));
    }

    [Fact]
    public void IdrSliceIsFoundAfterParameterSets()
    {
        // SPS, PPS e IDR — o formato em que o x264 entrega um keyframe.
        var quadro = new byte[] { 0, 0, 0, 1, 0x67, 1, 2, 0, 0, 0, 1, 0x68, 3, 0, 0, 0, 1, 0x65, 9, 9 };
        Assert.True(StreamManager.ContainsIdrSlice(quadro));
    }

    [Fact]
    public void NonIdrSliceIsNotAKeyFrame()
    {
        var quadro = new byte[] { 0, 0, 0, 1, 0x41, 0x9A, 0, 0, 3, 0x65 }; // 00 00 03 é emulação, não início
        Assert.False(StreamManager.ContainsIdrSlice(quadro));
        Assert.False(StreamManager.ContainsIdrSlice(Array.Empty<byte>()));
    }

    [Fact]
    public void RealEncoderKeyFrameIsRecognizedAndDeltaFrameIsNot()
    {
        // Trava o pedido de keyframe contra o encoder de verdade. O ForceKeyFrame() do
        // SIPSorcery 8.0.7 não produz IDR com o libx264 — foi este teste que mostrou. Se o
        // ForceIdr parar de funcionar, o viewer seguraria a imagem até o teto em toda perda.
        const int w = 320, h = 240;
        using var encoder = new FFmpegVideoEncoder(new Dictionary<string, string>
        {
            { "preset", "ultrafast" },
            { "tune", "zerolatency" }
        });

        byte[] Quadro(int semente)
        {
            var f = new byte[w * h * VideoCapturer.BytesPerPixel];
            for (int i = 0; i < f.Length; i++) f[i] = (byte)((i + semente) % 256);
            return f;
        }

        var encoded = new List<byte[]>();
        for (int i = 0; i < 12; i++)
        {
            if (i == 8) StreamManager.ForceIdr(encoder);
            var e = encoder.EncodeVideo(w, h, Quadro(i * 7), VideoPixelFormatsEnum.Bgra, VideoCodecsEnum.H264);
            if (e != null && e.Length > 0) encoded.Add(e);
        }

        // Nenhum quadro some no caminho, então o índice na lista é o do laço.
        Assert.Equal(12, encoded.Count);
        Assert.True(StreamManager.ContainsIdrSlice(encoded[0]));
        Assert.False(StreamManager.ContainsIdrSlice(encoded[7]));
        Assert.True(StreamManager.ContainsIdrSlice(encoded[8]));
        Assert.False(StreamManager.ContainsIdrSlice(encoded[9]));

        // O IDR sai de um contexto recriado, com SPS/PPS novos no meio do fluxo: o decoder do
        // viewer, que é um só para a sessão inteira, precisa seguir decodificando depois dele.
        using var decoder = new FFmpegVideoEncoder();
        foreach (var q in encoded)
        {
            Assert.NotEmpty(decoder.DecodeVideo(q, VideoPixelFormatsEnum.Bgr, VideoCodecsEnum.H264));
        }
    }
}
