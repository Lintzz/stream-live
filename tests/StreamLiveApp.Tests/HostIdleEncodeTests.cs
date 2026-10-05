using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// O host não codifica vídeo enquanto ninguém está conectado (medido: o encode custava mais de
/// um núcleo numa live sem público). Quem entra recebe uma rajada de keyframes ao conectar, então
/// nada se perde. O aviso de saúde precisa olhar a captura, não o encode — senão toda live sem
/// público acusaria "sua tela não está sendo capturada".
/// </summary>
public class HostIdleEncodeTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    public void EncodesOnlyWithSomeoneConnected(int connectedPeers, bool expected)
        => Assert.Equal(expected, StreamManager.ShouldEncode(connectedPeers));

    [Fact]
    public void NoAudienceWithCaptureRunningIsHealthy()
        => Assert.Null(StreamManager.DecideHealthWarning(conectados: 0, total: 0, capturedFps: 59, audio: 0));

    [Fact]
    public void CaptureStoppedWarnsEvenWithoutAudience()
        => Assert.Contains("não está sendo capturada", StreamManager.DecideHealthWarning(0, 0, capturedFps: 0, audio: 0));

    [Fact]
    public void ViewerWhoseVideoNeverConnectedWarns()
        => Assert.Contains("não fechou", StreamManager.DecideHealthWarning(conectados: 0, total: 1, capturedFps: 59, audio: 50));

    [Fact]
    public void ConnectedViewerWithoutAudioWarns()
        => Assert.Contains("áudio", StreamManager.DecideHealthWarning(conectados: 1, total: 1, capturedFps: 59, audio: 0));
}
