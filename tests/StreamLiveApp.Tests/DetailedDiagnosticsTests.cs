using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// Diagnóstico detalhado da live: existe só na build de diagnóstico do dono (-p:DiagDetalhado=true).
/// A build normal — a dos testes e a do instalador publicado — não pode trazê-lo ligado.
/// </summary>
public class DetailedDiagnosticsTests
{
    [Fact]
    public void PublicBuildDoesNotCompileItIn()
    {
        // Se isto falhar, alguém deixou o DIAG_DETALHADO fixo no csproj: o instalador
        // publicado passaria a gravar o log segundo a segundo na máquina de todo mundo.
        Assert.False(DetailedDiagnostics.IsCompiledIn);
    }

    [Theory]
    [InlineData("26.68.233.171", "Radmin VPN")]
    [InlineData("fdfd::1a44:e9ab", "Radmin VPN")]
    [InlineData("[fdfd::1a44:e9ab]", "Radmin VPN")]
    [InlineData("2001:0:14c9:cd04:18b3:3dc6:44a0:5bc5", "Teredo (túnel IPv6 do Windows)")]
    [InlineData("2804:a4:8e:5cc:bb6a:a8b7:7897:30a1", "IPv6 público (internet)")]
    [InlineData("192.168.1.64", "rede local")]
    [InlineData("10.0.0.5", "rede local")]
    [InlineData("172.20.1.1", "rede local")]
    [InlineData("172.40.1.1", "IPv4 público (internet)")]
    [InlineData("127.0.0.1", "esta máquina")]
    [InlineData(null, "desconhecido")]
    public void ClassifiesTheNetworkPath(string? address, string expected)
    {
        Assert.Equal(expected, DetailedDiagnostics.ClassifyAddress(address));
    }

    [Fact]
    public void FreezeWaitingForKeyFrameSaysSo()
    {
        var text = DetailedDiagnostics.DescribeFreeze(heldForKeyFrame: true, maxPacketGapMs: 40, lostPackets: 12);

        Assert.Contains("esperando keyframe", text);
        Assert.Contains("12 pacotes perdidos", text);
        Assert.DoesNotContain("rede parada", text);
    }

    [Fact]
    public void FreezeWithNoPacketsArrivingIsTheNetwork()
    {
        var text = DetailedDiagnostics.DescribeFreeze(heldForKeyFrame: false, maxPacketGapMs: 900, lostPackets: 0);

        Assert.Contains("rede parada: 900 ms", text);
    }

    [Fact]
    public void FreezeWithPacketsFlowingPointsAwayFromTheNetwork()
    {
        var text = DetailedDiagnostics.DescribeFreeze(heldForKeyFrame: false, maxPacketGapMs: 30, lostPackets: 0);

        Assert.Contains("pacotes chegando normalmente", text);
    }
}
