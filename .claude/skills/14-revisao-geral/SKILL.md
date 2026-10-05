---
name: 14-revisao-geral
description: "Ultima etapa do roteiro: revisao de conformidade do projeto inteiro, com a varredura de codigo morto junto. Le o contexto, o plano, todos os registros de auditoria e as funcionalidades adicionadas depois, confere por amostragem se o que consta como corrigido continua corrigido, detecta registros desatualizados em relacao ao codigo, procura sobra acumulada (skill limpeza), consolida todas as pendencias e da um veredito geral. Depois dela, se o projeto vai para cliente, vem o projeto-limpo. Use quando o usuario perguntar se esta tudo dentro do padrao, quiser um raio-x do projeto, uma conferencia final ou saber o que ainda falta."
---

# 14 — Revisão geral

Leia primeiro `.claude/lz/FLUXO.md`: ele define a ordem das etapas, o protocolo de perguntas, o arquivo `auditorias/contexto.md`, o vocabulário de status e o formato do registro. As regras de lá valem aqui.

Esta skill não refaz as auditorias. Ela confere se **o que ficou registrado ainda é verdade** e se o projeto, como um todo, está dentro do padrão. É a **última etapa do roteiro**, e a que se roda de novo sempre que quiser saber se ainda está tudo certo. Junto dela vai a varredura de código morto da skill `limpeza`.

Depois dela, o projeto está pronto. Se for para mandar a um cliente, o próximo passo é `/projeto-limpo`.

Você **não altera nenhum arquivo** nesta skill, exceto o registro do final e o roteiro, depois que eu aprovar. A remoção de sobra segue a Fase 4 da `limpeza`, só com aprovação.

## Fase 0: Reúna o histórico

Leia `auditorias/roteiro.md`, `auditorias/contexto.md`, `PLANO.md`, todos os registros em `auditorias/` e tudo em `auditorias/features/`. O roteiro diz o que este projeto se propôs a fazer e o que foi dispensado — etapa marcada como "não se aplica" não conta como pendência.

Roteiro de versão antiga do kit que cita `/14-entrega` ou `/revisao-geral`: troque a linha por `/14-revisao-geral` e, se o projeto for de cliente, anote que a entrega agora é pelo `/projeto-limpo`.

Se a pasta `auditorias/` não existir, diga isso e pare: não há o que revisar. Recomende começar pelo `00-diagnosticar`, que monta o roteiro de auditorias para o projeto.

Monte o **Mapa de etapas**:

| Etapa | Registro existe? | Data | Situação |
|---|---|---|---|

Situação pode ser: **Concluída**, **Concluída com pendências**, **Não rodou** ou **Desatualizada**.

Para detectar desatualização, compare a data do registro com a data da última alteração relevante no código. Use o Git quando houver:

```
git log -1 --format=%cd
git log --since="<data do registro>" --name-only --pretty=format: | sort -u | head -40
```

Se houve alteração depois do registro, marque a etapa como **Desatualizada** e diga **quais arquivos** mudaram desde então. Sem Git, compare pela data de modificação dos arquivos.

## Fase 1: Verificação por amostragem

Para cada registro, pegue os itens marcados como **Corrigido** e confirme no código que continuam corrigidos. Priorize:

- Tudo que foi **Crítico** ou **Alto** em segurança
- Tudo que foi **Bloqueante** em acessibilidade
- Consentimento de cookies antes de analytics e pixel
- Chaves e variáveis de ambiente
- Title, description e canonical das páginas

Se algo consta como corrigido mas o código mostra o contrário, isso é uma **regressão** e é o achado mais importante do relatório. Cite arquivo e linha.

## Fase 2: Consistência entre etapas

Procure contradições que só aparecem olhando o conjunto:

- Funcionalidade em `auditorias/features/` que reabriu uma auditoria e **não foi verificada** contra ela
- Página, formulário ou integração que existe no código e **não aparece** em nenhum registro nem no contexto
- Serviço usado no código que não está na lista de serviços do contexto ou da entrega
- Variável de ambiente usada no código e ausente do `.env.example`
- Dado pessoal coletado que não aparece no mapa de dados da LGPD
- Item que o pré-lançamento tratou como bloqueio e segue pendente
- Decisão registrada como "não vou fazer" que foi feita depois, ou o contrário
- **Suíte de testes apodrecida**: rode-a. Caso pulado com `.only` ou `skip`, asserção comentada, snapshot aceito sem ninguém olhar, ou fluxo que entrou depois e nunca ganhou caso. Suíte verde que cobre menos do que cobria é regressão silenciosa e vale como achado
- Dependência aceita em `auditorias/dependencias.md` cujo motivo **deixou de valer**: o pacote era só de desenvolvimento e agora vai para produção, ou já saiu versão corrigida. Vale rodar a visão de produção do `audit` aqui: advisory novo aparece sem ninguém mexer no projeto, só pelo tempo passando

## Fase 2.5: Sobra acumulada

Carregue `.claude/skills/limpeza/SKILL.md` e rode as Fases 0 a 2 dela: a ferramenta da stack, as sete formas de parecer morto sem estar e a confiança de cada achado. Se já existe `auditorias/limpeza.md`, comece pela seção **decidi manter** e não levante de novo o que está lá.

Aqui é só o relatório. Remover segue a Fase 4 da `limpeza` — um commit por grupo, verificação depois de cada um — e só com a minha aprovação. Coluna de banco nunca sai nesta etapa: vai para o plano de duas etapas da `limpeza`.

## Fase 3: Consolidação

Junte **todas** as pendências de todos os registros, sem repetir, e ordene por risco real, não pela etapa de origem. Separe em três listas:

1. **Impede publicar ou entregar** — segurança crítica ou alta, rastreamento sem consentimento, coleta de dados sem política de privacidade, acessibilidade bloqueante, segredo exposto não rotacionado, regressão encontrada na Fase 1
2. **Deveria ser resolvido** — o resto das pendências abertas
3. **Aceito conscientemente** — o que decidi não corrigir, com o motivo registrado

## Formato do relatório

1. **Veredito:** 🟢 Dentro do padrão · 🟡 Dentro do padrão com pendências · 🔴 Fora do padrão
2. **Mapa de etapas** (tabela da Fase 0)
3. **Regressões encontradas** — se houver, vêm antes de tudo, com arquivo e linha
4. **Inconsistências** (Fase 2)
5. **Pendências consolidadas**, nas três listas
6. **Sobra encontrada** (Fase 2.5): proponho remover, precisa da sua resposta, parece órfão e não é
7. **O que rodar agora**, em ordem: quais etapas precisam ser refeitas e por quê
8. **Resumo em uma frase** que eu possa mandar para o cliente, sem jargão

O veredito é 🔴 se houver qualquer item da lista 1. É 🟡 se houver pendências da lista 2 ou etapas desatualizadas. Só é 🟢 quando todas as etapas aplicáveis rodaram, estão atualizadas e não há pendência aberta além das aceitas conscientemente.

Etapa que nunca rodou não é falha automática: se ela não se aplica ao projeto, diga isso. Se se aplica, ela entra no "o que rodar agora".

Depois do relatório, pare e pergunte o que devo fazer.

## Registro

Depois que eu decidir, salve `auditorias/14-revisao-geral.md` com: data, veredito, regressões encontradas, pendências consolidadas, o que ficou aceito conscientemente e quais etapas recomendei refazer. A parte de sobra vai para `auditorias/limpeza.md`, no formato da skill `limpeza`. Atualize também `auditorias/contexto.md` se a revisão revelou algo que não estava lá.

Marque a etapa no `auditorias/roteiro.md` e feche dizendo o próximo passo: se o veredito não foi 🟢, as etapas a refazer; se foi, e o projeto é de cliente (perfil C ou D), `/projeto-limpo` para gerar a versão de entrega; se é projeto seu, que o roteiro terminou.
