---
name: 00-diagnosticar
description: "Ponto de partida para projeto que JA EXISTE, no lugar do 00-planejar. Faz um raio-x do codigo (stack, estrutura, padroes que ja usa, Git, versao, servicos, o que esta publicado), procura riscos imediatos, reconstroi o contexto do fluxo descrevendo o projeto como ele e, e monta a ordem de auditorias certa para aquele projeto. Use quando o usuario quiser aplicar o kit, verificar tudo ou organizar um projeto ja feito, antigo, herdado ou que nao passou pelo planejamento."
---

# Diagnóstico de projeto existente

Leia primeiro `.claude/lz/FLUXO.md` e `.claude/lz/DETECCAO.md`.

O fluxo numerado foi pensado para projeto que nasce de um design. Esta skill é a porta de entrada para o caso contrário: o projeto **já existe**, e a pergunta é "o que ele é, o que está arriscado e por onde começo a verificar".

**Regra principal: descrever, não reformar.** Esta skill não reorganiza pastas, não renomeia arquivos, não troca bibliotecas e não reescreve código para caber no padrão do kit. Ela documenta o que existe. Mudanças no código só acontecem depois, nas auditorias, item por item, com aprovação.

## Fase 0: Raio-x (sem perguntar)

Levante sozinho, pelo código e pelos comandos, sem alterar nada:

**Projeto**
- Tipo e stack, pelo `DETECCAO.md`
- **Roda hoje?** Suba o projeto como a skill `rodar` faz. Se não sobe, esse é o primeiro achado — e o resto do diagnóstico continua pelo código
- Estrutura de pastas real, em árvore resumida
- **Padrões que o código já segue**: nomes de arquivos e componentes, idioma, onde ficam estilos, como valida formulário, como acessa dados. Diga qual é o padrão **da maioria** e onde há inconsistência

**Dependências**
- Quantidade, as mais desatualizadas (`npm outdated` ou equivalente) e o resumo do `npm audit` — **só a contagem por severidade, e separando total de só produção** (`npm audit --omit=dev`). A triagem completa é da skill `dependencias`; aqui só entra o número, para dimensionar
- Se aparecer **Crítica ou Alta em dependência de produção**, isso é risco imediato e vai para o topo do relatório

**Testes**
- Existe alguma verificação automática? Procure `scripts/`, `tests/`, `test/`, `__tests__`, `*.spec.*`, `*.test.*`, `.maestro/`, e scripts de teste no `package.json`
- **Roda hoje?** Rode. Suíte que não passa é pior que suíte que não existe, porque ninguém confia nela — e é um achado
- Cobre o quê? E, principalmente: os fluxos que valem dinheiro ou dado estão cobertos?
- Tem caso pulado (`.only`, `skip`, asserção comentada)? Isso deixa a suíte verde rodando pouca coisa

**Documentação**
- O MCP de documentação está ligado nesta sessão? Projeto antigo que nunca usou é o caso mais comum, e é o que faz o código novo sair desatualizado. Anote para o `01-ambiente` resolver — ele instala

**Git e versão**
- Existe repositório? Remoto, visibilidade (se der para saber), branch principal, quantidade de commits
- As mensagens de commit seguem algum padrão?
- Versão atual em cada arquivo onde ela mora, se estão iguais, e se existem tags

**Serviços e publicação**
- Serviços externos pelo `.env.example`, configs e imports: banco, autenticação, pagamento, e-mail, analytics
- Está publicado? Em que URL? Domínio próprio ou temporário? (define o estágio: `rascunho`, `preview` ou `producao`)

**O que já existe de documentação**
- `README`, `CLAUDE.md` com conteúdo próprio, registros em `auditorias/` de uso anterior do kit. Nada disso é sobrescrito

**Riscos imediatos** — só uma varredura rápida atrás do que é **Crítico agora**, não a auditoria completa:
- chave secreta no código ou no front (`service_role`, `sk_`, senha)
- `.env` versionado no Git
- vulnerabilidade Crítica ou Alta em dependência de produção
- banco com tabela sem regra de acesso (se houver MCP do banco para conferir)
- site publicado coletando dado pessoal sem nenhuma política de privacidade

Se encontrar algo assim, **diga logo no início do relatório**, antes do resto.

## Fase 1: Perguntas

Siga o protocolo do FLUXO — no máximo 5, com opções e recomendação, e só o que o código não respondeu:

1. Em uma frase: o que é o projeto e para quem?
2. **Tem gente usando de verdade agora?** (muda a prioridade: projeto em uso com dados reais põe segurança na frente de tudo)
3. O que você quer fazer com ele agora: **só verificar e corrigir**, **continuar desenvolvendo** ou **preparar para entregar**?
4. É projeto seu ou de cliente, e outras pessoas usam? Encaixe nos perfis A, B, C ou D do FLUXO — é o que decide quais auditorias entram no roteiro.
5. Tem algo que você já sabe que está quebrado ou que te preocupa?

## Fase 2: Reconstruir o contexto

Com o raio-x e as respostas, prepare os arquivos que o resto do fluxo lê. Mostre cada um e peça aprovação antes de gravar:

- **`auditorias/contexto.md`** completo, no formato do FLUXO: estágio, identidade, stack, serviços, o que existe hoje, o que está pendente, decisões.
- **`CONVENCOES.md`** — **descrevendo o padrão que o código já segue**, não o padrão ideal. Inclua a seção **Testes** com o que existe hoje, mesmo que seja "nenhuma verificação automática": o comando de rodar o que existe, onde os casos ficam e a regra de que fluxo novo ganha caso. Sem essa seção escrita, o próximo trabalho no projeto não roda verificação nenhuma. Inconsistências entram numa seção "A padronizar aos poucos", para o código novo seguir o padrão da maioria e o antigo ser ajustado só quando for mexido. Se já existir um `CONVENCOES.md`, proponha só acréscimos.
- **`PLANO.md`** no formato "estado atual": o que o projeto faz, o que está incompleto ou quebrado, o que você quer a seguir. Não é um roteiro de construção.

Não crie nem altere nada fora desses três arquivos nesta skill.

## Fase 3: Roteiro para este projeto

Use a tabela de perfis do FLUXO para decidir o que entra e o que sai. O que sair aparece na lista "Não se aplicam", com o motivo em uma linha — nunca some em silêncio.

Monte a ordem das próximas skills **para este projeto**, só com as que se aplicam, e justifique cada posição em uma linha. Regras:

1. **Risco imediato encontrado na Fase 0 vem antes de tudo**, com a skill que resolve (normalmente `04-git` ou `07-seguranca`).
2. Depois, sempre: `01-ambiente` (MCPs, e é ele que liga o de documentação, obrigatório) e `02-configurar` (em modo projeto existente: mantém a versão atual, não reinicia o Git, não muda o id de app publicado, e monta a verificação rápida se o projeto não tem nenhuma e o nível pede).
   - Se a contagem do `audit` mostrou Crítica ou Alta em produção, ponha `dependencias` logo depois do `01-ambiente`: em projeto parado a lista costuma ser grande, e deixar para o `07-seguranca` mistura isso com o resto da auditoria.
3. **`03-desenvolver` não se aplica** a projeto existente. Para continuar construindo, depois das auditorias, use `nova-feature`.
4. Ordem das auditorias:
   - **Projeto em uso com dados de pessoas**: `04-git` → `07-seguranca` (+ `banco` em modo auditoria, se houver banco) → `08-lgpd` → depois as demais na ordem normal.
   - **Projeto ainda não publicado ou sem dados sensíveis**: a ordem normal, `04-git` → `05-conversao` → ... → `11-performance`.
5. Publicação só se for o objetivo: `12-pre-lancamento` e `13-dominio` quando houver domínio.
6. **Sempre por último:** `14-revisao-geral`. Se o projeto é de cliente, `projeto-limpo` depois dela, para gerar a versão de entrega.
7. **Projeto grande:** sugira rodar cada auditoria por área ou módulo, em vez do projeto inteiro de uma vez, e diga como dividir.

## Formato do relatório

1. **Riscos imediatos** (se houver) — primeiro, com evidência
2. **Raio-x** em tabela curta: `Aspecto | Situação | Observação`
3. **Perguntas** — e pare aqui

Depois das respostas:

4. **Rascunhos** de `contexto.md`, `CONVENCOES.md` e `PLANO.md` para aprovar
5. **Roteiro numerado** para este projeto, com o motivo de cada passo

## Registro

Depois de aprovado, grave os três arquivos, crie `auditorias/roteiro.md` no formato do FLUXO (perfil, etapas na ordem com o comando exato, concluídas e não aplicáveis) e salve `auditorias/00-diagnostico.md` com: data, resumo do raio-x, riscos imediatos encontrados, respostas às perguntas e o roteiro recomendado. As próximas skills e a `14-revisao-geral` leem esse registro.
