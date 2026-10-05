# Detecção do tipo de projeto

Usado por `02-configurar`, `rodar` e `build`. O objetivo é descobrir, sem perguntar, **que tipo de projeto é, com que ferramenta ele roda, como gera build e onde a versão mora**.

## Ordem de detecção

1. **Leia `auditorias/contexto.md`, seção "Como rodar e gerar build".** Se já estiver lá e os arquivos que sustentam a detecção não mudaram, use direto — não detecte de novo.
2. **Gerenciador de pacotes** pelo lockfile, nunca por suposição: `pnpm-lock.yaml` → pnpm · `yarn.lock` → yarn · `bun.lockb`/`bun.lock` → bun · `package-lock.json` → npm. Sem lockfile, npm. Usar o gerenciador errado gera um segundo lockfile e versões diferentes das testadas.
3. **Tipo do projeto** pelos sinais da tabela abaixo. Quando houver mais de um sinal (monorepo, ou web + mobile), liste as partes e pergunte qual.
4. **Scripts do `package.json`** têm prioridade sobre o comando padrão da ferramenta: se o projeto definiu `dev`, `start` ou `build`, é aquele que se usa.
5. **Grave o resultado** no `auditorias/contexto.md` (seção abaixo), para a próxima vez não precisar detectar.

## Sinais por tipo

| Sinal no projeto | Tipo | Rodar em desenvolvimento | Gerar build | Saída da ferramenta |
|---|---|---|---|---|
| `index.html` sem `package.json` | Site estático | `npx serve .` ou `python -m http.server` | não há build | a própria pasta |
| `vite` nas dependências | Web com Vite | `npm run dev` | `npm run build` | `dist/` |
| `next` | Next.js | `npm run dev` | `npm run build` | `.next/` ou `out/` com export estático |
| `astro`, `nuxt`, `@sveltejs/kit` | Web com framework | `npm run dev` | `npm run build` | `dist/`, `.output/` ou `build/` |
| `expo` nas dependências ou `app.json` com chave `expo` | App Expo | ver seção Expo | ver seção Android | ver seção Android |
| `react-native` sem `expo`, pasta `android/` | React Native puro | `npx react-native start` + `npx react-native run-android` | Gradle | `android/app/build/outputs/` |
| `capacitor.config.*` | Capacitor/Ionic | build web + `npx cap run android` | `npx cap sync android` + Gradle | `android/app/build/outputs/` |
| `electron` + `electron-builder` | Electron | script `dev` ou `start` | `electron-builder` | `dist/` ou `directories.output` |
| `electron` + `@electron-forge/cli` | Electron (Forge) | `npm start` | `npm run make` | `out/make/` |
| `electron-vite` | Electron com Vite | `npm run dev` | `npm run build` + empacotador | conforme config |
| `src-tauri/` ou `tauri.conf.json` | Tauri | `npm run tauri dev` | `npm run tauri build` | `src-tauri/target/release/bundle/` |
| `*.csproj` ou `*.sln` | .NET / C# | `dotnet run` (ou `dotnet watch`) | `dotnet publish -c Release` | `bin/Release/<framework>/publish/` |
| `pubspec.yaml` | Flutter | `flutter run` | `flutter build apk` / `windows` | `build/app/outputs/` |
| `pyproject.toml` / `requirements.txt` | Python | conforme o projeto (script, `uvicorn`, `flask run`) | `pyinstaller` se for executável | `dist/` |

Detalhes de comando mudam entre versões. Na dúvida, confirme na documentação oficial da versão instalada (MCP de documentação, se houver) em vez de confiar na tabela.

## Expo: rodar sem compilar ou compilar

A diferença entre as duas formas é a mais importante para não perder tempo:

- **Sem código nativo próprio** (sem `expo-dev-client`, sem pasta `android/`, só bibliotecas suportadas pelo Expo Go): `npx expo start --android` abre no Expo Go do emulador **sem compilar nada**. É o modo de teste rápido.
- **Com `expo-dev-client` e um development build já instalado no emulador**: `npx expo start --dev-client` e abrir o app instalado. Também não compila.
- **Precisa compilar** (`npx expo run:android`) só quando: é a primeira vez; mudou dependência com código nativo; mudou `plugins`, permissões ou configuração nativa no `app.json`; ou o app instalado no emulador não abre mais o bundle. Compare o `package.json` e o `app.json` com o momento do último `run:android` para decidir.

Diga sempre qual das três foi escolhida e por quê.

## Emulador Android

Antes de rodar um app Android:

1. `adb devices` — há emulador ou aparelho conectado e no estado `device`?
2. Se não houver: `emulator -list-avds` para listar os emuladores; se houver um só, inicie com `emulator -avd <nome>`; se houver vários, use o registrado no contexto ou pergunte.
3. Espere o boot terminar: `adb wait-for-device` e depois `adb shell getprop sys.boot_completed` até retornar `1`.
4. Se `adb` ou `emulator` não forem encontrados, verifique `ANDROID_HOME` (ou `ANDROID_SDK_ROOT`) e o `PATH` para `platform-tools` e `emulator`. Se o Gradle reclamar de Java, confira `JAVA_HOME` e a versão do JDK exigida pela versão do React Native/Expo em uso.

No Windows, o Gradle é `gradlew.bat`; no terminal bash do Windows, `./gradlew` também funciona.

## Onde a versão mora

| Tipo | Arquivo | Campos |
|---|---|---|
| Qualquer projeto Node | `package.json` | `version` |
| Expo | `app.json` / `app.config.*` | `expo.version`, `expo.android.versionCode`, `expo.ios.buildNumber` |
| React Native puro / Capacitor | `android/app/build.gradle` | `versionName`, `versionCode` |
| Electron | `package.json` | `version` (o instalador usa essa) |
| Tauri | `src-tauri/tauri.conf.json` e `Cargo.toml` | `version` |
| .NET | `.csproj` | `<Version>` |
| Flutter | `pubspec.yaml` | `version: 1.2.3+45` (nome + código) |

Quando a versão aparece em mais de um lugar, **todos têm que andar juntos**. Versão divergente entre `package.json` e `app.json` é a origem de "o app diz 1.0.0 mas a loja diz 1.2.0".

## O que gravar no contexto

```markdown
## Como rodar e gerar build
- Tipo: (ex.: App Expo com dev client)
- Gerenciador: (npm / pnpm / yarn / bun)
- Rodar (teste rápido): (comando exato)
- Rodar (compilando): (comando exato, se diferente)
- Emulador padrão: (nome do AVD)
- Build: (comando exato) → saída em (pasta)
- Versão mora em: (arquivos e campos)
- Dependências conferidas em: AAAA-MM-DD — (a linha do resultado; o `rodar` refaz uma vez por dia)
- Detectado em: AAAA-MM-DD
```
