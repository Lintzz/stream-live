# Contexto do projeto
Atualizado em: 2026-10-05

## Projeto
- Tipo: app desktop Windows (WPF) — transmissão de tela e áudio entre amigos na Radmin VPN
- Perfil: B — Pessoal, publicado (código aberto, amigos instalam pelas releases)
- Cliente ou projeto próprio: próprio
- Estágio: producao (releases no GitHub com auto-update; não há site nem domínio)
- URL atual: https://github.com/Lintzz/stream-live/releases
- Domínio definitivo: não se aplica
- Hospedagem: GitHub Releases (instalador + .sha256)
- Repositório: público — github.com/Lintzz/stream-live, branch main

## Ambiente
- MCPs conectados e testados: Context7 (2026-10-05, resolveu SIPSorcery e devolveu exemplos de RTCPeerConnection.SendVideo)
- Ressalva do Context7: a doc do SIPSorcery é do master (linha 10.x); o projeto usa 8.0.23 — API específica da 8.0 se confirma pelo XML/IntelliSense do pacote instalado
- GitHub: sem MCP, coberto pelo `gh` CLI (conta Lintzz, escopos repo + workflow): releases, Actions, visibilidade
- MCPs que ajudariam e não estão conectados: nenhum necessário (sem banco, hospedagem web ou pagamento)
- Acessos que tenho: repositório e releases via `gh`; não há painel, DNS nem banco
- Variáveis de ambiente: o projeto não usa (.env inexistente e desnecessário)
- Ferramentas locais: git 2.55 + git-lfs 3.7.1, gh 2.97, SDK .NET 8.0.424 em .dotnet/, Inno Setup 6 (ISCC)
- Modo de uso dos MCPs: somente leitura (confirmado pelo usuário em 2026-10-05)

## Identidade
- Dono do produto: Lintzz
- Nome do produto: Stream Live
- Descrição: Transmissão de tela e áudio entre amigos na Radmin VPN, com várias lives em grade.
- Id do app: `AppId=Stream Live` no build/setup.iss (explícito desde 2026-10-05, mesmo valor que valia por padrão; não mudar: instalaria uma segunda cópia)
- Autor: Alexandre Lintz (Lintzz) · metadados no StreamLiveApp.csproj e no setup.iss
- Licença: MIT

## Como rodar e gerar build
- Tipo: .NET 8 WPF (net8.0-windows10.0.19041.0), SDK local em .dotnet/
- Rodar (teste rápido): `.\.dotnet\dotnet.exe build StreamLive.sln -c Release` e abrir `src\StreamLiveAppin\Release
et8.0-windows10.0.19041.0\StreamLiveApp.exe --demo` (espere ~5 s pelo MainWindowHandle)
- Antes de abrir: conferir `Get-Process StreamLiveApp`. Instância aberta pode estar transmitindo para amigos — perguntar antes de encerrar. Sem `--demo`, duas instâncias brigam pela porta 8080
- Logs: `%LOCALAPPDATA%\StreamLiveApp\error.log` e `audio_error.log`
- Build (skill `/build`): `if (Test-Path publish_zip) { Remove-Item -Recurse -Force publish_zip }; .\.dotnet\dotnet.exe publish src\StreamLiveApp\StreamLiveApp.csproj -c Release -r win-x64 --self-contained true -o publish_zip; & "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" build\setup.iss` → `StreamLive_Setup.exe` na raiz (copiar para builds/windows/ com a versão no nome)
- Release: anexar um `.exe` e o `.sha256` dele: `(Get-FileHash <exe> -Algorithm SHA256).Hash.ToLower() + "  <nome do exe>" | Set-Content <exe>.sha256`
- Versão atual: 1.0.38 (tag v1.0.38) · última entrega: v1.0.38, 2026-10-04, publicada no GitHub
- Versão mora em: `<Version>` do StreamLiveApp.csproj (gera AppInfo.Version e build/version.iss)
- Dependências conferidas em: 2026-10-05 — 0 vulnerabilidades depois da subida para SIPSorcery 10.0.17 + FFmpeg 8.1.2; NAudio e Vortice atrasados
- Detectado em: 2026-10-05

## Stack e serviços
- SIPSorcery 10.0.17 + FFmpeg 8.1.2 gyan.dev full shared (WebRTC/H.264) — gratuito
- Fleck / Websocket.Client — sinalização e áudio PCM, porta 8080
- NAudio + ApplicationLoopback.dll — captura de áudio com exclusão do Discord
- Vortice (DXGI) com fallback GDI — captura de tela
- API de releases do GitHub — auto-update
- Sem banco, login, analytics ou servidor próprio

## O que existe hoje
- Host e viewer na mesma janela, várias lives em grade, PiP e modo teatro
- Lista de amigos com status online, sala com senha (HMAC + AES-GCM), live privada com convidados
- Aviso de Radmin VPN fechada, diagnóstico embutido, modo demonstração
- Auto-update verificado por SHA-256
- 153 testes xUnit + CI no GitHub Actions (últimas 3 execuções verdes)

## Planejado, ainda não feito
- Trocar o ícone do app
- Medir e afinar desempenho: 1080p com som bom e fps estável sem pesar no PC

## Decisões
- Áudio em PCM pelo WebSocket, não Opus — o Opus (v1.0.18–21) nunca funcionou em campo (detalhe no CLAUDE.md)
- Discord sempre excluído do áudio; "Modo leve" e toggle GDI removidos (fixos)
- 2026-10-05: perfil B; objetivo: verificar e corrigir, depois continuar desenvolvendo
- 2026-10-05: .gitignore ganhou exceções do projeto ao bloco do kit (build/ e .claude/lz/)
- 2026-10-05: sem MCP do GitHub — o `gh` CLI já cobre o que as auditorias precisam
- 2026-10-05: SIPSorcery 8→10 com FFmpeg 8.1.2 para fechar 2 Altas; não exigiu .NET 10. Transitivas System.Net.Http/RegularExpressions fixadas nas versões corrigidas
- 2026-10-05: versionamento SemVer de verdade a partir da v1.0.38 (feat → menor, fix → correção), sem betas, release local com pergunta antes do push; política no CONVENCOES.md
- 2026-10-05: skills antigas publish-release, build-installer e testar removidas; o projeto usa /build e /rodar do kit
- 2026-10-05: nível de teste 3 — AppSmokeTests abre o .exe compilado em --demo
