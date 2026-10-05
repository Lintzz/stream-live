---
name: 02-configurar
description: "Configuracao inicial do projeto: preenche os dados do desenvolvedor e do projeto (nome, descricao, autor, repositorio Git, licenca, id do app), prepara o Git e o repositorio remoto, e define a politica de versionamento - em que versao comeca, o que faz subir cada numero, uso de beta, tags, changelog e releases no GitHub. Use no inicio de um projeto, ou quando o usuario quiser configurar metadados, autor, repositorio, descricao ou decidir como o projeto vai ser versionado."
---

# Configuração do projeto

Leia primeiro `.claude/lz/FLUXO.md`, `.claude/lz/DETECCAO.md` e `.claude/lz/VERSIONAMENTO.md`.

Rode uma vez, no começo — depois do `00-planejar` e do `01-ambiente` (com o MCP do GitHub conectado, dá para criar o repositório daqui), antes do `03-desenvolver`. Ou direto, em projeto próprio que não passou pelo planejamento. Pode rodar de novo quando algo mudar (repositório novo, projeto passou para o cliente).

Ao fim, o projeto sabe **quem fez, de quem é, onde está o código e como vai ser versionado** — e a skill `build` segue isso sem perguntar de novo.

## Em projeto existente

Se o projeto já existia antes do kit (há `auditorias/00-diagnostico.md`, ou código e histórico de Git anteriores), esta skill **completa, não recomeça**:

- **Versão:** mantenha a atual. Se ela diverge entre arquivos, proponha unificar pela maior. Se não há versão nenhuma, proponha a que descreve o estado real: `1.0.0` se já está em uso, `0.x` se ainda não foi lançado.
- **Git:** não rode `git init` e não reescreva histórico. Se os commits antigos não seguem Conventional Commits, tudo bem: o padrão vale **daqui para a frente**. Se não há tag, proponha criar `vX.Y.Z` no commit atual, para a `build` ter um ponto de partida.
- **Id do app e nome do pacote:** se o app já foi publicado em loja, **não mude** — nem para corrigir o formato.
- **Metadados:** preencha só o que falta, e mostre a diferença antes.
- **`CHANGELOG.md`:** se não existir, comece com a versão atual e a linha "Estado do projeto ao adotar o kit".
- As perguntas de versionamento continuam valendo, exceto a da versão inicial.
- **Testes:** se já existe verificação, descreva na seção Testes do `CONVENCOES.md` e rode para ver se passa. Se não existe nada e o nível do projeto é 2 ou 3, carregue `.claude/skills/testes/SKILL.md` em modo montar e proponha, junto com o resto desta configuração, a **verificação rápida**: sobe o projeto e percorre o caminho principal. É o mínimo para as auditorias e a `nova-feature` saberem se quebraram algo. A suíte cresce depois, caso a caso.

## 1. Perfil do desenvolvedor

Seus dados são os mesmos em todo projeto, então ficam fora do projeto, em `~/.lz/perfil.json` (Windows: `%USERPROFILE%\.lz\perfil.json`).

- Se existir, leia e use.
- Se não existir, crie a partir de `.claude/skills/02-configurar/references/perfil-modelo.json`, perguntando só os campos vazios. Deixe claro que o e-mail vai aparecer publicamente no `package.json` e nos commits; se não quiser expor, use o e-mail de privacidade do GitHub (`<id>+<usuario>@users.noreply.github.com`).
- Nunca guarde senha, token ou chave nesse arquivo.

## 2. Perguntas do projeto

Leia `auditorias/contexto.md` e `PLANO.md` primeiro e **não pergunte o que já estiver lá**. Detecte o tipo do projeto pelo `DETECCAO.md`. Depois pergunte, de uma vez, com opções e recomendação marcada:

**Identidade**
1. É projeto seu ou de cliente? Se de cliente: nome da empresa ou pessoa, e quem fica com os direitos.
2. Nome do produto como aparece para o usuário, e uma descrição de uma frase.
3. Id do app em domínio invertido (só para app instalável). Sugira `io.github.<usuario>.<app>` para projeto seu sem domínio, ou o domínio do cliente. **Avise que no Android e na Microsoft Store o id não muda depois de publicado.**

**Repositório**
4. Já existe repositório remoto? Se não: quer que eu crie (público ou privado, na sua conta ou na do cliente) ou você cria e me passa a URL? Recomende **privado** para projeto de cliente.

**Versionamento** — explique cada opção em uma linha, com a recomendação do `VERSIONAMENTO.md`:
5. **Versão inicial:** `0.1.0` (construído aos poucos, chega a `1.0.0` no lançamento) ou `1.0.0` (entregue de uma vez)?
6. **O que conta como `1.0.0`** neste projeto: a primeira entrega ao cliente, a publicação na loja, o site no domínio definitivo?
7. **Betas** (`-beta.1`) para testadores antes da versão final: sim ou não?
8. **Ao gerar build de entrega:** só local, ou já perguntar se quer enviar para o GitHub (commit, tag, push) e criar uma **Release** com o arquivo anexado?

Se o usuário não responder alguma, siga a recomendação e registre como decisão assumida.

## 3. Aplicar

Mostre o que vai mudar em cada arquivo e peça aprovação antes. Depois:

**Metadados.** Preencha os campos da plataforma detectada seguindo `references/metadados.md`: autor a partir do perfil; nome, descrição, dono, id do app e licença a partir das respostas; `private: true` em projeto que não é pacote publicado; `repository` e `homepage` quando fizer sentido.

**Versão inicial.** Coloque a versão escolhida em **todos** os lugares onde ela mora (tabela do `DETECCAO.md`). Em app Android, `versionCode` começa em `1`.

**Git.**
- Se não houver repositório, `git init` com a branch principal `main`.
- Confira o `.gitignore` para a stack (dependências, saídas de build, `builds/`, `.env*` exceto `.env.example`, `*.keystore`, `*.jks`, `*.pfx`). Se estiver incompleto, complete.
- Configure o remoto com a URL informada. Se o usuário pediu para criar o repositório e houver `gh` (GitHub CLI) autenticado ou MCP do GitHub, crie com a visibilidade escolhida; senão, diga o passo a passo.
- Confira que o autor dos commits (`git config user.name` e `user.email`) corresponde ao perfil.
- **Trava de commits:** se existe `.githooks/commit-msg` (instalado pelo kit), ative com `git config core.hooksPath .githooks` — ou, se o projeto usa Husky, confira que `.husky/commit-msg` chama esse script. A partir daí, commit fora do padrão Conventional Commits é recusado. Essa configuração é local: quem clonar o repositório precisa rodar o comando de novo (ou reinstalar o kit).
- **Não faça push nesta etapa** sem pedido explícito.

**Arquivos de apoio.**
- `CHANGELOG.md` no formato *Keep a Changelog*, com a seção `## [Não lançado]` pronta para receber as mudanças.
- No `CONVENCOES.md`, a seção **Versionamento** abaixo. Se o arquivo não existir, crie só com essa seção e a de commits.

```markdown
## Versionamento
- Padrão: SemVer (MAIOR.MENOR.CORREÇÃO)
- Versão inicial: 0.1.0
- A 1.0.0 será: (ex.: a primeira entrega ao cliente)
- Correção: (exemplos concretos deste projeto)
- Menor: (exemplos concretos deste projeto)
- Maior: (exemplos concretos deste projeto, ou "improvável neste projeto")
- Betas: sim/não
- Build de teste não sobe versão; build de entrega sobe
- Tag: vX.Y.Z · Changelog: CHANGELOG.md
- Ao gerar build de entrega: (só local / perguntar sobre push e Release no GitHub)

## Commits
- Conventional Commits: feat:, fix:, docs:, refactor:, chore:, e ! para mudança que quebra

## Testes
- Nível: (1 checagem / 2 unidade + caminho principal / 3 smoke no app compilado)
- Rodar rápido: (comando, ou "ainda não montado")
- Rodar tudo: (comando, ou "ainda não montado")
- Onde ficam:
- Dados de teste: (banco temporário, conta de teste criada e destruída, nunca a base real)
- **Fluxo novo ganha caso na verificação.** Bug corrigido ganha caso que falhava antes.
```

A seção **Testes** entra já na configuração, mesmo com os comandos vazios: é ela que faz o agente da próxima conversa saber que existe essa obrigação. Proponha o nível pelo perfil do projeto (tabela no FLUXO). Em projeto novo não há código ainda: a montagem acontece sozinha no `03-desenvolver`, na primeira etapa com regra de negócio, dado ou login. As duas últimas linhas ficam escritas desde o começo — são elas que sustentam o padrão.

Escreva os exemplos de correção, menor e maior **pensando neste projeto** ("nova página de serviço" → menor, "trocar o número do WhatsApp" → correção), não copie a tabela genérica. São esses exemplos que respondem "de 1.0 para 1.1 ou 1.0.1?" na hora da build.

## 4. Informe

- O que foi preenchido e onde
- A política de versão em três linhas: onde começa, quando vira 1.0.0, como cada tipo de mudança sobe
- Estado do Git: repositório local, remoto configurado, nada enviado ainda

## Registro

No `auditorias/contexto.md`:

- Seção **Identidade**: dono do produto, nome, descrição, id do app, licença, repositório (URL e visibilidade)
- Seção **Como rodar e gerar build**: tipo detectado e versão atual
- Em **Decisões**: a política de versionamento escolhida, com a data
