using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace StreamLiveApp.Services
{
    /// <summary>
    /// Diz se a Radmin VPN está aberta e sabe abri-la. Sem ela nenhum amigo é alcançável:
    /// os IPs da lista são os 26.x que a VPN distribui, então o app abriria com todo mundo
    /// offline e sem nenhuma pista do motivo.
    /// </summary>
    public static class VpnStatusService
    {
        /// <summary>
        /// A janela do Radmin. O serviço (RvControlSvc) NÃO serve como sinal: ele sobe com o
        /// Windows e continua rodando com o Radmin fechado, então checá-lo diria "aberto"
        /// sempre.
        /// </summary>
        private const string GuiProcessName = "RvRvpnGui";

        private const string ExecutableRelativePath = @"Radmin VPN\RvRvpnGui.exe";

        /// <summary>
        /// Em caso de erro devolve true: um aviso errado atrapalha mais do que a ausência
        /// dele, e a checagem é só de conveniência.
        /// </summary>
        public static bool IsRunning()
        {
            try
            {
                return Process.GetProcessesByName(GuiProcessName).Length > 0;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>Caminho do executável do Radmin, ou null se ele não está instalado.</summary>
        public static string? FindExecutable()
        {
            try
            {
                var candidates = new[]
                {
                    // O Radmin é 32 bits, então em quase toda máquina cai no primeiro.
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), ExecutableRelativePath),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), ExecutableRelativePath)
                };

                return PickExistingPath(candidates, File.Exists);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Primeiro candidato que existe. Puro, para poder ser testado sem tocar no disco.</summary>
        internal static string? PickExistingPath(IEnumerable<string?> candidates, Func<string, bool> exists)
        {
            foreach (var candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate)) continue;
                if (exists(candidate)) return candidate;
            }

            return null;
        }

        /// <summary>Abre o Radmin VPN. False quando ele não está instalado ou não subiu.</summary>
        public static bool TryStart()
        {
            var path = FindExecutable();
            if (path == null) return false;

            try
            {
                // UseShellExecute porque o Radmin pede elevação: sem o shell o Start falha.
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
