using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// 60 ou 30 quadros por segundo, conforme o encoder dá conta (2026-10-06). Numa live jogando
/// Valorant o encoder levava ~24 ms por quadro e pulava 23 de 53 quadros por segundo; como ele
/// seguia achando que eram 60, dividia o teto em 60 fatias e só 30 saíam: a live usava metade
/// do teto (2,5 Mbps) e o movimento virava bloco. Medido no encoder real: declarar 30 com 30
/// quadros saindo dobra os bits por quadro (10 → 20 KB em 1080p).
/// </summary>
public class EncodeRateGovernorTests
{
    // Um segundo de jogo pesado, igual à live medida: 53 capturados, 30 codificados, 23 pulados.
    private static bool Heavy(EncodeRateGovernor g) => g.OnSecond(captured: 53, skippedBusy: 23, encoded: 30, avgEncodeMs: 24);

    // Um segundo folgado a 30: encoder rápido de novo (jogo fechado).
    private static bool Light(EncodeRateGovernor g) => g.OnSecond(captured: 60, skippedBusy: 0, encoded: 30, avgEncodeMs: 9);

    [Fact]
    public void StartsAtSixty()
    {
        Assert.Equal(60, new EncodeRateGovernor().Fps);
    }

    [Fact]
    public void DropsToThirtyAfterSustainedSkipping()
    {
        var g = new EncodeRateGovernor();

        Assert.False(Heavy(g));
        Assert.False(Heavy(g));
        Assert.True(Heavy(g));
        Assert.Equal(30, g.Fps);
    }

    [Fact]
    public void ABriefHiccupDoesNotDrop()
    {
        var g = new EncodeRateGovernor();
        Heavy(g);
        Heavy(g);
        // Um segundo bom no meio zera a conta: troca de fps custa um keyframe, não vale por um soluço.
        g.OnSecond(captured: 58, skippedBusy: 2, encoded: 56, avgEncodeMs: 12);
        Heavy(g);
        Heavy(g);
        Assert.Equal(60, g.Fps);
    }

    [Fact]
    public void DesktopWithFewSkipsStaysAtSixty()
    {
        // Área de trabalho medida no mesmo PC: 56 capturados, 12 pulados (21%), 14 ms por quadro.
        var g = new EncodeRateGovernor();
        for (int i = 0; i < 30; i++) g.OnSecond(captured: 56, skippedBusy: 12, encoded: 44, avgEncodeMs: 14);
        Assert.Equal(60, g.Fps);
    }

    [Fact]
    public void StaticScreenWithFewCapturesNeverCounts()
    {
        var g = new EncodeRateGovernor();
        for (int i = 0; i < 10; i++) g.OnSecond(captured: 4, skippedBusy: 3, encoded: 1, avgEncodeMs: 30);
        Assert.Equal(60, g.Fps);
    }

    [Fact]
    public void TriesSixtyAgainWhenTheEncoderHasRoom()
    {
        var g = new EncodeRateGovernor();
        for (int i = 0; i < 3; i++) Heavy(g);
        Assert.Equal(30, g.Fps);

        bool changed = false;
        for (int i = 0; i < EncodeRateGovernor.RestoreAfterSeconds && !changed; i++) changed = Light(g);
        Assert.True(changed);
        Assert.Equal(60, g.Fps);
    }

    [Fact]
    public void StillHeavyAtThirtyStaysAtThirty()
    {
        var g = new EncodeRateGovernor();
        for (int i = 0; i < 3; i++) Heavy(g);
        for (int i = 0; i < 120; i++) g.OnSecond(captured: 53, skippedBusy: 0, encoded: 30, avgEncodeMs: 24);
        Assert.Equal(30, g.Fps);
    }

    [Fact]
    public void FailedTryWaitsLongerBeforeTheNext()
    {
        var g = new EncodeRateGovernor();
        for (int i = 0; i < 3; i++) Heavy(g);
        for (int i = 0; i < EncodeRateGovernor.RestoreAfterSeconds; i++) Light(g);
        Assert.Equal(60, g.Fps);

        // A tentativa falhou logo (ainda jogando): volta a 30...
        for (int i = 0; i < 3; i++) Heavy(g);
        Assert.Equal(30, g.Fps);

        // ...e a próxima tentativa espera o dobro: cada troca é um keyframe, não pode virar pingue-pongue.
        for (int i = 0; i < EncodeRateGovernor.RestoreAfterSeconds; i++) Light(g);
        Assert.Equal(30, g.Fps);
        for (int i = 0; i < EncodeRateGovernor.RestoreAfterSeconds; i++) Light(g);
        Assert.Equal(60, g.Fps);
    }

    [Theory]
    [InlineData(60, 16.0, true)]   // captura a 60: todo quadro serve
    [InlineData(30, 16.0, false)]  // a 30, pula o do meio...
    [InlineData(30, 33.0, true)]   // ...e pega o seguinte
    [InlineData(30, 30.0, true)]   // folga para a variação da captura
    public void FrameCadenceFollowsTheTarget(int fps, double msSinceLast, bool take)
    {
        Assert.Equal(take, EncodeRateGovernor.ShouldTakeFrame(msSinceLast, fps));
    }
}
