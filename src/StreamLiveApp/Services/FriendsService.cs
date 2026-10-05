using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using StreamLiveApp.Models;

namespace StreamLiveApp.Services
{
    public static class FriendsService
    {
        /// <summary>
        /// Disparado quando ler ou gravar a lista falha. Antes as exceções eram engolidas em
        /// silêncio: um erro de gravação fazia o usuário perder os amigos sem nunca saber.
        /// </summary>
        public static event Action<string>? OnPersistenceError;

        private static string GetFilePath() => AppPaths.GetFilePath("friends.json");

        /// <summary>
        /// IPv4 em quatro partes de 0 a 255, sem zero à esquerda nem espaço. Estrito de
        /// propósito: o IPAddress.TryParse aceita "26.10" (lido como 26.0.0.10) e "26.10.0.05",
        /// e um IP salvo assim faz o amigo aparecer sempre offline sem pista do motivo.
        /// Quem chama apara os espaços antes.
        /// </summary>
        internal static bool IsValidFriendIp(string? ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;

            var parts = ip.Split('.');
            if (parts.Length != 4) return false;

            foreach (var part in parts)
            {
                if (part.Length is 0 or > 3) return false;
                if (part.Length > 1 && part[0] == '0') return false;
                foreach (var c in part)
                    if (c is < '0' or > '9') return false;
                if (int.Parse(part) > 255) return false;
            }
            return true;
        }

        public static List<Friend> LoadFriends()
        {
            try
            {
                var file = GetFilePath();
                if (File.Exists(file))
                {
                    var json = File.ReadAllText(file);
                    return JsonSerializer.Deserialize<List<Friend>>(json) ?? new List<Friend>();
                }
            }
            catch (Exception ex)
            {
                OnPersistenceError?.Invoke($"Não foi possível carregar a lista de amigos: {ex.Message}");
            }
            return new List<Friend>();
        }

        /// <summary>
        /// Grava num arquivo temporário e só então substitui o definitivo: uma falha no meio
        /// da escrita não deixa o friends.json truncado.
        /// </summary>
        public static bool SaveFriends(List<Friend> friends)
        {
            // No modo demonstração a lista é fictícia: gravar apagaria a lista de verdade.
            if (DemoMode.IsEnabled) return true;

            var file = GetFilePath();
            var temp = file + ".tmp";

            try
            {
                var json = JsonSerializer.Serialize(friends, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(temp, json);
                File.Move(temp, file, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
                OnPersistenceError?.Invoke($"Não foi possível salvar a lista de amigos: {ex.Message}");
                return false;
            }
        }
    }
}
