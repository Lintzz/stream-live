# Versionamento

Usado por `02-configurar` (para definir a política do projeto) e `build` (para aplicar a cada entrega). A política escolhida para cada projeto fica escrita no `CONVENCOES.md`, seção **Versionamento** — ela vale mais do que este guia se os dois divergirem.

## O padrão: SemVer

Versão é `MAIOR.MENOR.CORREÇÃO`, por exemplo `1.4.2`. Ao subir um número, os da direita voltam a zero.

| Sobe | Quando | Exemplo |
|---|---|---|
| **CORREÇÃO** | conserto que não muda o que o usuário faz | `1.4.2 → 1.4.3` |
| **MENOR** | algo novo, e o que existia continua funcionando | `1.4.2 → 1.5.0` |
| **MAIOR** | algo que quebra ou obriga o usuário a mudar | `1.4.2 → 2.0.0` |

### Na prática, para sites e apps

| O que mudou | Sobe |
|---|---|
| Texto errado, imagem trocada, erro de digitação | correção |
| Botão que não funcionava, tela que travava, cálculo errado | correção |
| Ajuste visual pequeno (espaçamento, cor, alinhamento) | correção |
| Página, tela, seção ou funcionalidade nova | menor |
| Campo novo no formulário, filtro novo, opção nova | menor |
| Layout redesenhado, mas o usuário faz as mesmas coisas | menor |
| Melhoria de desempenho perceptível | menor |
| Funcionalidade ou tela **removida** | maior |
| Usuário precisa refazer login, reinstalar ou perde configurações | maior |
| Dados salvos antes deixam de funcionar | maior |
| Mudança numa API que outro sistema usa | maior |

Em site e app de cliente, **maior é raro**. Se toda entrega está virando maior, a classificação está errada.

Várias mudanças numa versão só: vale a **maior** delas. Três correções e uma página nova → menor.

### Antes da 1.0.0

Enquanto o projeto está em construção, a versão começa com zero: `0.1.0`, `0.2.0`, `0.2.1`...

- Funcionalidade nova → sobe o do meio (`0.2.0 → 0.3.0`)
- Correção → sobe o último (`0.3.0 → 0.3.1`)
- Mudança que quebra → também sobe o do meio, não vira 1.0 por isso

**`1.0.0` é um marco, não uma conta:** é a primeira versão que o cliente usa de verdade, que vai ao ar para o público ou que entra na loja. A partir dela, vale a tabela completa.

Não se usa `0.0.x`. O primeiro número de um projeto é `0.1.0`.

### Onde começar

- **Projeto que vai ser construído aos poucos, com entregas parciais** (app, sistema, site com várias etapas): começa em `0.1.0` e chega a `1.0.0` no lançamento.
- **Projeto entregue de uma vez** (landing page pronta em poucos dias): pode nascer direto em `1.0.0` no dia da entrega.
- **Projeto que já existe sem versionamento**: começa pela versão que descreve o estado atual — se já está no ar e em uso, `1.0.0`.

### Pré-lançamento

Para builds que vão para testadores antes da versão final: `1.5.0-beta.1`, `1.5.0-beta.2` e, quando aprovado, `1.5.0`. Use só se o projeto tiver fase de teste com outras pessoas; senão, é complicação à toa.

## Quando **não** subir a versão

A versão sobe quando a build **vai para alguém**: cliente, testador, loja, instalador publicado, deploy de produção.

Não sobe em build para testar no próprio emulador ou computador. Essa build leva a versão atual com sufixo no **nome do arquivo** (`app-1.4.2-dev-a1b2c3d.apk`, com o hash curto do commit), sem mexer em arquivo de versão. Subir a cada teste faz o número inflar sem significar nada.

## Decidir o próximo número

1. **Última versão:** a tag mais recente (`git describe --tags --abbrev=0`) e o valor nos arquivos de versão (tabela "Onde a versão mora" do `DETECCAO.md`). Se divergirem, avise antes de tudo.
2. **O que mudou desde ela:** `git log <última-tag>..HEAD --oneline`; se as mensagens não forem claras, o diff.
3. **Classificar:**
   - Com **Conventional Commits**, é mecânico: `fix:` → correção; `feat:` → menor; `!` depois do tipo (`feat!:`) ou `BREAKING CHANGE:` no corpo → maior. `docs:`, `style:`, `refactor:`, `test:`, `chore:` sozinhos não pedem versão nova.
   - Sem Conventional Commits, leia as mudanças e classifique pela tabela "Na prática", **mostrando o motivo** de cada uma.
   - Aplique a regra "antes da 1.0.0" se for o caso.
4. **Propor e confirmar:** mostre `atual → proposta` com a justificativa em poucas linhas, e peça confirmação. Se o projeto está em `0.x` e parece pronto para o lançamento, pergunte se esta é a hora da `1.0.0`.

## Aplicar

1. Atualize **todos** os lugares onde a versão mora, juntos.
2. **`versionCode` do Android** (e `buildNumber` do iOS): inteiro que **sempre aumenta** e nunca se repete — a loja recusa código já usado. Some 1 ao anterior a cada build distribuída, inclusive betas. Não derive por fórmula do número da versão.
3. **`CHANGELOG.md`** no formato *Keep a Changelog*: seção `## [X.Y.Z] - AAAA-MM-DD`, agrupando em **Adicionado**, **Alterado**, **Corrigido** e **Removido**, escrito para quem usa o produto — não o texto cru dos commits.
4. **Commit e tag** (depois que a build der certo): commit `chore(release): vX.Y.Z` e tag anotada `vX.Y.Z` com o resumo do changelog.

## Conventional Commits

Formato: `tipo: descrição curta no imperativo`.

- `feat: adiciona página de agendamento`
- `fix: corrige máscara do telefone no formulário`
- `docs:`, `style:`, `refactor:`, `test:`, `chore:` para o resto
- `feat!: remove login por senha` — o `!` marca mudança que quebra

Ao escrever commits num projeto que adotou o padrão, siga-o. É isso que permite decidir a próxima versão sem ter que ler o diff.
