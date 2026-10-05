---
name: 03-desenvolver
description: "Constroi o projeto a partir do PLANO.md e do design em HTML (ou das telas descritas no PLANO.md, quando o projeto nasceu so da ideia), etapa por etapa do roteiro, seguindo o CONVENCOES.md e os padroes de banco e interface, com um commit no padrao Conventional Commits por mudanca e uma checagem de que o projeto roda ao fim de cada etapa. Use depois de 00-planejar, 01-ambiente e 02-configurar, quando o usuario pedir para desenvolver, construir, implementar ou fazer o projeto."
---

# Desenvolver o projeto

Leia primeiro `.claude/lz/FLUXO.md`. Esta é a etapa em que o código é escrito — as anteriores decidiram **o quê** e **como**; as seguintes conferem.

## 0. Confira o que já foi decidido

Leia, nesta ordem: `PLANO.md`, `CONVENCOES.md`, `auditorias/contexto.md` e o HTML do design — ou, se o projeto nasceu só da ideia, as seções **Escopo**, **Telas** e **Dados** do `PLANO.md`, que fazem o papel do design.

- **Sem `PLANO.md`:** pare e recomende rodar `00-planejar`. Construir sem plano é justamente o que este fluxo evita.
- **Sem `CONVENCOES.md` ou sem metadados e Git configurados:** recomende `02-configurar` antes. Dá para seguir se o usuário quiser, mas avise que a estrutura de pastas e os commits vão ser decididos no improviso.
- **Sem registro do `01-ambiente`:** siga, mas use os MCPs que estiverem disponíveis — em especial o de documentação, para escrever código com a API da versão instalada.

Mostre o roteiro do `PLANO.md` e pergunte como seguir:

1. **Uma etapa por vez, parando para eu ver** (recomendado) — ao fim de cada etapa, mostro o que foi feito e rodo o projeto para você conferir.
2. **Todas as etapas seguidas** — só paro se algo travar ou exigir decisão.

## 1. Em cada etapa

**Antes de escrever**
- Diga o que a etapa entrega e quais arquivos vai criar ou alterar.
- Se a etapa exige algo que não está no plano (serviço novo, biblioteca pesada, mudança de stack), **pare e pergunte**. Não amplie o escopo por conta própria.

**Ao escrever**
- Estrutura de pastas, nomes, idioma e tokens de design: exatamente como no `CONVENCOES.md`.
- **Fidelidade ao design:** o HTML do design é a referência visual. Converta para componentes sem "melhorar" layout, cores ou textos por conta própria. Se algo no design não funciona (estouro no celular, contraste ruim), aponte e pergunte.
- **Sem design (projeto da ideia):** as telas do `PLANO.md` dizem o que cada uma mostra, o que a pessoa faz e para onde vai. O visual é seu: simples, consistente, com os tokens do `CONVENCOES.md` desde a primeira tela. Construa **só o que está na v1** — o que está em "Depois" ou "Não vai ter" não entra, nem "já que estou aqui". Ao fim da primeira tela, mostre e pergunte se o visual está no caminho antes de replicar nas outras.
- **Padrões carregados por você, sem o usuário pedir:** biblioteca ou framework → `.claude/lz/DOCUMENTACAO.md`; banco de dados → `.claude/skills/banco/SKILL.md`; telas, listas, formulários e botões → `.claude/skills/06-interface/SKILL.md`; serviço externo novo → `.claude/skills/01-ambiente/SKILL.md` em modo preparar; dependência nova ou aviso de vulnerabilidade → `.claude/skills/dependencias/SKILL.md`. Esses padrões entram enquanto o código é escrito, não depois.
- **Documentação antes da primeira linha com cada biblioteca:** na primeira vez que este projeto usa uma biblioteca, confirme a API da versão instalada. Depois disso, siga o padrão que já ficou no código e no `CONVENCOES.md` — não consulte de novo à toa. Se uma tentativa falhar com erro de API, consulte antes da segunda.
- Segredos só em variável de ambiente, sempre com o `.env.example` atualizado. Nunca chave de verdade no código, nem temporariamente.
- **Conteúdo que falta** (texto do cliente, foto, número de telefone, preço): use um marcador claro, `TODO(conteudo): <o que falta>`, e siga. Não invente dado do cliente.
- Nenhuma dependência nova sem dizer qual, por quê e quanto pesa. Depois de instalar, aplique a skill `dependencias` naquele pacote: aviso da instalação classificado, e checagem de nome e de script de `postinstall` antes de seguir escrevendo em cima dele.

**Ao terminar a etapa**
- **Commits:** um por mudança lógica, no padrão Conventional Commits, em português (`feat: adiciona seção de depoimentos`, `fix: corrige menu no celular`). Nunca junte coisas de tipos diferentes no mesmo commit. **Não faça push.**
- **Verifique.** Se o projeto já tem verificação automática (seção Testes do `CONVENCOES.md`), rode-a. Se não tem, rode o projeto como a skill `rodar` faz: sobe sem erro, a página ou tela da etapa abre, o console não mostra erro. Se quebrou algo que funcionava na etapa anterior, conserte antes de seguir — e a correção ganha primeiro a verificação que falha.
- **Fluxo terminado ganha caso na verificação**, no nível que o projeto adotou.
- **A verificação nasce aqui, sem ninguém pedir.** Na primeira etapa com regra de negócio, dado ou login, carregue `.claude/skills/testes/SKILL.md` em modo montar e faça parte da etapa: diga o nível, a ferramenta e os primeiros casos no "antes de escrever", monte junto com o código e grave os comandos na seção Testes do `CONVENCOES.md`. Projeto de nível 1 (landing page) não monta suíte — a checagem é a do `rodar`. Depois disso os casos só vão crescendo junto com o código.
- Marque a etapa como concluída no `PLANO.md` (`- [x]`) e acrescente ao `auditorias/contexto.md`, em "O que existe hoje", o que passou a existir.
- No modo "uma por vez", mostre o resumo e **espere** antes da próxima.

## 2. Quando algo travar

- **Falta decisão:** pergunte, com opções e recomendação, e registre a resposta em `auditorias/contexto.md`, seção Decisões.
- **Falta acesso ou credencial:** siga com o que der, deixe a integração preparada e diga exatamente o que falta para ligar.
- **Erro que não se resolve em duas tentativas:** pare, mostre o erro, o que já tentou e a hipótese mais provável. Não saia reescrevendo partes que funcionavam. Antes da segunda tentativa, confirme a API na documentação (`.claude/lz/DOCUMENTACAO.md`): tentar de novo de memória costuma repetir o mesmo erro com outra roupa.

## 3. Ao concluir o roteiro

1. Rode o projeto inteiro uma última vez, com a verificação completa.
2. Liste todos os `TODO(conteudo)` que ficaram, agrupados por página ou tela — é a lista do que pedir ao cliente.
3. Diga o que ficou fora do plano e foi sugerido durante o caminho.
4. Recomende a próxima etapa: `04-git`, **antes do primeiro push** para o repositório remoto.

## Registro

Salve `auditorias/03-desenvolvimento.md` com: data, etapas concluídas, decisões tomadas no caminho, dependências adicionadas, a lista de `TODO(conteudo)` pendentes e o que ficou fora do plano. O `12-pre-lancamento` usa essa lista para conferir que nenhum marcador sobrou.
