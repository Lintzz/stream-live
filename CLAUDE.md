# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Idioma

O repositório inteiro é escrito em português — README, comentários de código, mensagens de
commit e textos de UI. Mantenha esse padrão ao escrever código novo.

## Comandos

O SDK fica em `.dotnet/` (fora do versionamento). Num clone novo essa pasta não existe: use
`dotnet` global (o `global.json` já fixa a linha 8.0) ou recrie a pasta com o
`dotnet-install.ps1 -Channel 8.0 -InstallDir .dotnet`.

```powershell
# Compilar
.\.dotnet\dotnet.exe build StreamLive.sln -c Release

# Rodar todos os testes
.\.dotnet\dotnet.exe test StreamLive.sln -c Release

# Rodar uma classe / um teste
.\.dotnet\dotnet.exe test StreamLive.sln --filter "FullyQualifiedName~DuplicationRecoveryTests"
.\.dotnet\dotnet.exe test StreamLive.sln --filter "FullyQualifiedName~SignalingHandshakeTests.CorrectPasswordIsAccepted"

# Publicar + gerar o instalador (é o comando que a skill /build usa)
if (Test-Path "publish_zip") { Remove-Item -Recurse -Force "publish_zip" } ; & ".\.dotnet\dotnet.exe" publish src\StreamLiveApp\StreamLiveApp.csproj -c Release -r win-x64 --self-contained true -o "publish_zip" ; & "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" build\setup.iss
```

**Modo demonstração (`--demo`, `DemoMode`):** amigos fictícios, lives desenhadas pelo app, sem
servidor na 8080 e sem gravar `friends.json`/`settings.json`. É o que gera o GIF e os prints de
`docs/images/` — regrave por ele quando a UI mudar, nunca com a lista de amigos real.

**Git LFS é obrigatório.** As DLLs do FFmpeg em `src/StreamLiveApp/FFmpegLibs/` (~250 MB)
vivem no LFS. Sem `git lfs`, o working tree recebe ponteiros de texto e o build falha ao
carregar o FFmpeg (`git lfs pull` conserta um clone já feito). São o build *full shared* do
gyan.dev, e a versão é presa à do `FFmpeg.AutoGen` que o `SIPSorceryMedia.FFmpeg` traz (hoje
8.1 → FFmpeg 8.1.x, `avcodec-62`). Subir o SIPSorcery sem trocar as DLLs junto quebra o encoder
H.264 — o `VideoEncoderFormatTests` pega isso.

**Versão em um lugar só:** `<Version>` no `StreamLiveApp.csproj`. Dali saem o
`AssemblyVersion`, o `AppInfo.Version` mostrado na UI e o `build/version.iss` (gerado pelo
target `GenerateInnoSetupVersion`, consumido pelo `setup.iss`). Nunca edite `version.iss` nem
repita a versão no XAML. Release pela skill `/build` do kit (SemVer pelos Conventional Commits,
`CHANGELOG.md`, tag anotada). A release precisa anexar **um** `.exe` e o `.sha256` dele
(`<hash>  <arquivo>`): o `UpdateManager` pega o primeiro de cada tipo, e sem o `.sha256` o
auto-update recusa a atualização. O `AppId` do `setup.iss` é fixo — mudar cria uma segunda
instalação em vez de atualizar.

O CI (`.github/workflows/ci.yml`) roda restore + build + test em `windows-latest`.

## Arquitetura

App WPF (.NET 8, `net8.0-windows10.0.19041.0`) que transmite tela e áudio entre amigos numa
Radmin VPN. A mesma janela é host e viewer ao mesmo tempo.

### Dois caminhos, um `StreamManager`

`StreamManager` (Core) é a peça central e serve os dois papéis — no host cria capturadores e
encoder, no viewer só decodifica. `EnsureCapturers()` só roda no host; `_isHost` separa o resto.

- **Host:** `MainWindow` → `HostBroadcast` (ciclo de vida da live, zero UI) → `StreamManager`
  → `VideoCapturer`/`AudioCapturer`. O `SignalingServer` (Fleck, porta 8080) sobe **junto com o
  app**, não com a live: é ele que responde ao `STATUS_CHECK` dos amigos e faz você aparecer
  como online na lista deles.
- **Viewer:** `MainWindow` → uma `ViewerSession` por live aberta (várias em grade) →
  `SignalingClient` (Websocket.Client) + `StreamManager`. `ViewerSession` é `INotifyPropertyChanged`
  e a UI faz binding nela; o code-behind do `MainWindow` cuida só de foco, grade, PiP e teatro.

### Transportes (assimétricos de propósito)

| Mídia | Codec | Caminho |
|---|---|---|
| Vídeo | H.264 (FFmpeg, `ultrafast`+`zerolatency`) | trilha de vídeo do WebRTC (SIPSorcery) |
| Áudio | PCM 44,1 kHz estéreo 16 bits, **sem compressão** | quadro binário no WebSocket de sinalização, prefixado pelo byte `1` |
| Sinalização | JSON (`SignalingMessage`) | WebSocket `ws://` porta 8080 |

O áudio já viajou em Opus pela trilha do WebRTC (v1.0.18–v1.0.21) e foi **revertido** por nunca
ter funcionado em campo. Custa ~1,4 Mbps por viewer e não sincroniza com o vídeo; a deriva de
relógio é contida pelo `LatencyTrimmingProvider` (teto 250 ms, alvo 80 ms). Não reintroduza o
Opus sem medir ponta a ponta.

### Handshake de sala (`SignalingServer` ↔ `ViewerSession`)

`STATUS_CHECK`/`STATUS_RESPONSE` · `AUTH_REQUIRED`(`v2:salt:desafio`) → `AUTH`(`v2:HMAC`) →
`AUTH_OK`/`AUTH_FAIL`/`AUTH_LOCKED`/`AUTH_OUTDATED` · `CLIENT_CONNECTED` · `offer`/`answer`/`ice` ·
`STREAM_STARTED`/`STREAM_STOPPED`/`SOURCE_CHANGED`.

Detalhes que quebram fácil:
- O host responde `AUTH_REQUIRED` a **cada** mensagem pré-autenticação (o `CLIENT_CONNECTED` e
  um por candidato ICE). `ViewerSession._passwordPromptOpen` impede que isso abra quatro modais.
- Só quem manda `CLIENT_CONNECTED` entra em `_viewers`; conexões de `STATUS_CHECK` ficam fora do
  broadcast, senão recebem áudio binário no lugar do `STATUS_RESPONSE`.
- `SignalingServer.NormalizeIp` existe porque o Fleck entrega IPv4 mapeado (`::ffff:x.x.x.x`) e
  `::1`; sem normalizar, nada casa com a lista de amigos. `127.0.0.1` sempre passa.
- O `OnOpen` tem **dois** portões independentes, decididos por `ShouldAcceptConnection` (lógica
  pura, testada): a lista de amigos e, durante uma **live privada**, a lista de convidados
  daquela live (`SetLiveVisibility`, zerada pelo `AnnounceStop`). Recusar no `OnOpen` é o que
  esconde a live: o `FriendStatusService` do outro lado falha ao conectar e reporta *offline*,
  e a mesma recusa nega a entrada, sem precisar filtrar `RegisterViewer` nem o broadcast. A
  recusa por live privada **não** dispara `OnConnectionRejected`: o evento vira um aviso no
  rodapé (`ShowTransientStatus`), e cada não convidado sonda a cada 5 s.
- A senha nunca trafega. **Protocolo de sala v2** (desde a 2.0): o host sorteia um salt a cada
  senha (`RoomPassword`), que vai no desafio; `CryptoHelper.DeriveRoomKeys` faz PBKDF2 (200k
  iterações, cache obrigatório — derivar custa ~100 ms e o áudio cifra ~50×/s) e divide a
  chave-mestra por HKDF em `Auth` (HMAC do desafio) e `Enc` (AES-GCM, formato
  `[nonce 12][tag 16][cipher]`). A v1 tinha salt fixo do app e uma chave só. **v1 e v2 não
  conversam em sala com senha** (sem senha, conversam): host v2 responde `AUTH_OUTDATED` e
  avisa no rodapé (`ShowTransientStatus`); viewer v2 que recebe desafio sem `v2:` mostra "versão antiga".
- Autenticar vale para a senha em uso: trocar ou zerar `RoomPassword` (o `AnnounceStop` zera)
  desautentica todos. Por isso o `STREAM_STARTED` sai por `BroadcastStreamStarted`, em claro para
  quem ainda não autenticou, e o viewer esquece a chave no `STREAM_STOPPED`. Com chave ativa, o
  `SignalingClient` **descarta** o que não decifra, exceto o controle em claro (`AUTH_*`,
  `STATUS_RESPONSE`, `PONG`) — usar o dado bruto anulava a autenticação do AES-GCM.
- `AuthThrottle`: 5 senhas erradas por IP bloqueiam 60 s (`AUTH_LOCKED`), contadas por IP e não
  por conexão. Mensagem de texto acima de 64 KB derruba a conexão. Com a lista de amigos
  desligada, `ShouldAcceptConnection` ainda exige a faixa da Radmin (`26.0.0.0/8`).

### Captura de tela com fallback

`VideoCapturer` tenta `DesktopDuplicationGrabber` (DXGI/Vortice) e cai para `CopyFromScreen`
(GDI) quando a duplicação não existe (RDP, driver antigo) ou nunca entregou quadro. A duplicação
**morre** em situações comuns (tela cheia exclusiva, UAC, troca de modo de vídeo) e não volta
sozinha — `DecideDuplicationAction` decide entre usar / esperar / recriar / desistir para o GDI.
O quadro sai em **BGRA cru**; a conversão de cor é do swscale, e `VideoEncoderFormatTests` trava
essa decisão contra upgrades do SIPSorcery.

### Encoder de vídeo: taxa declarada e teto

`StreamManager.CreateH264Encoder` usa libx264 `ultrafast`+`zerolatency` com CRF 23 e teto pelo VBV
(`x264-params` `vbv-maxrate`=`MaxVideoKbps` 8000, `vbv-bufsize` metade). Dois detalhes do SIPSorcery
que quebram isso em silêncio: o `EncodeVideo` inicializa o encoder dizendo 30 fps (e o `ForceIdr`
o recria a cada keyframe), então `PrepareEncoder` declara `TargetFps` (60) antes de **cada** quadro;
e as opções vão para o `priv_data` do x264, onde `maxrate`/`bufsize` não existem. O
`EncoderBitrateTests` trava as duas coisas. Sem nenhum peer conectado o host **não codifica**
(`ShouldEncode`); o fps mostrado e o aviso de saúde (`DecideHealthWarning`) olham a captura.
Medições e alternativas descartadas (AMF, decode por GPU) em `auditorias/11-performance.md`.

### Áudio: exclusão por processo

`AudioCapturer` usa `WasapiLoopbackCapture` para o sistema inteiro, ou `ProcessAudioCapturer`
(P/Invoke em `NativeLibs/ApplicationLoopback.dll`, Windows 10 20348+) para **excluir** um
processo. O processo excluído é **sempre o Discord**: sem isso a mesa se escuta em eco. O alvo é
fixado por **nome** (`AudioExclusionService.ResolvePid` resolve o PID a cada live, porque o
programa pode ter sido reaberto); com o Discord fechado o PID sai 0 e a captura volta ao loopback
do sistema inteiro, sem aviso. `SetTargetProcess` reabre a captura fora da thread de UI — os
parâmetros só chegam ao Windows na abertura.

Isto já foi um ComboBox nas configurações. Junto com ele saíram o "Modo leve" (agora fixo:
preset `ultrafast`, escala por vizinho mais próximo e prioridade `BelowNormal`) e o toggle
manual de captura GDI — o **fallback automático** DXGI→GDI descrito acima continua valendo.
Nenhuma das três era usada como escolha; só um dos valores rodava.

### Radmin VPN pela tela do app

A aba "Radmin VPN" das configurações (`RadminConnectionViewModel`) entra numa rede do Radmin
sem abrir o Radmin. O Radmin **não tem** linha de comando nem API para isso (o executável só
aceita `/minimized`), então `RadminUiAutomation` dirige a janela dele por UI Automation — é a
**única** classe que toca a interface do Radmin, com os seletores em constantes. A GUI é Qt
Widgets e publica o caminho de objectName como `AutomationId`, igual em qualquer idioma; só os
menus vão por nome (pt/en) com a posição como reserva. Armadilhas medidas no Radmin 2.1.1:
- O primeiro `MenuBar` da árvore é o menu de sistema do Windows ("Sistema"), não o do Qt: a
  barra se acha pelo id. Nunca busque a partir da raiz com `Descendants` (varre o desktop).
- O Qt prende a janela na tela pelo `TransformPattern`; fora da tela só com `SetWindowPos`.
- Na bandeja, a janela some da árvore; abrir o `.exe` de novo traz a instância (é única), e o
  X do Radmin a esconde de volta. Erro de entrar vira tooltip (`QTipLabel`) com o diálogo
  aberto, e "rede inexistente" e "senha errada" dão a **mesma** mensagem.
- O serviço é `RvControlSvc` e segura a VPN sozinho; a GUI roda sem admin. Iniciar o serviço
  pede UAC só no clique (`sc start` com `runas`) — o app não roda como admin.
- O IP 26.x é da conta, não da rede: só a lista de redes da janela prova "estou na rede X".
- Fechamento só desfaz o que o app fez na sessão (`DecideExitCleanup`). A senha fica em
  `radmin_rede.dat` (DPAPI, CurrentUser), fora do `settings.json` e do relatório de
  diagnóstico; nem senha nem nome da rede vão para o log.

### Persistência e estado

`friends.json` e `settings.json` em `%LOCALAPPDATA%\StreamLiveApp\` (mesma pasta de
`error.log` e `audio_error.log`). O caminho sai **só** do `AppPaths` — ele cria a pasta e, na
primeira execução depois do rename do app, move a pasta da versão anterior por cima da nova
(sem isso o usuário abriria o app com a lista de amigos vazia). Montar o caminho à mão em
outro lugar reintroduz o bug: quem criasse a pasta nova primeiro cancelaria a migração.
`SettingsService.Save` grava em `.tmp` e move por cima.
`UpdateManager` consulta a release mais recente do GitHub (`AppInfo.RepositoryOwner/Name`) e
**exige** o `.sha256` publicado ao lado do instalador.

## Convenções

- Comentários explicam **por quê**, não o quê — quase todo bloco de comentário no código
  documenta um bug real e a decisão que o fechou. Ao mexer numa dessas áreas, preserve ou
  atualize a explicação em vez de apagá-la.
- `Nullable` e `ImplicitUsings` ligados nos dois projetos.
- Lógica testável é exposta como `internal static` puro (`DecideDuplicationAction`,
  `ShouldReemit`, `CopyRect`, `NormalizeIp`) e alcançada pelo `InternalsVisibleTo` para
  `StreamLiveApp.Tests`. Prefira esse formato a testar através da UI ou da captura real.
- Testes são xUnit em `tests/StreamLiveApp.Tests/`; o `SignalingHandshakeTests` sobe um
  `SignalingServer` real em porta livre. Os `PackageReference` de `System.Net.Http` e
  `System.Text.RegularExpressions` são remendo de transitivas vulneráveis (motivo no csproj).

<!-- lz:inicio v0.23.0 — gerado pelo kit lz; edições dentro deste bloco são sobrescritas na atualização -->
# Padrões do projeto (kit lz)

Este projeto usa o kit lz. As regras abaixo valem em **toda** tarefa, não só quando uma skill é chamada.

## Commits

- **Todo commit segue Conventional Commits**: `tipo(escopo opcional): descrição`.
- Tipos: `feat` (algo novo), `fix` (correção), `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`. Mudança que quebra algo leva `!`: `feat!: remove login por senha`.
- Descrição em português, no imperativo, começando em minúscula, sem ponto final, com até ~70 caracteres. Detalhes vão no corpo, depois de uma linha em branco.
- Um commit por mudança lógica. Não junte correção e funcionalidade nova no mesmo commit — o tipo do commit decide a próxima versão.
- O hook `.githooks/commit-msg` recusa commit fora do padrão. Se ele recusar, corrija a mensagem; não use `--no-verify` sem o usuário pedir.
- Nunca commite `.env`, chaves, keystore, certificados nem a pasta `builds/`.
- Não faça `push` sem o usuário pedir.

## Antes de trabalhar

- Contexto do projeto: `auditorias/contexto.md`. Leia antes de perguntar ao usuário algo que pode estar lá.
- Padrões de código, pastas, nomes e versionamento: `CONVENCOES.md`, se existir. Código novo segue esse arquivo.
- Plano e escopo: `PLANO.md`, se existir. Se ele tiver a lista "Não vai ter", aquilo foi cortado de propósito: não construa sem o usuário pedir.
- Passo a passo do projeto: `auditorias/roteiro.md`. Toda skill do fluxo marca a própria linha ao terminar e fecha a resposta dizendo o comando da próxima etapa.

## Padrões que você carrega sozinho, sem o usuário pedir

Estes **não são comandos**. São padrões obrigatórios. Quando a situação abaixo acontecer, leia o arquivo indicado **antes de escrever o código** e siga. Não pergunte se deve carregar, não espere o usuário digitar o comando, e não peça aprovação para seguir o padrão — só para as mudanças em si.

| Quando | Leia |
|---|---|
| Vai escrever código com uma biblioteca ou framework — confirme a API **da versão instalada** antes | `.claude/lz/DOCUMENTACAO.md` |
| Terminou de mexer em qualquer coisa, corrigiu um bug, ou um fluxo novo ficou pronto | `.claude/skills/testes/SKILL.md` |
| A instalação de pacotes reportou vulnerabilidade, entrou uma dependência nova, ou a checagem diária do `rodar` achou Crítica ou Alta em produção | `.claude/skills/dependencias/SKILL.md` |
| Vai criar ou alterar tabela, coleção, schema, migration, query, índice, policy de RLS ou regra do Firestore | `.claude/skills/banco/SKILL.md` |
| Vai construir ou alterar tela, lista, formulário, botão ou componente interativo | `.claude/skills/06-interface/SKILL.md` |
| A mudança traz um serviço externo novo (banco, autenticação, pagamento, e-mail, hospedagem) | `.claude/skills/01-ambiente/SKILL.md`, em modo preparar |

Resumo do padrão de testes, que vale em toda tarefa: **mexeu, verifica.** Rode a verificação rápida do projeto (o comando está na seção Testes do `CONVENCOES.md`) ao fim de cada mudança, e não siga escrevendo código em cima de vermelho. Bug encontrado ganha **primeiro** a verificação que falha, depois a correção — teste escrito depois da correção passa por construção e não prova nada. Fluxo novo pronto ganha caso na verificação. Se a verificação falhar, o culpado é o código até prova em contrário: nunca enfraqueça a asserção nem atualize snapshot para o teste passar. Projeto sem verificação nenhuma: rode o projeto como a skill `rodar` faz; e se o nível do projeto pede teste (2 ou 3, pelo perfil), carregue `testes` em modo montar e proponha o mínimo junto com a tarefa — não espere o usuário pedir.

Resumo do padrão de documentação, que vale em toda tarefa: o que você sabe de cor é de uma versão qualquer, o projeto usa uma versão específica. Consulte o MCP do serviço quando houver, senão o Context7 (`resolve-library-id` → `query-docs`), senão o site oficial. Não precisa consultar para JS/CSS puro nem para algo que o projeto já faz em outro arquivo — nesse caso copie o padrão de lá. Nunca invente API: se não achou, diga, escreva do jeito conservador e marque `TODO(doc)`.

Ao adicionar algo novo a um projeto já auditado, use a skill `nova-feature` — essa sim é um comando, porque envolve planejar e aprovar antes.

## Skills do kit, em ordem

`00-planejar` → `01-ambiente` → `02-configurar` → `03-desenvolver` → `04-git` → `05-conversao` → `06-interface` → `07-seguranca` → `08-lgpd` → `09-seo` → `10-acessibilidade` → `11-performance` → `12-pre-lancamento` → `13-dominio` → `14-revisao-geral` → `projeto-limpo` (só se for entregar a um cliente)

Projeto que já existe começa por `00-diagnosticar` (no lugar do `00-planejar`), pula o `03-desenvolver`, segue o roteiro que ele recomendar e termina na `14-revisao-geral`.

Comandos a qualquer momento: `rodar`, `build`, `nova-feature`, `limpeza` e `projeto-limpo`. As skills `testes`, `dependencias` e `banco` não são comandos: são os padrões da tabela acima, que você carrega sozinho.

Regras completas do fluxo: `.claude/lz/FLUXO.md`.
<!-- lz:fim -->
