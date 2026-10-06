# Contexto do projeto
Atualizado em: 2026-10-06

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
- Ressalva do Context7: a doc do SIPSorcery é do master; o projeto usa 10.0.17 — detalhe de API se confirma por reflexão no pacote instalado ou na fonte (src/FFmpegVideoEncoder.cs no GitHub)
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
- Rodar (teste rápido): `.\.dotnet\dotnet.exe build StreamLive.sln -c Release` e abrir `src\StreamLiveApp\bin\Release\net8.0-windows10.0.19041.0\StreamLiveApp.exe --demo` (espere ~5 s pelo MainWindowHandle)
- Rodar para testar live real (encoder, rede): o mesmo .exe sem `--demo` — sobe a 8080 e abre o Radmin; o `--demo` não passa pelo encoder
- Antes de abrir: conferir `Get-Process StreamLiveApp`. Instância aberta pode estar transmitindo para amigos — perguntar antes de encerrar. Sem `--demo`, duas instâncias brigam pela porta 8080
- Logs: `%LOCALAPPDATA%\StreamLiveApp\error.log` e `audio_error.log`
- Build (skill `/build`): `if (Test-Path publish_zip) { Remove-Item -Recurse -Force publish_zip }; .\.dotnet\dotnet.exe publish src\StreamLiveApp\StreamLiveApp.csproj -c Release -r win-x64 --self-contained true -o publish_zip; & "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" build\setup.iss` → `StreamLive_Setup.exe` na raiz (copiar para builds/windows/ com a versão no nome)
- Release: anexar um `.exe` e o `.sha256` dele: `(Get-FileHash <exe> -Algorithm SHA256).Hash.ToLower() + "  <nome do exe>" | Set-Content <exe>.sha256`
- Versão atual: 2.1.0 · última entrega: v2.1.0, 2026-10-05, publicada no GitHub (https://github.com/Lintzz/stream-live/releases/tag/v2.1.0); builds/windows/StreamLive-Setup-2.1.0.exe (121 MB) + .sha256; 244 testes; anterior: v2.0.0 (mesmo dia)
- Asset da release: sempre `StreamLive_Setup.exe` + `StreamLive_Setup.exe.sha256` (nome usado por todas as releases); a cópia em builds/ leva a versão no nome
- Versão mora em: `<Version>` do StreamLiveApp.csproj (gera AppInfo.Version e build/version.iss)
- Dependências conferidas em: 2026-10-06 — 0 vulnerabilidades (app e testes); atrasadas: NAudio 2.2.1→3.1.0 (major), Vortice 3.6.2→3.8.3, System.Drawing.Common 10.0.11→10.0.12, Websocket.Client 5.5.0→5.5.1
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
- Desde a 2.0 (ainda não lançada): protocolo de sala v2 (salt por sala, chaves separadas), bloqueio após 5 senhas erradas, descarte do que não decifra; desfazer remoção de amigo; confirmação ao fechar com amigos assistindo; validação de IP; uso completo pelo teclado e leitor de tela; vídeo com teto de 8 Mbps e encode só com público
- Desde a 2.1.0 (2026-10-05): ícone novo; Radmin aberto sozinho na bandeja (/minimized) quando está fechado na abertura do app, e ligado (on-line) sozinho quando sobe off-line; ao fechar, confirmação com "Fechar o Radmin VPN também" (lembrada)
- Desde a 2.1.1 (não lançada, 2026-10-06): encoder com deblock, AQ e busca de movimento (movimento rápido com metade do bitrate e sem blocos), keyframe periódico a cada 5 s e nenhum automático do x264
- 254 testes xUnit (inclui smoke do .exe) + CI no GitHub Actions

## Planejado, ainda não feito
- Teste com amigo na 2.1.0 publicada: Radmin abrindo na bandeja, ficando on-line e fechando junto (ver auditorias/features/radmin-e-icone.md)
- Teste com amigo da qualidade de vídeo (2.1.1, ainda não lançada): fps, CPU e perda no diagnostico.log — ver auditorias/features/qualidade-de-video.md

## Radmin VPN (levantado em 2026-10-05, Radmin 2.1.1 / 2.1.4951.1 nesta máquina)
- Serviço é `RvControlSvc` (Auto), não `RvpnService`; a GUI é `RvRvpnGui.exe`, manifest `asInvoker` (UI Automation funciona sem admin)
- Instalação: chave de desinstalação `{D9EE3D13-28AC-4638-9485-FFB52242E1D0}`, `InstallLocation` em `C:\Program Files (x86)\Radmin VPN\`; dados em `HKLM\SOFTWARE\WOW6432Node\Famatech\RadminVPN\1.0`
- Linha de comando: só `/minimized` (o autostart usa). Nenhum argumento, protocolo de URL ou API para entrar em rede
- GUI em Qt Widgets: todo controle tem `AutomationId` = caminho de objectName, igual em qualquer idioma. Diálogo de entrar: `MainWindow.DlgJoinNetwork` (campos `...tab_private.LENetName`/`LEPassword`, botões `...joinPushButton`/`cancelPushButton`); menu `Rede` → `Conectar à rede`; lista de redes em `...CNetworkWidget.NetworkWidget.NetworkName`; liga/desliga em `...userInfoWidget.BPower` (TogglePattern)
- 2026-10-05, decisões da nova-feature: "Desconectar" = desligar pelo BPower (não sair da rede); serviço parado → botão que pede UAC só no clique; tela = aba "Radmin VPN" nas Configurações + atalho no aviso de abertura; teste real só com rede inexistente aleatória
- 2026-10-05, depois do teste: a janela do Radmin precisa rodar (fechada = amigos offline). Entrar na rede pelo app foi removido; o app só abre o Radmin com /minimized
- O IP 26.x é da conta, não da rede: adaptador "Radmin VPN" com 26.x prova que o Radmin está ligado, não que você está numa rede específica

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
- 2026-10-05: protocolo de sala v2 (salt por sala, chaves Auth/Enc) — quebra compatibilidade com a 1.0.38 em sala com senha; próxima release é 2.0.0
- 2026-10-05: instalador sem assinatura Authenticode por decisão (sem custo); mitigação: 2FA no GitHub
- 2026-10-05: vídeo com teto de 8 Mbps (CRF 23 + VBV) e 60 fps declarados; sem encode sem público. AMF e decode por GPU medidos e descartados por ora
- 2026-10-05, nova-feature qualidade de vídeo: o jogo continua na frente (processo e threads seguem BelowNormal); o preset do x264 é escolhido por medição, com teto de ~1,5× o custo de CPU atual; seletor 1080p/720p fica para depois
