using System.Diagnostics;
using System.IO;
using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// Abre o StreamLiveApp.exe compilado de verdade e confere que a janela principal aparece e
/// continua viva. Os outros testes exercitam a lógica por dentro; este pega o que só aparece
/// no executável: XAML ou recurso que estoura na inicialização, assembly que falta na saída,
/// exceção no startup. Não cobre o FFmpeg: ele só é carregado quando uma live começa (o teste
/// passa até sem as DLLs) — quem trava o FFmpeg real é o VideoEncoderFormatTests.
/// Roda em --demo: sem servidor na 8080 (não briga com uma instância real aberta, que pode
/// estar transmitindo para amigos) e sem gravar friends.json/settings.json.
/// </summary>
public class AppSmokeTests
{
    // O WPF não tem MainWindowHandle no instante do Start; numa máquina fria, com o FFmpeg
    // saindo do disco pela primeira vez, a janela já levou mais de 5 s para aparecer.
    private static readonly TimeSpan WindowTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StayAlive = TimeSpan.FromSeconds(3);

    [Fact]
    public void CompiledAppOpensMainWindowInDemoMode()
    {
        // O ProjectReference copia o .exe do app para a saída dos testes.
        string exe = Path.Combine(AppContext.BaseDirectory, "StreamLiveApp.exe");
        Assert.True(File.Exists(exe), $"executável não encontrado em {exe}");

        string errorLog = AppPaths.GetFilePath("error.log");
        long logBefore = File.Exists(errorLog) ? new FileInfo(errorLog).Length : 0;

        using var process = Process.Start(new ProcessStartInfo(exe, "--demo") { UseShellExecute = false })!;
        try
        {
            var deadline = DateTime.UtcNow + WindowTimeout;
            while (DateTime.UtcNow < deadline && !process.HasExited)
            {
                process.Refresh();
                if (process.MainWindowHandle != IntPtr.Zero && process.MainWindowTitle.Contains("Stream Live"))
                    break;
                Thread.Sleep(200);
            }

            Assert.False(process.HasExited, $"o app fechou na inicialização (código {SafeExitCode(process)}){NewLogText(errorLog, logBefore)}");
            Assert.True(process.MainWindowHandle != IntPtr.Zero,
                $"a janela principal não apareceu em {WindowTimeout.TotalSeconds:0} s{NewLogText(errorLog, logBefore)}");

            Thread.Sleep(StayAlive);
            Assert.False(process.HasExited, $"o app morreu logo depois de abrir{NewLogText(errorLog, logBefore)}");

            string newLog = NewLogText(errorLog, logBefore);
            Assert.True(newLog.Length == 0, $"a inicialização escreveu no error.log:{newLog}");
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
    }

    private static string SafeExitCode(Process p)
    {
        try { return p.ExitCode.ToString(); } catch { return "?"; }
    }

    /// <summary>O que o app acrescentou ao error.log durante o teste, para a falha já dizer o porquê.</summary>
    private static string NewLogText(string path, long from)
    {
        try
        {
            if (!File.Exists(path)) return "";
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (stream.Length <= from) return "";
            stream.Seek(from, SeekOrigin.Begin);
            return Environment.NewLine + new StreamReader(stream).ReadToEnd().Trim();
        }
        catch (IOException)
        {
            return "";
        }
    }
}
