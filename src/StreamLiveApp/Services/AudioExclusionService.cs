using System;
using System.Diagnostics;
using System.Linq;

namespace StreamLiveApp.Services
{
    /// <summary>
    /// Resolve para um PID o programa cujo áudio fica fora da transmissão. O alvo é guardado
    /// pelo NOME do processo, não pelo PID: entre uma transmissão e outra o programa pode ter
    /// sido reaberto com outro identificador.
    /// </summary>
    public static class AudioExclusionService
    {
        /// <summary>
        /// Traduz o nome do programa para um PID. Devolve 0 (capturar tudo) quando ele não
        /// está rodando — é esse 0 que faz o app cair para o loopback do sistema inteiro.
        /// </summary>
        public static uint ResolvePid(string? processName)
        {
            if (string.IsNullOrEmpty(processName)) return 0;

            try
            {
                var processes = Process.GetProcessesByName(processName);
                var withWindow = processes.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
                var target = withWindow ?? processes.FirstOrDefault();
                return target == null ? 0u : (uint)target.Id;
            }
            catch
            {
                return 0;
            }
        }
    }
}
