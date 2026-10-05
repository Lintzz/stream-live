---
name: dependencias
description: "Trata os avisos de dependencia que aparecem ao instalar pacotes e audita as bibliotecas do projeto: separa aviso de depreciacao de vulnerabilidade real, faz a triagem do npm audit (producao ou dev, direta ou transitiva, caminho usado ou nao), corrige sem quebrar o projeto e registra o que foi aceito e por que. Nao e um comando: carregue sozinho, sem o usuario pedir, sempre que uma instalacao de pacotes reportar vulnerabilidade, uma dependencia nova entrar no projeto ou a checagem diaria da skill rodar encontrar Critica ou Alta em producao. Use tambem quando o usuario falar em npm audit, vulnerabilidade de pacote, dependencia desatualizada ou aviso vermelho na instalacao."
user-invocable: false
---

# Dependências: avisos, vulnerabilidades e atualização

Leia primeiro `.claude/lz/FLUXO.md`: vocabulário de status, severidade, protocolo de perguntas e formato do registro valem aqui.

Esta skill existe porque o momento mais comum é também o mais mal resolvido: a instalação termina dizendo `7 vulnerabilities (2 moderate, 5 high)` e as duas reações habituais estão erradas — ignorar, ou rodar `npm audit fix --force` e quebrar o projeto. O trabalho aqui é **triagem**: decidir, item por item, o que é risco de verdade neste projeto.

**Regra principal: contagem não é veredito.** "5 high" num projeto pode ser zero risco real e noutro ser bloqueio de lançamento. O que decide é se o pacote vai para o navegador ou para o binário do usuário, se o caminho vulnerável é usado e o que o atacante precisa controlar.

## Fase 0: Separe os avisos da instalação

Antes de tudo, classifique o que apareceu. O usuário vê tudo vermelho ou amarelo e trata igual; não é igual:

| Aviso | O que é | Urgência |
|---|---|---|
| `npm warn deprecated <pkg>@x` | o autor parou de manter aquela versão. **Não é vulnerabilidade.** Em geral vem de uma dependência transitiva que você não escolheu | Nenhuma agora. Anote |
| `npm warn EBADENGINE` | o pacote pede outra versão de Node | Resolver: ou sobe o Node, ou usa versão compatível do pacote |
| `peer dependency` não atendida | duas bibliotecas esperam versões diferentes de uma terceira | Resolver antes de seguir — costuma virar erro estranho em tempo de execução |
| `X vulnerabilities (… high, … critical)` | advisories de segurança nas versões instaladas | É o que esta skill tria |
| `npm error` / falha do `postinstall` | a instalação **não** completou | Bloqueia tudo. Resolva antes |

Diga qual é qual em uma linha cada. Nunca chame depreciação de vulnerabilidade.

## Fase 1: Levante o estado real

Descubra o gerenciador pelo lockfile (`DETECCAO.md`) e rode o comando do ecossistema. Rode; não peça para o usuário rodar e colar.

| Ecossistema | Vulnerabilidades | Só produção | Desatualizadas |
|---|---|---|---|
| npm | `npm audit` | `npm audit --omit=dev` | `npm outdated` |
| pnpm | `pnpm audit` | `pnpm audit --prod` | `pnpm outdated` |
| yarn (berry) | `yarn npm audit` | `yarn npm audit --environment production` | `yarn outdated` |
| bun | `bun audit` | — | `bun outdated` |
| Python | `pip-audit` | — | `pip list --outdated` |
| .NET | `dotnet list package --vulnerable --include-transitive` | — | `dotnet list package --outdated` |
| Go | `govulncheck ./...` | — | `go list -m -u all` |
| Dart / Flutter | não há comando oficial: confira os avisos do `pub.dev` | — | `dart pub outdated` |

Se o comando não existir no ambiente (`pip-audit`, `govulncheck`), diga o comando de instalação e siga com o que der.

Rode **sempre as duas visões**: total e só produção. A diferença entre elas é metade da resposta.

Confirme também: existe lockfile e ele está versionado? Sem lockfile, cada instalação traz versões diferentes e a auditoria não vale para o que está publicado.

## Fase 2: Triagem, item por item

Para cada advisory, responda quatro perguntas antes de decidir:

1. **Vai para o usuário ou fica na sua máquina?** Vulnerabilidade em `vite`, `eslint`, `webpack-dev-server`, `@types/*` ou script de build roda só no seu ambiente de desenvolvimento e não vai no site publicado nem no binário. Vulnerabilidade em `next`, `axios`, `express`, `sharp` ou numa biblioteca de UI vai. É a distinção mais importante e é ela que o `--omit=dev` responde.
2. **Direta ou transitiva?** Direta (está no seu `package.json`): você sobe a versão ou troca o pacote. Transitiva (dependência de uma dependência): depende do pai publicar correção — e aí entra `overrides`.
3. **O caminho vulnerável é usado neste projeto?** Leia a descrição do advisory. ReDoS numa função de parsing que o projeto nunca chama, ou falha num modo de configuração que você não usa, é risco teórico. Diga isso — com a evidência de que não é chamado — em vez de listar como se fosse explorável.
4. **O que o atacante precisa controlar?** "Requer entrada controlada pelo atacante" muda de significado se o dado vem de formulário público ou se vem só de um arquivo seu de configuração.

Veredito de cada item, com essas respostas na mão:

- **Corrigir agora** — produção, caminho usado, correção existe em versão compatível
- **Corrigir com cuidado** — a correção só existe em versão major; exige ler o changelog e testar
- **Aceitar e registrar** — dev-only, ou caminho comprovadamente não usado. Fica no registro com motivo e data para rever
- **Sem correção disponível** — não há versão corrigida ainda. Registre, e diga qual é a mitigação possível (bloquear a entrada, trocar o pacote, fixar em versão anterior à introdução da falha)

## Fase 3: Corrigir sem quebrar

Ordem, e nada fora dela:

1. **`npm audit fix`** (sem `--force`) primeiro: ele só sobe dentro do que o semver permite. Rode, e depois **rode o projeto e gere o build**. Correção que quebra a build não é correção.
2. **`npm audit fix --force` não se roda por conta própria.** Ele instala versões major e é exatamente aí que o projeto quebra. Antes: liste quais pacotes pulam de major, o que muda em cada um (leia o changelog — use o MCP de documentação, conforme `.claude/lz/DOCUMENTACAO.md`) e **peça aprovação**. Se aprovado, faça em commit separado, um pacote ou um grupo por vez, rodando o projeto entre cada um.
3. **Transitiva sem pai corrigido:** force a versão com `overrides` (npm), `pnpm.overrides` ou `resolutions` (yarn). Isso é remendo: registre o motivo e a data, e reveja quando o pai atualizar.
4. **Nunca apague o lockfile** nem o `node_modules` como "correção". Isso esconde o problema e troca todas as versões de uma vez.
5. **Um commit por grupo**, no padrão do projeto: `fix(deps): sobe axios para corrigir SSRF` para correção de vulnerabilidade, `chore(deps): atualiza eslint` para atualização de rotina.

Depois de tudo: rode `npm audit` de novo e mostre o antes e depois em números.

## Fase 4: Pacote suspeito

Vulnerabilidade conhecida é o caso fácil. Confira também o que o `audit` não pega:

- **Nome parecido com o certo** (`reactt`, `lodahs`, `axsios`): typosquatting. Confirme cada dependência direta contra o nome oficial da biblioteca.
- **Pacote que talvez não exista de verdade.** Nome sugerido por IA que ninguém publicou — e que alguém pode ter publicado depois, justamente para isso. Se uma dependência direta não tem repositório, documentação e histórico de versões, trate como suspeita.
- **Poucos downloads, publicado há poucos dias, ou mantenedor trocado recentemente** numa dependência que o projeto usa em produção.
- **Script `postinstall`** em pacote que não deveria precisar de um. Leia o que ele roda.
- **Dependência direta que ninguém importa** no código: sobra de teste, e é superfície à toa. Proponha remover.

## Quando esta skill roda

| Momento | O que fazer |
|---|---|
| Logo depois da primeira instalação de pacotes (`01-ambiente`) | Fase 0 e Fase 1, para o projeto não começar com vulnerabilidade herdada. Corrigir agora é mais barato que depois |
| `rodar`, uma vez por dia | Só a contagem de produção e as majors atrasadas, numa linha. Se aparecer Crítica ou Alta em produção fora das "Aceitas", Fases 0 a 2 e proposta de correção, sem aplicar |
| Dependência nova entrando durante o `03-desenvolver` ou a `nova-feature` | Fase 0, Fase 2 e Fase 4 só daquele pacote — não do projeto inteiro |
| `00-diagnosticar` em projeto antigo | Fase 1 completa: projeto parado costuma acumular, e a lista pode ser longa |
| `07-seguranca` | A auditoria completa de dependências é esta skill. O `07` traz o resultado em vez de repetir |
| `12-pre-lancamento` | Confere o corte: nada Crítico nem Alto em dependência de **produção** |
| Antes de um `build` de release | Vale rodar a visão de produção. Binário publicado carrega o que estiver nele |

## Corte para lançamento

- **Crítica ou Alta em dependência de produção** → bloqueia lançamento
- **Crítica ou Alta só em dependência de desenvolvimento** → não bloqueia; fica registrada com o motivo
- **Média ou Baixa** → registrada; corrige quando aquele pacote for tocado
- **Sem correção disponível, em produção** → não bloqueia automaticamente, mas precisa de decisão explícita do usuário, escrita no registro

## Formato do relatório

1. **Avisos da instalação**, classificados pela tabela da Fase 0 (se houver)
2. **Números:** total e só produção, por severidade, e se existe lockfile versionado
3. **Tabela:** `Pacote | Severidade | Direta/Transitiva | Prod/Dev | Falha | Caminho usado? | Veredito`
4. **Corrigir agora** — com o comando exato
5. **Corrigir com cuidado** — quais pulam major e o que muda em cada um
6. **Aceitas** — com motivo e data para rever
7. **Suspeitas** da Fase 4, se houver
8. **Desatualizadas sem vulnerabilidade** — em lista curta, só as majors atrasadas, como manutenção futura, não como pendência

Depois, pare e pergunte o que aplicar.

## Registro

Salve `auditorias/dependencias.md` com: data, gerenciador e comando usado, números antes e depois, o que foi corrigido, o que foi aceito (com motivo e data para rever) e o que ficou sem correção disponível.

A seção **Aceitas** é a mais importante do arquivo: é ela que evita rediscutir o mesmo advisory em cada auditoria. Ao rodar de novo, confira essa lista primeiro — item já aceito e ainda válido não volta como pendência nova; só volta se o contexto mudou (o pacote passou a ir para produção, ou saiu correção).
