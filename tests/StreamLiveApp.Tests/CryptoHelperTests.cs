using System.Text;
using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

public class CryptoHelperTests
{
    private static readonly string Salt = CryptoHelper.NewSalt();

    /// <summary>Chave de cifra de uma sala de teste (o salt só muda entre salas).</summary>
    private static byte[] Key(string password) => CryptoHelper.DeriveRoomKeys(password, Salt).Enc;

    [Fact]
    public void TextRoundTripsWithTheSamePassword()
    {
        var key = Key("sala-secreta");

        var cipher = CryptoHelper.EncryptText("oi, mundo", key);

        Assert.NotEqual("oi, mundo", cipher);
        Assert.Equal("oi, mundo", CryptoHelper.TryDecryptText(cipher, key));
    }

    [Fact]
    public void BytesRoundTrip()
    {
        var key = Key("sala-secreta");
        var plain = Encoding.UTF8.GetBytes("pacote de áudio qualquer");

        var decrypted = CryptoHelper.TryDecryptBytes(CryptoHelper.EncryptBytes(plain, key), key);

        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void WrongPasswordDoesNotDecrypt()
    {
        var cipher = CryptoHelper.EncryptText("segredo", Key("certa"));

        Assert.Null(CryptoHelper.TryDecryptText(cipher, Key("errada")));
    }

    /// <summary>
    /// A razão de o payload ser AES-GCM e não AES cru: mensagem adulterada é rejeitada em
    /// vez de "descriptografar" em lixo que o resto do app trataria como dado válido.
    /// </summary>
    [Fact]
    public void TamperedPayloadIsRejected()
    {
        var key = Key("sala-secreta");
        var cipher = CryptoHelper.EncryptBytes(Encoding.UTF8.GetBytes("conteúdo"), key);

        cipher[^1] ^= 0xFF; // um bit trocado no fim do ciphertext

        Assert.Null(CryptoHelper.TryDecryptBytes(cipher, key));
    }

    [Fact]
    public void TruncatedPayloadIsRejectedInsteadOfThrowing()
    {
        var key = Key("sala-secreta");

        // Menor que nonce + tag: vem da rede, então não pode estourar exceção.
        Assert.Null(CryptoHelper.TryDecryptBytes(new byte[4], key));
        Assert.Null(CryptoHelper.TryDecryptBytes(System.Array.Empty<byte>(), key));
        Assert.Null(CryptoHelper.TryDecryptText("isto não é base64 válido!!", key));
    }

    [Fact]
    public void SameNonceIsNeverReused()
    {
        var key = Key("sala-secreta");
        var plain = Encoding.UTF8.GetBytes("mesma mensagem");

        var a = CryptoHelper.EncryptBytes(plain, key);
        var b = CryptoHelper.EncryptBytes(plain, key);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void RoomKeysAreDeterministicAndPasswordSpecific()
    {
        Assert.Equal(Key("abc"), Key("abc"));
        Assert.NotEqual(Key("abc"), Key("abd"));
        Assert.Equal(32, Key("abc").Length);
    }

    /// <summary>
    /// O motivo do protocolo v2: com o salt fixo da v1, um dicionário pré-calculado servia
    /// contra todas as salas do app. Mesma senha em salas diferentes dá chaves diferentes.
    /// </summary>
    [Fact]
    public void SameRoomPasswordWithAnotherSaltGivesOtherKeys()
    {
        var a = CryptoHelper.DeriveRoomKeys("mesma-senha", CryptoHelper.NewSalt());
        var b = CryptoHelper.DeriveRoomKeys("mesma-senha", CryptoHelper.NewSalt());

        Assert.NotEqual(a.Auth, b.Auth);
        Assert.NotEqual(a.Enc, b.Enc);
    }

    /// <summary>A prova do login não pode ser a chave que cifra a live (v1 usava a mesma).</summary>
    [Fact]
    public void AuthAndEncryptionKeysAreSeparate()
    {
        var keys = CryptoHelper.DeriveRoomKeys("sala-secreta", Salt);
        Assert.NotEqual(keys.Auth, keys.Enc);
        Assert.Equal(32, keys.Auth.Length);
    }

    [Fact]
    public void ChallengeRoundTripsInTheV2Format()
    {
        var data = CryptoHelper.FormatChallenge(Salt, "desafio");

        Assert.StartsWith("v2:", data);
        Assert.True(CryptoHelper.TryParseChallenge(data, out var salt, out var challenge));
        Assert.Equal(Salt, salt);
        Assert.Equal("desafio", challenge);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("aGVsbG8gd29ybGQgZGVzYWZpbw==")] // desafio da v1: base64 puro, sem salt
    [InlineData("v1:c2FsdA==:desafio")]
    [InlineData("v2::desafio")]
    [InlineData("v2:c2FsdA==:")]
    [InlineData("v2:não-é-base64:desafio")]
    public void ChallengeOutsideV2IsRecognized(string? data)
    {
        Assert.False(CryptoHelper.TryParseChallenge(data, out _, out _));
    }

    [Fact]
    public void ProofRoundTripsAndV1ProofIsRecognized()
    {
        Assert.True(CryptoHelper.TryParseProof(CryptoHelper.FormatProof("abc"), out var proof));
        Assert.Equal("abc", proof);

        Assert.False(CryptoHelper.TryParseProof("abc", out _));   // prova da v1: HMAC em base64 puro
        Assert.False(CryptoHelper.TryParseProof("v2:", out _));
        Assert.False(CryptoHelper.TryParseProof(null, out _));
    }

    [Fact]
    public void ChallengesAreUnique()
    {
        var seen = new HashSet<string>();
        for (int i = 0; i < 50; i++)
        {
            Assert.True(seen.Add(CryptoHelper.NewChallenge()), "desafio repetido");
        }
    }

    [Fact]
    public void AuthProofDependsOnBothPasswordAndChallenge()
    {
        var right = Key("certa");
        var wrong = Key("errada");
        var challenge = CryptoHelper.NewChallenge();

        var expected = CryptoHelper.ComputeAuthProof(right, challenge);

        Assert.Equal(expected, CryptoHelper.ComputeAuthProof(right, challenge));
        Assert.NotEqual(expected, CryptoHelper.ComputeAuthProof(wrong, challenge));
        Assert.NotEqual(expected, CryptoHelper.ComputeAuthProof(right, CryptoHelper.NewChallenge()));
    }

    [Fact]
    public void FixedTimeEqualsHandlesNullsAndDifferentLengths()
    {
        Assert.True(CryptoHelper.FixedTimeEquals("igual", "igual"));
        Assert.False(CryptoHelper.FixedTimeEquals("igual", "diferente"));
        Assert.False(CryptoHelper.FixedTimeEquals(null, "x"));
        Assert.False(CryptoHelper.FixedTimeEquals("x", null));
        Assert.False(CryptoHelper.FixedTimeEquals(null, null));
    }
}
