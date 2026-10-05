# Metadados por plataforma

Quais campos preencher, onde, e o que cada um significa. Preencha só os campos da plataforma do projeto.

## Regra de quem é quem

Em projeto de **cliente**, separe as duas pessoas:

- **Autor** (`author`, `Authors`) — você, que desenvolveu. Vem do `~/.lz/perfil.json`.
- **Dono do produto** (nome da empresa, `copyright`, `Company`, `publisher`, id do app) — o cliente, conforme combinado. Se o combinado for outro, siga o combinado e registre no contexto.

Em projeto **próprio**, as duas são você.

## Id do app (domínio invertido)

Formato `com.empresa.app`, só minúsculas, letras, números e ponto (no Android, também `_`; sem hífen).

- Cliente com domínio próprio: `br.com.cliente.app` ou `com.cliente.app`
- Você, sem domínio próprio: `io.github.<seu-usuario>.<app>` — é a convenção aceita para quem não tem domínio
- **Não pode mudar depois de publicado** no Android (Google Play) nem na Microsoft Store. Mudar o id significa, para a loja, outro app: perde avaliações, instalações e atualização automática. Confirme antes da primeira build distribuída.

## Node (qualquer projeto com `package.json`)

| Campo | O que colocar |
|---|---|
| `name` | minúsculo, com hífen, sem espaço (`site-clinica-sorriso`) |
| `version` | a versão atual (ver `.claude/lz/VERSIONAMENTO.md`) |
| `description` | uma frase |
| `author` | `{ "name", "email", "url" }` do perfil. E-mail só se você quiser ele público |
| `license` | `UNLICENSED` para projeto de cliente ou fechado; licença aberta (ex.: `MIT`) só se o código for público de propósito |
| `private` | `true` em qualquer projeto que não seja pacote publicado. Impede publicar no npm por engano |
| `repository` | URL do repositório, se fizer sentido expor |
| `homepage` | site do produto |

## Electron (electron-builder)

No `package.json` (chave `build`) ou em `electron-builder.yml`:

| Campo | O que colocar |
|---|---|
| `appId` | id invertido do app |
| `productName` | nome visível, com espaço e acento; vira o nome do instalador e da pasta de instalação |
| `copyright` | `Copyright © AAAA <dono do produto>` |
| `directories.output` | pasta de saída (a skill `build` copia de lá para `builds/`) |
| `win.icon` / `mac.icon` / `linux.icon` | ícone em `.ico`, `.icns` e `.png` do tamanho exigido |
| `nsis` | instalador do Windows: `oneClick`, `allowToChangeInstallationDirectory`, atalhos |
| `artifactName` | padrão do nome do arquivo, com versão: `${productName}-Setup-${version}.${ext}` |

No Electron Forge, os equivalentes ficam em `forge.config.*` (`packagerConfig.name`, `appBundleId`, e os `makers`).

**Assinatura de código no Windows:** instalador sem assinatura mostra o aviso do SmartScreen ("editor desconhecido"). Para uso interno ou teste, tudo bem; para distribuir a público, avise que assinar exige certificado pago. Publicação na Microsoft Store tem assinatura própria da loja.

## Expo (`app.json` ou `app.config.*`)

| Campo | O que colocar |
|---|---|
| `expo.name` | nome visível do app |
| `expo.slug` | identificador curto, minúsculo com hífen |
| `expo.version` | versão SemVer |
| `expo.owner` | conta Expo, se usar EAS |
| `expo.android.package` | id invertido — **imutável depois de publicar** |
| `expo.android.versionCode` | inteiro que sempre aumenta |
| `expo.ios.bundleIdentifier` | id invertido do iOS |
| `expo.ios.buildNumber` | texto que sempre aumenta |
| `expo.icon`, `expo.splash`, `expo.android.adaptiveIcon` | caminhos das imagens |
| `expo.scheme` | esquema de link profundo, se houver |

Se existir pasta `android/` gerada (prebuild), os valores nativos também estão em `android/app/build.gradle`. Mantenha os dois iguais ou regenere com prebuild.

## Android nativo (`android/app/build.gradle`)

`applicationId` (id invertido, imutável), `versionCode` (inteiro que sempre aumenta), `versionName` (SemVer). Nome visível em `res/values/strings.xml` (`app_name`).

## .NET (`.csproj`)

`<Version>`, `<Authors>`, `<Company>`, `<Product>`, `<Copyright>`, `<Description>`, `<RepositoryUrl>`. Em aplicativo Windows empacotado (MSIX), a identidade também está no `Package.appxmanifest`.

## Tauri (`tauri.conf.json`)

`productName`, `version`, `identifier` (id invertido), e em `bundle`: `publisher`, `copyright`, `icon`.

## Flutter (`pubspec.yaml`)

`name`, `description`, `version: 1.2.3+45` (versão + código de build). O id Android fica em `android/app/build.gradle`.

## Segredos de assinatura

Keystore do Android, certificado de assinatura do Windows e senhas associadas:

- **Nunca no repositório.** Arquivo fora do projeto; senhas em variável de ambiente ou em `~/.gradle/gradle.properties`.
- **Backup do keystore em lugar seguro.** No Android, perder a chave de upload complica publicar atualização (com Play App Signing dá para pedir a troca; sem ele, o app fica sem atualização possível).
- Confira que `*.keystore`, `*.jks` e `*.pfx` estão no `.gitignore`.
