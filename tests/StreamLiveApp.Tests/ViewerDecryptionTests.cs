using System.Text;
using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// O viewer usava o dado bruto quando algo não decifrava com a chave da sala, o que anulava a
/// autenticação do AES-GCM: alguém no meio do caminho dentro da VPN podia injetar sinalização
/// ou áudio falso. Com a chave ativa, só passa o que decifra — mais o controle que o host
/// manda em claro de propósito.
/// </summary>
public class ViewerDecryptionTests
{
    private static readonly byte[] Key = CryptoHelper.DeriveKey("sala-secreta");

    private static string Json(string type, string? data = null)
        => SignalingMessage.Serialize(new SignalingMessage { Type = type, Data = data });

    [Fact]
    public void WithoutKeyEverythingPassesAsIs()
    {
        Assert.Equal("STREAM_STARTED", SignalingClient.ResolveIncomingText("STREAM_STARTED", null));
        Assert.Equal(Json("offer", "sdp"), SignalingClient.ResolveIncomingText(Json("offer", "sdp"), null));
    }

    [Fact]
    public void WithKeyEncryptedMessageIsDecrypted()
    {
        var cipher = CryptoHelper.EncryptText(Json("offer", "sdp"), Key);
        Assert.Equal(Json("offer", "sdp"), SignalingClient.ResolveIncomingText(cipher, Key));
    }

    [Theory]
    [InlineData("AUTH_REQUIRED")]
    [InlineData("AUTH_OK")]
    [InlineData("AUTH_FAIL")]
    [InlineData("AUTH_LOCKED")]
    [InlineData("STATUS_RESPONSE")]
    [InlineData("PONG")]
    public void WithKeyClearControlMessagesStillPass(string type)
    {
        var plain = Json(type, "x");
        Assert.Equal(plain, SignalingClient.ResolveIncomingText(plain, Key));
    }

    [Fact]
    public void WithKeyAnythingElseThatDoesNotDecryptIsDropped()
    {
        Assert.Null(SignalingClient.ResolveIncomingText(Json("offer", "sdp-injetado"), Key));
        Assert.Null(SignalingClient.ResolveIncomingText("STREAM_STOPPED", Key));
        Assert.Null(SignalingClient.ResolveIncomingText(CryptoHelper.EncryptText(Json("offer"), CryptoHelper.DeriveKey("outra")), Key));
        Assert.Null(SignalingClient.ResolveIncomingText("lixo", Key));
    }

    [Fact]
    public void BinaryWithKeyMustDecrypt()
    {
        var audio = Encoding.ASCII.GetBytes("pcm");
        Assert.Equal(audio, SignalingClient.ResolveIncomingBinary(CryptoHelper.EncryptBytes(audio, Key), Key));
        Assert.Null(SignalingClient.ResolveIncomingBinary(audio, Key));
        Assert.Equal(audio, SignalingClient.ResolveIncomingBinary(audio, null));
    }
}
