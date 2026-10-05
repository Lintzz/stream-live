# Guia: ferramenta de teste por stack

Consultado pela skill `testes`. Escolha **uma** ferramenta por camada e não some outra sem motivo. Antes de escrever o primeiro caso, confirme a API da versão instalada pelo padrão `.claude/lz/DOCUMENTACAO.md` — as três ferramentas abaixo mudam de assinatura entre majors.

## Regras que valem em qualquer stack

- **Localize por papel e texto, não por CSS.** `getByRole("button", { name: "Salvar" })` sobrevive a mudança de classe; `.btn-primary > span` quebra na primeira troca de estilo. Bônus: localizar por papel acessível só funciona se o elemento tiver papel acessível — o teste passa a cobrar acessibilidade de graça.
- **Nunca `sleep`.** Espere por condição: elemento visível, texto presente, requisição concluída. Espera por tempo é a origem de quase todo teste instável.
- **Data e hora fixas** nos testes que dependem delas. Teste que passa hoje e falha dia 1º do mês não é teste.
- **Um comando por camada** no `package.json`: `"smoke"` para a verificação rápida, `"test"` para tudo. Quem chega no projeto não deve precisar adivinhar.
- **Artefato de teste não vai para o Git**: `test-results/`, `playwright-report/`, capturas de tela, banco temporário, vídeo, trace.

## Site estático ou landing page — nível 1

Não monte suíte. A verificação é:

1. A build passa.
2. A página abre e o console fica limpo.
3. O formulário chega ao destino (teste manual uma vez, com registro do que chegou).
4. Nenhum link quebrado.

Se houver formulário ou alguma interação em JS que vale proteger, um único arquivo Playwright de 15 linhas já cobre — sem instalar mais nada além do Playwright.

## Web com framework (Next, Vite, React, Svelte) — nível 2

| Camada | Ferramenta | Comando |
|---|---|---|
| Regras puras | **Vitest** (projeto com Vite) ou **Jest** | `npx vitest run` |
| Componente | Vitest + Testing Library | idem |
| Caminho fim-a-fim | **Playwright** | `npx playwright test` |

```
npm init playwright@latest
```

Dois ajustes que valem muito no `playwright.config.ts`:

- **`webServer`** — o Playwright sobe o dev server sozinho e espera a porta responder. Sem isso, todo mundo esquece de subir o projeto antes.
- **`trace: "on-first-retry"`** — quando um teste falha, você abre o trace e vê a tela, a rede e o console no instante da falha. É o que substitui `console.log` espalhado.

Ferramenta de apoio: `npx playwright codegen <url>` grava o clique-a-clique e escreve o esqueleto do teste. Serve para rascunhar; o caso final precisa de asserção de verdade, não só a navegação gravada.

## Electron — nível 3

O Playwright tem API própria para Electron, e a documentação do Electron recomenda esse caminho. **Spectron está morto** (arquivado em 2022) — não use.

```js
const { _electron: electron } = require("playwright");

const app = await electron.launch({
  args: ["out/main/index.js"],
  env: { ...process.env, MEU_APP_LOCAL_ONLY: "1", MEU_APP_USER_DATA: pastaTemporaria },
});

const janela = await app.firstWindow();
await janela.waitForLoadState("domcontentloaded");

// mesma API que as telas usam
const criado = await janela.evaluate(() => window.api.eventos.create({ titulo: "teste" }));

await janela.screenshot({ path: "capturas/inicio.png" });
await app.close();
```

O que se ganha em relação a um script próprio com `executeJavaScript` e `capturePage`: espera automática, localizadores por papel, captura de tela, vídeo e trace sem código, e o mesmo vocabulário dos testes de web.

**Script próprio que já funciona não se troca por isso.** A ideia é a mesma e a troca custa tempo sem entregar comportamento novo. Use o Playwright em projeto novo, ou quando o script próprio começar a dar trabalho.

Em qualquer das duas formas, o essencial é o isolamento: **pasta de dados do usuário apontando para um temporário** (`app.setPath("userData", ...)` via variável de ambiente lida no main) e modo só local, sem login e sem sincronização.

Capturas de tela merecem um script separado do smoke: ele abre o app com dados de exemplo, passa pelas telas e salva um PNG de cada. É assim que as telas podem ser conferidas de verdade, olhando — e não deduzindo pelo código.

## Expo e React Native — nível 2 ou 3

| Camada | Ferramenta |
|---|---|
| Regras e componentes | **Jest** + `@testing-library/react-native` (o preset do Expo já vem pronto) |
| Fluxo no emulador | **Maestro** — fluxos em YAML, tolerante a tempo de animação, instalação leve |
| Alternativa robusta | **Detox** — mais poderoso, muito mais configuração; só se o app justificar |

```yaml
# .maestro/login.yaml
appId: com.lintz.meuapp
---
- launchApp:
    clearState: true
- tapOn: "Entrar"
- inputText: "teste@exemplo.com"
- tapOn: "Continuar"
- assertVisible: "Meus eventos"
```

```
maestro test .maestro/login.yaml
```

Duas armadilhas: o Maestro precisa do app **instalado** no emulador (dev build ou release, não o Expo Go genérico), e a versão do SDK do Expo manda na compatibilidade das bibliotecas de teste — confirme antes de instalar.

## Qualquer app Android no emulador — o smoke mínimo por `adb`

Responde "dá para testar no emulador?" com o mínimo de peça móvel. Não cobre fluxo, mas pega o que mais acontece: o app que não abre, e o que abre e morre.

```bash
adb install -r app-release.apk
adb logcat -c                                   # limpa o log antes
adb shell am start -n com.pacote/.MainActivity
sleep 4
adb shell pidof com.pacote                      # vazio = morreu na abertura
adb logcat -d | grep -E "FATAL|AndroidRuntime|ANR"
adb exec-out screencap -p > tela.png            # e olhe a imagem
```

Para uma varredura bruta atrás de crash, `adb shell monkey -p com.pacote -v 300` manda 300 eventos aleatórios. Não é teste de comportamento: é caça a travamento.

A skill `rodar` já sabe iniciar o emulador e esperar o boot — aproveite, não reescreva.

## .NET, WPF e MAUI

| Camada | Ferramenta | Comando |
|---|---|---|
| Regras | **xUnit** ou NUnit | `dotnet test` |
| Compilação como portão | o próprio compilador | `dotnet build -warnaserror` |
| UI do WPF | **FlaUI** | via `dotnet test` |

WinAppDriver está praticamente parado; FlaUI é a escolha prática para automatizar janela de WPF. Em MAUI, o caminho mais barato continua sendo teste de regra em xUnit mais um smoke que abre o app e confere que a janela principal aparece.

`dotnet build -warnaserror` é o portão mais barato que existe em .NET: transforma aviso em erro e pega muita coisa antes de qualquer teste.

## Python

**pytest**, sem framework adicional. `pytest -q` na verificação rápida. Para API, `pytest` + `httpx`; para script de dados, teste a função pura com entrada conhecida e saída esperada.

## Onde cada coisa fica

```
tests/            # unidade e integração (ou __tests__/, conforme o CONVENCOES.md)
e2e/              # casos Playwright
.maestro/         # fluxos Maestro
scripts/          # smoke próprio e captura de telas
capturas/         # PNGs gerados — fora do Git
```

Se o projeto já tem outra organização, siga a dele. A tabela acima vale para projeto novo.
