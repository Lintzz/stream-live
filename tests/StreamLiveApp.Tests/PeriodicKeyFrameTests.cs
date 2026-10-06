using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// O keyframe periódico é só a rede de segurança: perda de pacote já pede keyframe na hora
/// (REQUEST_KEYFRAME, pelo WebSocket, que não perde), e quem entra ganha a rajada. Cada keyframe
/// de 1080p é uma rajada de ~150 KB de UDP — a causa dos "quadradinhos" em quem assiste —, e
/// a cada 2 s eles eram metade das rajadas de uma live parada.
/// </summary>
public class PeriodicKeyFrameTests
{
    private static readonly TimeSpan NoBurst = TimeSpan.MinValue;

    [Fact]
    public void NoPeriodicKeyFrameThreeSecondsAfterTheLast()
    {
        var last = TimeSpan.FromSeconds(10);
        Assert.False(StreamManager.ShouldForcePeriodicKeyFrame(last + TimeSpan.FromSeconds(3), last, NoBurst));
    }

    [Fact]
    public void PeriodicKeyFrameStillComesAsASafetyNet()
    {
        var last = TimeSpan.FromSeconds(10);
        Assert.True(StreamManager.ShouldForcePeriodicKeyFrame(last + TimeSpan.FromSeconds(5), last, NoBurst));
    }

    [Fact]
    public void BurstWindowAfterSomeoneJoinsKeepsFrequentKeyFrames()
    {
        var last = TimeSpan.FromSeconds(10);
        var burstUntil = TimeSpan.FromSeconds(14);
        Assert.True(StreamManager.ShouldForcePeriodicKeyFrame(last + TimeSpan.FromMilliseconds(400), last, burstUntil));
        Assert.False(StreamManager.ShouldForcePeriodicKeyFrame(last + TimeSpan.FromMilliseconds(200), last, burstUntil));
    }
}
