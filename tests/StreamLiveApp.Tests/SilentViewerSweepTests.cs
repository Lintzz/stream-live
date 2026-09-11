using System;
using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// A varredura de viewers calados chamava <c>Close()</c> e confiava no <c>OnClose</c> do Fleck
/// para limpar as listas. Com o TCP meio-aberto esse evento nunca dispara, e nos logs de campo
/// o mesmo viewer reaparecia a cada 5s — "calado ha 342s", cinco cópias, para sempre — inflando
/// a contagem de peers que decide o aviso de saúde da live. Daí a segunda passagem.
/// </summary>
public class SilentViewerSweepTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    [Fact]
    public void ViewerFalanteFicaEmPaz()
    {
        var acao = SignalingServer.DecideSilentViewerAction(
            TimeSpan.FromSeconds(3), Timeout, closeAlreadyRequested: false);

        Assert.Equal(SignalingServer.SilentViewerAction.Manter, acao);
    }

    [Fact]
    public void SilencioNoLimiteAindaNaoDerruba()
    {
        var acao = SignalingServer.DecideSilentViewerAction(Timeout, Timeout, closeAlreadyRequested: false);

        Assert.Equal(SignalingServer.SilentViewerAction.Manter, acao);
    }

    [Fact]
    public void PrimeiroSilencioLongoPedeFechamentoEducado()
    {
        var acao = SignalingServer.DecideSilentViewerAction(
            TimeSpan.FromSeconds(25), Timeout, closeAlreadyRequested: false);

        Assert.Equal(SignalingServer.SilentViewerAction.Fechar, acao);
    }

    [Fact]
    public void QuemJaLevouCloseESeguiuCaladoEExpulso()
    {
        // É este o caso que vazava: o Close não pegou, e sem expulsar a conexão voltava na
        // varredura seguinte com o mesmo aviso, indefinidamente.
        var acao = SignalingServer.DecideSilentViewerAction(
            TimeSpan.FromSeconds(342), Timeout, closeAlreadyRequested: true);

        Assert.Equal(SignalingServer.SilentViewerAction.Expulsar, acao);
    }

    [Fact]
    public void CloseAnteriorNaoDerrubaQuemVoltouAFalar()
    {
        var acao = SignalingServer.DecideSilentViewerAction(
            TimeSpan.FromSeconds(1), Timeout, closeAlreadyRequested: true);

        Assert.Equal(SignalingServer.SilentViewerAction.Manter, acao);
    }
}
