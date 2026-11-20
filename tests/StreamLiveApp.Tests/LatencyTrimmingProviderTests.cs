using NAudio.Wave;
using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// O trimmer é o que impede o áudio de ir ficando cada vez mais atrás do vídeo ao longo da
/// sessão. É aritmética pura de buffer — nunca tinha teste, e é fácil de errar por um
/// múltiplo de BlockAlign.
/// </summary>
public class LatencyTrimmingProviderTests
{
    private static readonly WaveFormat Format = new(44100, 16, 2); // BlockAlign = 4

    private static BufferedWaveProvider NewSource()
        => new(Format) { BufferDuration = TimeSpan.FromSeconds(5), DiscardOnBufferOverflow = true };

    private static byte[] Silence(TimeSpan duration)
        => new byte[BytesFor(duration)];

    private static int BytesFor(TimeSpan duration)
    {
        var bytes = (int)(Format.AverageBytesPerSecond * duration.TotalSeconds);
        return bytes - (bytes % Format.BlockAlign);
    }

    [Fact]
    public void PassesAudioThroughUntouchedWhenTheBufferIsShort()
    {
        var source = NewSource();
        var trimmer = new LatencyTrimmingProvider(source,
            TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(80));

        var payload = Silence(TimeSpan.FromMilliseconds(50));
        for (int i = 0; i < payload.Length; i++) payload[i] = (byte)(i % 251);
        source.AddSamples(payload, 0, payload.Length);

        var read = new byte[payload.Length];
        int n = trimmer.Read(read, 0, read.Length);

        Assert.Equal(payload.Length, n);
        Assert.Equal(payload, read);
    }

    [Fact]
    public void DiscardsDownToTheTargetOnceTheBufferPassesTheCeiling()
    {
        var source = NewSource();
        var max = TimeSpan.FromMilliseconds(250);
        var target = TimeSpan.FromMilliseconds(80);
        var trimmer = new LatencyTrimmingProvider(source, max, target);

        // 1s acumulado: bem acima do teto de 250ms.
        var backlog = Silence(TimeSpan.FromSeconds(1));
        source.AddSamples(backlog, 0, backlog.Length);

        trimmer.Read(new byte[4], 0, 4);

        // Corta até o alvo, não até o teto: cortar até o teto faria a fila estourar de novo
        // no quadro seguinte e o descarte viraria constante (e audível).
        int expected = BytesFor(target) - 4;
        Assert.InRange(source.BufferedBytes, expected - Format.BlockAlign, expected + Format.BlockAlign);
    }

    [Fact]
    public void DoesNotTrimExactlyAtTheCeiling()
    {
        var source = NewSource();
        var trimmer = new LatencyTrimmingProvider(source,
            TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(80));

        var exactly = Silence(TimeSpan.FromMilliseconds(250));
        source.AddSamples(exactly, 0, exactly.Length);
        int before = source.BufferedBytes;

        trimmer.Read(new byte[4], 0, 4);

        Assert.Equal(before - 4, source.BufferedBytes);
    }

    [Fact]
    public void AlwaysLeavesTheBufferOnAWholeSampleBoundary()
    {
        var source = NewSource();
        var trimmer = new LatencyTrimmingProvider(source,
            TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(80));

        var backlog = Silence(TimeSpan.FromMilliseconds(900));
        source.AddSamples(backlog, 0, backlog.Length);

        trimmer.Read(new byte[Format.BlockAlign], 0, Format.BlockAlign);

        // Sobrar meia amostra estoura os canais: o resto do stream sairia com L e R trocados.
        Assert.Equal(0, source.BufferedBytes % Format.BlockAlign);
    }

    /// <summary>
    /// O bug: um engasgo do PC (jogo abrindo, CPU cheia) deixa ~200 ms na fila. Abaixo do teto
    /// de 250 nada cortava, e como o áudio entra e sai no mesmo ritmo, a fila ficava parada ali
    /// para sempre, com o som atrasado em relação ao vídeo até fechar a live.
    /// </summary>
    [Fact]
    public void StandingDelayBelowTheCeilingIsCutBackToTheTarget()
    {
        var source = NewSource();
        var target = TimeSpan.FromMilliseconds(80);
        var trimmer = new LatencyTrimmingProvider(source, TimeSpan.FromMilliseconds(250), target);

        var engasgo = Silence(TimeSpan.FromMilliseconds(200));
        source.AddSamples(engasgo, 0, engasgo.Length);

        // Três segundos de entrada e saída no mesmo ritmo, 20 ms por vez.
        var chunk = Silence(TimeSpan.FromMilliseconds(20));
        var read = new byte[chunk.Length];
        for (int i = 0; i < 150; i++)
        {
            source.AddSamples(chunk, 0, chunk.Length);
            trimmer.Read(read, 0, read.Length);
        }

        Assert.InRange(source.BufferedBytes, 0, BytesFor(target) + chunk.Length);
    }

    /// <summary>
    /// Rede com rajadas: a fila sobe e desce, mas chega perto de zero a cada ciclo. Isso não é
    /// atraso parado, e cortar aqui só criaria buracos no som.
    /// </summary>
    [Fact]
    public void JitterThatDrainsTheQueueIsNeverCut()
    {
        var source = NewSource();
        var trimmer = new LatencyTrimmingProvider(source,
            TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(80));

        var burst = Silence(TimeSpan.FromMilliseconds(200));
        var read = new byte[BytesFor(TimeSpan.FromMilliseconds(20))];
        long expected = 0;

        for (int i = 0; i < 200; i++)
        {
            if (i % 10 == 0)
            {
                source.AddSamples(burst, 0, burst.Length);
                expected += burst.Length;
            }

            trimmer.Read(read, 0, read.Length);
            expected = Math.Max(0, expected - read.Length);

            Assert.Equal(expected, source.BufferedBytes);
        }
    }

    [Fact]
    public void ExposesTheSourceFormat()
    {
        var source = NewSource();
        var trimmer = new LatencyTrimmingProvider(source,
            TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(80));

        Assert.Equal(Format.SampleRate, trimmer.WaveFormat.SampleRate);
        Assert.Equal(Format.Channels, trimmer.WaveFormat.Channels);
    }
}
