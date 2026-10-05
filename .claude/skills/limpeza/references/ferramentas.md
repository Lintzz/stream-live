# Guia: ferramenta de código morto por stack

Consultado pela skill `limpeza`. Rode a ferramenta e trabalhe a partir da saída dela — não saia lendo arquivo por arquivo.

## JavaScript e TypeScript — Knip

É a escolha, e substitui o que antes se fazia com três ferramentas. Acha em uma passada: **arquivos** não alcançáveis, **exports** não importados, **tipos** e membros de enum não usados, **dependências** instaladas e não usadas, dependências **usadas e não declaradas**, e binários não referenciados.

```
npx knip
```

Traz mais de 150 plugins que reconhecem o projeto sozinhos — Next, Vite, Vitest, Jest, Playwright, Storybook, Svelte, Astro, Remix, ESLint, GitHub Actions — então em projeto comum ele funciona sem configuração.

Modos que valem conhecer:

- `npx knip --production` — analisa só o caminho de produção, ignorando teste e ferramenta de desenvolvimento. **É este modo que revela o que existe só para o teste usar** — exatamente o caso da forma 5 de falso positivo. Compare as duas saídas: o que aparece só aqui é usado apenas por teste.
- `npx knip --strict` — mais exigente na separação entre `dependencies` e `devDependencies`.
- `npx knip --reporter json` — quando quiser processar a saída.

**Configure, não silencie.** Quando o Knip aponta algo inesperado, ele está certo sobre o grafo dele: não chegou lá partindo de um arquivo de entrada. O certo é ensinar o projeto a ele — declarar os pontos de entrada de verdade em `knip.json` (`entry`, `project`) — e não pôr `ignore`. As chaves `ignoreDependencies`, `ignoreUnresolved` e `ignoreBinaries` existem, mas são último recurso: `ignore` posto cedo esconde justamente o achado que importava.

Para manter um export de propósito (API pública de biblioteca), marque com anotação JSDoc no próprio export, que é mais honesto do que uma regra global.

O que o Knip **não** faz: CSS morto, imagem órfã, coluna de banco, chave de tradução, rota sem link. Esses ficam com as seções abaixo e com `grep`.

Substituídos por ele: `ts-prune` (arquivado em favor do Knip), `depcheck` e `unimported` (cobrem só um pedaço). Não instale os três.

## Complementos de JS que pegam outra camada

- **ESLint** com `no-unused-vars` ou `@typescript-eslint/no-unused-vars`: variável e import não usados **dentro** do arquivo. Barato, roda sempre, mas não vê nada entre arquivos.
- **`tsc --noUnusedLocals --noUnusedParameters`**: a mesma camada, pelo compilador.
- **madge**: `npx madge --orphans src/` lista módulos que ninguém importa e `--circular` acha dependência circular. Vista de grafo, útil quando o Knip aponta muita coisa e você quer entender a topologia.
- **Analisador de bundle** (`rollup-plugin-visualizer`, `webpack-bundle-analyzer`): mostra o que de fato **foi para o navegador**. Biblioteca pesada que ninguém usa aparece aqui como peso, e isso liga com a auditoria `11-performance`.
- **Aba Coverage do DevTools**: com a página aberta, mostra CSS e JS carregados e não executados. É a única forma honesta de achar CSS morto em página com comportamento dinâmico.

## CSS

- **Tailwind** já remove o que não é usado na build; CSS morto ali é classe montada por string, que o Tailwind também não vê — confira o `safelist`.
- **CSS escrito à mão**: `@fullhuman/postcss-purgecss` em modo relatório, sem aplicar, e confira a lista à mão. Purge automático apaga estado que só aparece em erro ou em hover.
- **Token de design órfão**: `grep` do nome da variável CSS em todo o projeto.

## Python

```
vulture .                 # funcao, classe, variavel e import nao usados
ruff check .              # F401 import nao usado, F841 variavel atribuida e nao usada
deptry .                  # dependencia instalada e nao usada, e usada e nao declarada
```

O `vulture` trabalha com confiança percentual e erra em decorador, framework e chamada dinâmica — trate a saída dele como suspeita, não como veredito. `vulture --min-confidence 80` reduz o ruído.

## .NET

O compilador e os analisadores do Roslyn já fazem boa parte:

```
dotnet build -warnaserror
dotnet format --verify-no-changes
```

Regras que importam, ativadas no `.editorconfig`: `IDE0051` (membro privado não usado), `IDE0052` (campo privado só escrito), `IDE0005` (`using` desnecessário), e os avisos `CS0169` e `CS0414` do próprio compilador. Membro `public` não é apontado — do ponto de vista do compilador, alguém de fora pode usar; aí vale o `grep`.

## Go

```
go vet ./...
staticcheck ./...                                    # U1000: nao usado
go run golang.org/x/tools/cmd/deadcode@latest ./...  # alcancabilidade a partir do main
```

O `deadcode` é o mais próximo do que o Knip faz em JS: parte dos pontos de entrada e diz o que não é alcançável.

## Dart e Flutter

```
dart analyze
dart run dart_code_metrics:metrics check-unused-files lib
dart run dart_code_metrics:metrics check-unused-code lib
```

## Sem ferramenta: o que `grep` resolve

Vale para qualquer stack, e é o que confirma todo achado antes de propor:

```bash
# o nome aparece em algum lugar, em qualquer tipo de arquivo?
grep -rn --exclude-dir=node_modules --exclude-dir=.git "nomeDaCoisa" .

# imagem ou fonte sem nenhuma referencia
for f in public/img/*; do n=$(basename "$f"); \
  grep -rqn --exclude-dir=node_modules "$n" . || echo "orfa: $f"; done

# variavel de ambiente declarada no exemplo e ausente do codigo
grep -o '^[A-Z_]*' .env.example | while read v; do \
  grep -rqn --exclude-dir=node_modules "$v" src/ || echo "no exemplo e nao usada: $v"; done

# blocos de codigo comentado
grep -rn --include=*.{ts,tsx,js,jsx} -E "^\s*//\s*(const|function|import|if|return|export)" src/
```

E o `git log` para a data: `git log -1 --format=%ci -- caminho/do/arquivo`. Arquivo criado há poucos dias é obra em andamento, não sobra.
