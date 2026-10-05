using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace StreamLiveApp
{
    /// <summary>Rede do Radmin lembrada pelo "Lembrar rede".</summary>
    public sealed record RadminCredential(string NetworkName, string Password);

    /// <summary>
    /// Guarda a rede do Radmin cifrada com DPAPI (escopo do usuário do Windows): só a mesma
    /// conta, na mesma máquina, consegue abrir. Arquivo próprio, e não um campo no
    /// settings.json, por dois motivos: o settings.json é legível e vai inteiro para quem
    /// pedir ajuda com ele aberto, e "esquecer a rede" vira apagar um arquivo, sem sobrar
    /// nada da senha em lugar nenhum.
    ///
    /// Fica fora do relatório de diagnóstico de propósito (a lista do DiagnosticLog é
    /// fechada) — mesmo cifrado, o blob não tem por que sair da máquina.
    /// </summary>
    public sealed class RadminCredentialStore
    {
        private const string FileName = "radmin_rede.dat";

        // Entropia adicional do DPAPI. Não é segredo (está no código aberto): só impede que
        // outro programa do mesmo usuário abra o blob chamando o DPAPI sem saber deste valor
        // — o que ele ainda conseguiria lendo este arquivo-fonte, mas não por acidente.
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("StreamLive.RadminCredential.v1");

        private readonly string _filePath;

        public RadminCredentialStore() : this(AppPaths.GetFilePath(FileName)) { }

        /// <summary>Caminho injetável para os testes não tocarem na pasta real do app.</summary>
        internal RadminCredentialStore(string filePath)
        {
            _filePath = filePath;
        }

        public bool HasSaved => File.Exists(_filePath);

        /// <summary>A rede salva, ou null se não há nada salvo ou o arquivo não abre mais.</summary>
        public RadminCredential? Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return null;

                var plain = ProtectedData.Unprotect(File.ReadAllBytes(_filePath), Entropy, DataProtectionScope.CurrentUser);
                var credential = JsonSerializer.Deserialize<RadminCredential>(plain);
                return credential is { NetworkName.Length: > 0 } ? credential : null;
            }
            catch (Exception ex)
            {
                // Perfil do Windows recriado ou arquivo copiado de outra máquina: o DPAPI não
                // abre mais. Não é erro do usuário — ele só digita a rede de novo.
                DiagnosticLog.Warn("Radmin", $"Rede salva ilegível, ignorada ({ex.GetType().Name})");
                return null;
            }
        }

        /// <summary>Grava em .tmp e move por cima, como o SettingsService.</summary>
        public bool Save(RadminCredential credential)
        {
            ArgumentNullException.ThrowIfNull(credential);
            if (DemoMode.IsEnabled) return true;

            var temp = _filePath + ".tmp";
            try
            {
                var plain = JsonSerializer.SerializeToUtf8Bytes(credential);
                var cipher = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
                CryptographicOperations.ZeroMemory(plain);

                File.WriteAllBytes(temp, cipher);
                File.Move(temp, _filePath, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
                DiagnosticLog.Error("Radmin", "Não foi possível salvar a rede", ex);
                return false;
            }
        }

        public void Forget()
        {
            if (DemoMode.IsEnabled) return;
            try { if (File.Exists(_filePath)) File.Delete(_filePath); }
            catch (Exception ex) { DiagnosticLog.Error("Radmin", "Não foi possível apagar a rede salva", ex); }
        }
    }
}
