using System;
using System.IO;
using System.Text.Json;

namespace StreamLiveApp.Services
{
    /// <summary>Preferências do app que precisam sobreviver ao fechamento da janela.</summary>
    public sealed class AppSettings
    {
        // Já moraram aqui a exclusão de áudio por nome de processo, o modo leve e o GDI
        // forçado. Viraram comportamento fixo (Discord sempre excluído, modo leve sempre
        // ligado, GDI só pelo fallback automático) porque nenhuma delas era usada como
        // escolha — só um dos valores rodava, e o outro caminho era código morto.
        // Chaves antigas continuam no settings.json de quem já usava o app; a
        // desserialização simplesmente as ignora.

        /// <summary>Só IPs da lista de amigos conseguem conectar.</summary>
        public bool RestrictToFriends { get; set; } = true;
    }

    /// <summary>Lê e grava as preferências em <c>settings.json</c>, ao lado do <c>friends.json</c>.</summary>
    public static class SettingsService
    {
        /// <summary>Disparado quando ler ou gravar falha, no mesmo espírito do FriendsService.</summary>
        public static event Action<string>? OnPersistenceError;

        private static string GetFilePath() => AppPaths.GetFilePath("settings.json");

        public static AppSettings Load()
        {
            try
            {
                var file = GetFilePath();
                if (File.Exists(file))
                {
                    var json = File.ReadAllText(file);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                OnPersistenceError?.Invoke($"Não foi possível carregar as configurações: {ex.Message}");
            }

            // Sem arquivo (primeira execução) valem as preferências de fábrica.
            return new AppSettings();
        }

        /// <summary>
        /// Grava num arquivo temporário e só então substitui o definitivo: uma falha no meio
        /// da escrita não deixa o settings.json truncado.
        /// </summary>
        public static bool Save(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var file = GetFilePath();
            var temp = file + ".tmp";

            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(temp, json);
                File.Move(temp, file, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
                OnPersistenceError?.Invoke($"Não foi possível salvar as configurações: {ex.Message}");
                return false;
            }
        }
    }
}
