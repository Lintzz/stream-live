using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace StreamLiveApp
{
    /// <summary>
    /// Criptografia da sala. Duas mudanças em relação à primeira versão:
    /// a chave sai de um PBKDF2 (e não de um SHA256 cru, que era força-bruta trivial),
    /// e o payload é AES-GCM — autenticado, então mensagem adulterada é rejeitada
    /// em vez de "descriptografar" em lixo.
    /// </summary>
    public static class CryptoHelper
    {
        // Protocolo de sala v2. A v1 (até a 1.0.38) usava um salt fixo do app para todas as
        // salas e a mesma chave no HMAC do login e no AES: um dicionário pré-calculado uma
        // vez servia contra qualquer sala, e quem capturasse um login testava senhas a custo
        // de um HMAC. Agora o host sorteia um salt por sala (vai junto do desafio) e a
        // chave-mestra se divide em duas, uma para cada uso. Muda o formato do AUTH — os dois
        // lados precisam ser v2; ver FormatChallenge/TryParseProof.
        private const string ProtocolTag = "v2";
        private const int Pbkdf2Iterations = 200_000;
        private const int SaltSize = 16;

        private const int NonceSize = 12;  // AES-GCM padrão
        private const int TagSize = 16;

        private static readonly byte[] AuthInfo = Encoding.UTF8.GetBytes("StreamLive room auth v2");
        private static readonly byte[] EncInfo = Encoding.UTF8.GetBytes("StreamLive room enc v2");

        // PBKDF2 a 200k iterações custa ~100ms; o áudio cifra ~50x/s.
        // Sem cache o app trava, então as chaves ficam guardadas por senha + salt.
        private static readonly ConcurrentDictionary<string, RoomKeys> KeyCache = new();

        /// <summary>Chaves de uma sala: uma só prova a senha, a outra só cifra.</summary>
        public sealed record RoomKeys(byte[] Auth, byte[] Enc);

        /// <summary>Salt novo para uma sala, em base64 (é assim que ele viaja no desafio).</summary>
        public static string NewSalt()
        {
            var salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);
            return Convert.ToBase64String(salt);
        }

        public static RoomKeys DeriveRoomKeys(string password, string saltB64)
        {
            return KeyCache.GetOrAdd((password ?? string.Empty) + "\n" + saltB64, _ =>
            {
                var master = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(password ?? string.Empty),
                    Convert.FromBase64String(saltB64),
                    Pbkdf2Iterations,
                    HashAlgorithmName.SHA256,
                    32);
                return new RoomKeys(
                    HKDF.Expand(HashAlgorithmName.SHA256, master, 32, AuthInfo),
                    HKDF.Expand(HashAlgorithmName.SHA256, master, 32, EncInfo));
            });
        }

        /// <summary>Data do AUTH_REQUIRED/AUTH_FAIL: "v2:&lt;salt&gt;:&lt;desafio&gt;".</summary>
        public static string FormatChallenge(string saltB64, string challenge)
            => $"{ProtocolTag}:{saltB64}:{challenge}";

        /// <summary>
        /// Lê o desafio do host. False quando o formato não é v2 — o host está numa versão
        /// antiga, que manda só o desafio, sem salt.
        /// </summary>
        public static bool TryParseChallenge(string? data, out string saltB64, out string challenge)
        {
            saltB64 = challenge = string.Empty;
            var parts = data?.Split(':');
            if (parts is not { Length: 3 } || parts[0] != ProtocolTag) return false;
            if (parts[1].Length == 0 || parts[2].Length == 0) return false;
            try { Convert.FromBase64String(parts[1]); } catch (FormatException) { return false; }
            saltB64 = parts[1];
            challenge = parts[2];
            return true;
        }

        /// <summary>Data do AUTH: "v2:&lt;prova&gt;".</summary>
        public static string FormatProof(string proof) => $"{ProtocolTag}:{proof}";

        /// <summary>False quando a prova não é v2 — o viewer está numa versão antiga.</summary>
        public static bool TryParseProof(string? data, out string proof)
        {
            proof = string.Empty;
            var prefix = ProtocolTag + ":";
            if (data == null || !data.StartsWith(prefix, StringComparison.Ordinal) || data.Length == prefix.Length)
                return false;
            proof = data.Substring(prefix.Length);
            return true;
        }

        public static string EncryptText(string plainText, byte[] key)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = EncryptBytes(plainBytes, key);
            return Convert.ToBase64String(cipherBytes);
        }

        public static string? TryDecryptText(string cipherTextB64, byte[] key)
        {
            try
            {
                var cipherBytes = Convert.FromBase64String(cipherTextB64);
                var plainBytes = TryDecryptBytes(cipherBytes, key);
                if (plainBytes == null) return null;
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Formato: [nonce 12][tag 16][ciphertext].</summary>
        public static byte[] EncryptBytes(byte[] plain, byte[] key)
        {
            var result = new byte[NonceSize + TagSize + plain.Length];
            var nonce = result.AsSpan(0, NonceSize);
            var tag = result.AsSpan(NonceSize, TagSize);
            var cipher = result.AsSpan(NonceSize + TagSize);

            RandomNumberGenerator.Fill(nonce);
            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plain, cipher, tag);
            return result;
        }

        /// <summary>Devolve null quando a tag não confere — payload adulterado ou chave errada.</summary>
        public static byte[]? TryDecryptBytes(byte[] cipher, byte[] key)
        {
            try
            {
                if (cipher == null || cipher.Length < NonceSize + TagSize) return null;

                var nonce = cipher.AsSpan(0, NonceSize);
                var tag = cipher.AsSpan(NonceSize, TagSize);
                var payload = cipher.AsSpan(NonceSize + TagSize);

                var plain = new byte[payload.Length];
                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(nonce, payload, tag, plain);
                return plain;
            }
            catch
            {
                return null;
            }
        }

        // ───────────────────────── Autenticação por desafio ─────────────────────────

        /// <summary>Desafio aleatório que o host manda junto do AUTH_REQUIRED.</summary>
        public static string NewChallenge()
        {
            var nonce = new byte[32];
            RandomNumberGenerator.Fill(nonce);
            return Convert.ToBase64String(nonce);
        }

        /// <summary>
        /// Prova de que o viewer conhece a senha, sem mandar a senha no fio: HMAC do desafio
        /// com a chave de autenticação da sala (<see cref="RoomKeys.Auth"/>). Antes o AUTH carregava a senha em texto claro sobre ws://,
        /// então qualquer um na VPN a lia.
        /// </summary>
        public static string ComputeAuthProof(byte[] key, string challenge)
        {
            using var hmac = new HMACSHA256(key);
            var mac = hmac.ComputeHash(Encoding.UTF8.GetBytes(challenge ?? string.Empty));
            return Convert.ToBase64String(mac);
        }

        /// <summary>Comparação em tempo constante: evita distinguir senhas pelo tempo de resposta.</summary>
        public static bool FixedTimeEquals(string? a, string? b)
        {
            if (a == null || b == null) return false;

            var bytesA = Encoding.UTF8.GetBytes(a);
            var bytesB = Encoding.UTF8.GetBytes(b);

            // FixedTimeEquals exige tamanhos iguais; normalizamos por hash para não
            // vazar o comprimento nem cair no atalho de tamanho diferente.
            Span<byte> hashA = stackalloc byte[32];
            Span<byte> hashB = stackalloc byte[32];
            SHA256.HashData(bytesA, hashA);
            SHA256.HashData(bytesB, hashB);

            return CryptographicOperations.FixedTimeEquals(hashA, hashB);
        }
    }
}
