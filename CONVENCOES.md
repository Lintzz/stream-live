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

## Commits
- Conventional Commits em português (hook `.githooks/commit-msg`). O histórico até a v1.0.38 usa outro formato; não reescrever
- Bump de versão: `chore(release): v1.0.x`

## Testes
- Verificação rápida (rodar ao fim de toda mudança):
  `.\.dotnet\dotnet.exe test StreamLive.sln -c Release`
- Casos ficam em `tests/StreamLiveApp.Tests` (xUnit); `SignalingHandshakeTests` sobe servidor real em porta livre
- Bug corrigido ganha primeiro o teste que falha; fluxo novo ganha caso
- Mudança na UI ou na captura: abrir com `--demo` (skill `testar`) e conferir na tela
- Nível alvo: 3 (app instalável) — falta o smoke que abre o executável compilado

## A padronizar aos poucos
- Namespace: a maioria usa `namespace StreamLiveApp` em bloco; Models e Services usam subnamespace e um arquivo usa file-scoped. Código novo: `namespace StreamLiveApp` em bloco
