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
                    // Pasta escolhida no instalador; cobre quem instalou fora do Program Files.
                    ReadInstallLocation() is string folder ? Path.Combine(folder, Path.GetFileName(ExecutableRelativePath)) : null,
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

        /// <summary>
        /// InstallLocation da entrada de desinstalação do Radmin VPN (só leitura). O nome da
        /// chave é um GUID que muda entre versões, então a busca é pelo DisplayName.
        /// </summary>
        private static string? ReadInstallLocation()
        {
            foreach (var root in new[]
            {
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
            })
            {
                using var uninstall = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(root);
                if (uninstall == null) continue;

                foreach (var name in uninstall.GetSubKeyNames())
                {
                    using var entry = uninstall.OpenSubKey(name);
                    if (entry?.GetValue("DisplayName") is string display
                        && display.StartsWith("Radmin VPN", StringComparison.OrdinalIgnoreCase)
                        && entry.GetValue("InstallLocation") is string location
                        && !string.IsNullOrWhiteSpace(location))
                    {
                        return location;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// A janela do Radmin precisa estar rodando — testado em 2026-10-05: com ela fechada, o
        /// serviço e o adaptador continuam de pé, mas os amigos aparecem offline. Ela só não
        /// precisa ser vista: /minimized (o argumento do início automático do Windows) sobe
        /// direto na bandeja. UseShellExecute porque versões antigas do Radmin pediam
        /// elevação, e sem o shell o Start falha nelas; o 2.1 roda sem admin.
        /// </summary>
        internal static ProcessStartInfo BuildStartInfo(string executablePath)
            => new(executablePath, "/minimized") { UseShellExecute = true };

        /// <summary>
        /// Abre o Radmin VPN direto na bandeja, sem janela na tela. False quando ele não está
        /// instalado ou não subiu.
        /// </summary>
        public static bool TryStart()
        {
            var path = FindExecutable();
            if (path == null) return false;

            try
            {
                Process.Start(BuildStartInfo(path))?.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
