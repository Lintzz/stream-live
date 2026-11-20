using System;
using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// A exclusão do Discord só valia a partir do build 20348, o número do SDK que a Microsoft
/// cita no exemplo. O Windows 10 de usuário para no 19045, então todo host com Windows 10 caía
/// direto no loopback do sistema inteiro sem nem tentar, e o Discord vazava na live dele. A
/// captura por processo existe desde o Windows 10 2004 (19041), a mesma linha de corte da
/// captura de aplicativo do OBS. Se o Windows recusar, o fallback do AudioCapturer continua lá.
/// </summary>
public class ProcessAudioSupportTests
{
    [Theory]
    [InlineData(19041, true)]   // Windows 10 2004
    [InlineData(19045, true)]   // Windows 10 22H2, o último
    [InlineData(20348, true)]   // Windows Server 2022
    [InlineData(22631, true)]   // Windows 11 23H2
    [InlineData(26100, true)]   // Windows 11 24H2
    [InlineData(18363, false)]  // Windows 10 1909: a API não existe
    [InlineData(17763, false)]  // Windows 10 1809
    public void SupportFollowsTheWindowsBuild(int build, bool expected)
    {
        Assert.Equal(expected, ProcessAudioCapturer.IsSupported(new Version(10, 0, build)));
    }

    [Fact]
    public void Windows81IsNotSupported()
    {
        Assert.False(ProcessAudioCapturer.IsSupported(new Version(6, 3, 9600)));
    }
}
