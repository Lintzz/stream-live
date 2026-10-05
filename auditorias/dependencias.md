# Dependências — registro
Data: 2026-10-05
Gerenciador: NuGet (.NET 8) · Comandos: `dotnet list StreamLive.sln package --vulnerable --include-transitive` e `--outdated`
Lockfile: não há packages.lock.json; versões fixadas exatas no csproj

## Números
- Antes: 2 Altas em produção (SIPSorcery 8.0.23, direta) · 0 Críticas · 0 Médias/Baixas
- Depois: 0 vulnerabilidades, diretas e transitivas, nos dois projetos

## Corrigido
- GHSA-28gm-jrmw-xx93 (Alta, DoS por UDP malformado na porta ICE/RTP) e GHSA-jwjp-4649-v8jp (Alta, DoS no SCTP SACK — caminho não usado, o app não cria data channel): SIPSorcery 8.0.23 → 10.0.17 e SIPSorceryMedia.FFmpeg 8.0.7 → 10.0.17
- DLLs do FFmpeg trocadas de 7.0.2 (avcodec-61) para 8.1.2 (avcodec-62), build full shared do gyan.dev (GyanD/codexffmpeg), SHA-256 do zip conferido com o digest da release: 274923c6…d066. Mesmas flags de licença (gpl, version3, libx264). postproc saiu (o FFmpeg 8 não tem mais)
- Transitivas que o SIPSorcery 10 trouxe (IPNetwork2 2.1.2 → NETStandard.Library 1.6.1): System.Net.Http 4.3.0 (GHSA-7jgj-8wvc-jh57) e System.Text.RegularExpressions 4.3.0 (GHSA-cmhx-cq75-c4mj), fixadas em 4.3.4 e 4.3.1 por referência direta no csproj
- SIPSorceryMedia.Encoders removido: só havia um `using` sem uso em StreamManager.cs
- NoWarn NU1903 removido; o comentário dizia que a correção exigia .NET 10, o que não era verdade (SIPSorcery 10 tem alvo net8.0). O impedimento real eram as DLLs do FFmpeg 8 — documentado no CLAUDE.md
- Prova: tentativa com só o SIPSorcery 10 falhou em 3 testes (`Codec H264 is not supported`); com o conjunto completo, 153/153 verdes, 0 avisos de build, app abre com `--demo` sem erro no error.log

## Pendente
- Teste de live real entre duas máquinas na VPN (vídeo, áudio, reconexão) antes da próxima release — Alta, só humano

## Aceitas
- Nenhuma

## Decidido depois
- 2026-10-05 (/11-performance): FFmpegLibs fica com ~253 MB. O FFmpegInit chama avdevice_register_all e a avdevice depende da avfilter (124 MB) — sem as duas o app não inicializa o FFmpeg. Instalador: 121 MB

## Desatualizadas sem vulnerabilidade (manutenção futura)
- NAudio 2.2.1 → 3.1.0 (major; mexe no áudio)
- Vortice.Direct3D11 / Vortice.DXGI 3.6.2 → 3.8.3
- Websocket.Client 5.5.0 → 5.5.1 · System.Drawing.Common 10.0.11 → 10.0.12

## Revisar
- Remendo de System.Net.Http/RegularExpressions: tirar quando o SIPSorcery largar o IPNetwork2 2.x
