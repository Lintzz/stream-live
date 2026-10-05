# Convenções do Stream Live
Descreve o padrão que o código já segue. Arquitetura e armadilhas estão no CLAUDE.md.

## Pastas
- `src/StreamLiveApp/Core` — orquestração (StreamManager, HostBroadcast, ViewerSession, AppPaths, UpdateManager)
- `Media` — captura e áudio · `Network` — sinalização · `Services` — persistência e sondagens
- `Views` — janelas XAML + code-behind · `Models` — dados · `Helpers` — utilidades de janela
- `tests/StreamLiveApp.Tests` — um arquivo `<Assunto>Tests.cs` por área

## Código
- Idioma: português em comentários, UI, mensagens de log e commits; identificadores em inglês
- Classes em PascalCase; campos privados `_camelCase`; Nullable e ImplicitUsings ligados
- Comentário explica o porquê (bug real + decisão). Ao mexer, preserve ou atualize — não apague
- Lógica testável sai como `internal static` puro, alcançada por InternalsVisibleTo
- UI do viewer por binding em ViewerSession (INotifyPropertyChanged); code-behind só para foco, grade, PiP e teatro
- Caminhos de arquivo só via AppPaths; gravação de settings em `.tmp` + move
- Versão só no `<Version>` do csproj

## Versionamento
- Padrão: SemVer (MAIOR.MENOR.CORREÇÃO), contado a partir da 1.0.38 (tag `v1.0.38`) — antes disso toda release subia só o último número
- A 1.0.0 já passou: o app está em uso pelos amigos desde as primeiras releases
- Correção: imagem borrada depois de perda de pacote, som mudo, congelamento ao parar a live, atualização de dependência por segurança
- Menor: live privada com convidados, aviso de Radmin fechada, diagnóstico embutido, grade de lives, ícone novo, ganho de desempenho perceptível
- Maior: improvável. Seria quebrar a conversa entre versões (o amigo com a versão antiga deixa de conseguir assistir ou transmitir) ou perder `friends.json`/`settings.json`
- Betas: não. O auto-update lê `releases/latest`; teste antes de publicar é live real com um amigo
- Build de teste não sobe versão; build de entrega sobe
- Tag: vX.Y.Z (anotada) · Changelog: CHANGELOG.md
- Ao gerar build de entrega: commit e tag locais, e perguntar antes do push e da Release no GitHub
- Release anexa **um** `.exe` e o `.sha256` dele (`<hash>  <arquivo>`): sem o `.sha256` o auto-update recusa a atualização
- `AppId` do `build/setup.iss` é fixo: mudar instala uma segunda cópia em vez de atualizar

## Commits
- Conventional Commits em português (hook `.githooks/commit-msg`): feat:, fix:, docs:, refactor:, chore:, e ! para mudança que quebra. O histórico até a v1.0.38 usa outro formato; não reescrever
- Release: `chore(release): vX.Y.Z`
- `core.hooksPath` aponta para `.githooks`, onde ficam também os hooks do git lfs — clone novo: `git config core.hooksPath .githooks`

## Testes
- Nível: 3 — unidade nas regras puras + smoke no app compilado
- Rodar rápido e rodar tudo (são o mesmo, ~15 s): `.\.dotnet\dotnet.exe test StreamLive.sln -c Release`
- Só o smoke: `.\.dotnet\dotnet.exe test StreamLive.sln -c Release --filter "FullyQualifiedName~AppSmokeTests"`
- Onde ficam: `tests/StreamLiveApp.Tests` (xUnit), um `<Assunto>Tests.cs` por área. `AppSmokeTests` abre o `StreamLiveApp.exe --demo` compilado; `SignalingHandshakeTests` sobe servidor real em porta livre; `VideoEncoderFormatTests` usa as DLLs reais do FFmpeg
- Dados de teste: `--demo` não abre a porta 8080 nem grava `friends.json`/`settings.json`; nunca rode teste com a lista de amigos real
- O que não tem teste automático: live real entre duas máquinas (vídeo, som, reconexão) — obrigatória antes de release que mexe em captura, encoder, rede ou dependência de mídia
- Mudança visível na UI: abrir com `--demo` (skill `rodar`) e conferir na tela
- **Fluxo novo ganha caso na verificação.** Bug corrigido ganha caso que falhava antes.

## A padronizar aos poucos
- Namespace: a maioria usa `namespace StreamLiveApp` em bloco; Models e Services usam subnamespace e um arquivo usa file-scoped. Código novo: `namespace StreamLiveApp` em bloco
