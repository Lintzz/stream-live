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
- Id do app: AppId do Inno Setup em build/setup.iss (não mudar: quebraria a atualização do instalado)
- Licença: MIT

## Como rodar e gerar build
- Tipo: .NET 8 WPF (net8.0-windows10.0.19041.0), SDK local em .dotnet/
- Rodar (teste rápido): build Release + `StreamLiveApp.exe --demo` (skill `testar`)
- Build: `/build-installer` → StreamLive_Setup.exe na raiz; release completa: `/publish-release`
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
