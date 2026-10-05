---
name: testes
description: "Padrao obrigatorio de verificacao automatica. Nao e um comando: carregue sozinho, sem o usuario pedir, ao terminar qualquer mudanca, ao corrigir bug, quando um fluxo novo fica pronto, e para montar a verificacao quando o projeto ainda nao tem (no 03-desenvolver, no 02-configurar de projeto existente e na nova-feature). Define, monta e roda a verificacao automatica do projeto no nivel que ele merece: checagem rapida que abre o projeto de verdade e confere o caminho principal, e suite de casos que acumula. Decide a ferramenta pela stack (Playwright na web e no Electron, Maestro no mobile, adb e logcat no Android, xUnit no .NET), escreve os primeiros casos que valem, isola os dados de teste da base real e registra tudo no CONVENCOES.md para o padrao se manter. Use tambem quando o usuario falar em teste, smoke, verificar se quebrou ou garantir que continua funcionando."
user-invocable: false
---

# Testes: a verificação que roda sempre

Leia primeiro `.claude/lz/FLUXO.md`. Detalhe por stack — ferramenta, comandos e o primeiro caso de cada — em `references/ferramentas.md`.

Duas coisas diferentes vivem aqui, e confundi-las é o erro que faz projeto ter 200 testes verdes que não provam nada:

- **Verificação rápida (smoke)** — abre o projeto de verdade e confere se o básico funciona. Roda **sempre**, depois de cada mudança, sem ninguém pedir. Precisa ser rápida e ter saída curta.
- **Suíte de casos** — casos que acumulam no repositório e protegem o que já foi consertado. Cresce a cada bug corrigido e a cada fluxo novo.

**A regra que faz isso funcionar não é a existência dos scripts, é a linha no `CONVENCOES.md`:** "fluxo novo ganha caso na verificação". Sem ela escrita, o agente da próxima conversa não sabe que existe essa obrigação.

## Modos

- **Montar** — decidir o nível, escolher a ferramenta, escrever os primeiros casos e gravar a regra no `CONVENCOES.md`. Use quando o projeto ainda não tem verificação, ou quando o nível dele mudou (ganhou login, banco, pagamento). **Ninguém precisa pedir:** o `03-desenvolver` monta na primeira etapa com regra de negócio, dado ou login; o `02-configurar` monta em projeto existente que não tem nada; a `nova-feature` monta quando a novidade é a primeira coisa que merece caso. Carregada assim, no meio do trabalho, a proposta entra no plano daquele trabalho — nível, ferramenta e primeiros casos — e é aprovada junto com ele, não como um comando à parte.
- **Rodar** — executar o que existe, ler as falhas e consertar. É o que acontece automaticamente durante o desenvolvimento.

## Nível: quanto este projeto merece

Testar demais um site de uma página é desperdício; testar de menos um app com dados de pessoas é dívida. Decida pelo que o projeto perde se quebrar:

| Nível | Quando | O que tem |
|---|---|---|
| **1 — Checagem** | Landing page, site institucional, projeto de uma tela (perfil C, e perfil A simples) | Sem framework de teste. A build passa, a página abre sem erro no console, o formulário chega ao destino, nenhum link quebrado. É o que o `12-pre-lancamento` já faz — não monte suíte aqui |
| **2 — Unidade + caminho principal** | Site ou app com regra de negócio, dados ou login (perfis B e D) | Testes de unidade nas **regras puras** (cálculo, recorrência, data, validação, formatação) + um caminho fim-a-fim por fluxo que vale dinheiro ou dado: entrar, criar, editar, excluir, pagar |
| **3 — Smoke no app compilado** | App instalável: Electron, mobile, desktop | Tudo do nível 2 + um smoke que **abre o app compilado de verdade** e percorre os fluxos pela mesma API que as telas usam |

Diga o nível e o motivo em uma linha. Nível é decisão do usuário: proponha, não imponha.

**Comece pelo que dá lucro imediato:** as regras puras (baratas, rápidas, nunca flakey) e **um** caminho principal fim-a-fim. Suíte grande montada de uma vez não é mantida.

## O que a verificação rápida precisa cobrir

Não é "testar tudo". É responder: *quebrou algo que funcionava?*

1. **Abre.** O projeto sobe, ou o app compilado inicia, sem erro fatal.
2. **Console limpo.** Nenhum erro novo no console. Erro que já existia e está registrado não conta — mas fica escrito.
3. **Um caminho completo por área**: criar → ler → editar → excluir. É o que pega 90% das regressões, porque é onde os dados passam.
4. **O fluxo que você acabou de mexer**, especificamente.
5. **Nada trava esperando gente**: nenhum diálogo nativo (`alert`, `confirm`) abrindo, nenhuma tela pedindo login quando o teste roda isolado.
6. **O que já foi consertado antes** continua consertado — é para isso que os casos acumulam.

Escreva a saída como uma linha por checagem (`ok` / `FALHA`) e uma contagem no fim. Log comprido ninguém lê, e você também não precisa: leia a falha, não o log inteiro.

**Alvo de tempo: menos de um minuto para a verificação rápida.** Verificação lenta deixa de ser rodada, e aí não serve para nada. Se passar disso, separe: a rápida em cada mudança, a completa antes do build de entrega.

## Isolamento dos dados: a parte perigosa

Teste que escreve é teste que pode destruir. Regras, sem exceção:

- **Banco próprio e temporário**, em pasta de temporários, criado e apagado pelo próprio teste. Nunca o banco de trabalho, nunca o arquivo de dados real do usuário.
- **Nunca apontar teste para o projeto de produção.** Se o serviço tem ambiente separado (Supabase, Firebase), use o de teste; se não tem, o teste cria a conta que vai usar e a exclui no fim, e isso precisa estar dito no nome do arquivo e no cabeçalho dele.
- **Teste que depende de internet ou de serviço real fica separado** dos outros e não entra na verificação rápida: ele falha por motivo que não é o seu código.
- **Nada de credencial real no código do teste.** Variável de ambiente, e `.env.example` atualizado.
- Antes de rodar um teste que escreve em serviço real pela primeira vez, **diga o que ele vai criar e apagar, e peça aprovação.**

## A regra de ouro: verificação antes da correção

Quando aparecer um bug:

1. **Escreva primeiro a verificação que falha** por causa dele. Rode e veja falhar — é isso que prova que a verificação testa o que você pensa que testa.
2. **Depois** corrija.
3. Rode de novo e veja passar.

Teste escrito depois da correção passa por construção e não prova nada. Este é o único ponto desta skill que não tem exceção: bug corrigido sem verificação que falhava volta, e volta silencioso.

## Anti-padrões: teste verde que não protege nada

Recuse-se a produzir isto, mesmo que deixe o relatório mais bonito:

- **Enfraquecer a asserção para o teste passar.** Se o teste falhou, o culpado é o código até prova em contrário. Mudar o esperado para o que saiu é apagar o alarme, não consertar o incêndio.
- **Mock de tudo.** Teste que simula o banco, a rede e o relógio testa os mocks. Regra pura: sem mock, é função. Caminho principal: com o dado de verdade, no banco temporário.
- **Snapshot atualizado no automático.** Snapshot só se atualiza depois de olhar a diferença e concluir que a mudança era esperada.
- **Teste que depende de ordem, de `sleep` ou do relógio do sistema.** Espere por condição (elemento visível, valor presente), não por tempo. Data fixa nos testes de data.
- **`.only` e `skip` esquecidos**, que fazem a suíte verde rodando três casos.
- **Cobertura como meta.** 80% de cobertura sem asserção é 0% de proteção. Conte fluxos protegidos, não linhas visitadas.
- **Testar o que a biblioteca já garante.** Não teste que o React renderiza; teste a sua regra.

## Projeto que já tem testes

**Descrever e estender, não migrar.** Script próprio que funciona é melhor que ferramenta padrão recém-instalada. Levante o que existe, rode, diga o que cobre e o que não cobre, e acrescente os casos que faltam **no estilo que já está lá**. Só proponha trocar de ferramenta se o usuário pedir, ou se o que existe estiver claramente travando o trabalho — e aí com o custo da troca dito na frente.

Em modo montar, comece sempre por: existe algo em `scripts/`, `test/`, `tests/`, `__tests__`, `*.spec.*`, `*.test.*`, ou script de teste no `package.json`? Roda hoje?

## Quando roda sozinho

Isto não depende de o usuário chamar:

| Momento | O que roda |
|---|---|
| Primeira etapa do `03-desenvolver` com regra de negócio, dado ou login | Modo montar, no nível do projeto |
| `02-configurar` em projeto existente sem verificação (nível 2 ou 3) | Modo montar: a verificação rápida primeiro |
| Fim de cada etapa do `03-desenvolver` | Verificação rápida. Se quebrou algo da etapa anterior, conserte antes de seguir |
| Fim de cada etapa da `nova-feature` | Verificação rápida + os casos das áreas que a novidade toca |
| Depois de corrigir qualquer bug | A verificação que falhava, agora passando |
| Fluxo novo terminado | O caso novo entra na suíte — é a regra do `CONVENCOES.md` |
| Antes de um `build` de entrega | A suíte completa. Build de entrega não sai de suíte vermelha |
| `12-pre-lancamento` | Suíte completa passando faz parte do veredito |
| `14-revisao-geral` | Confere se a suíte ainda passa e se não apodreceu (casos pulados, asserção comentada) |

Quando a verificação falha, **pare e mostre a falha** — qual checagem, o que esperava, o que veio. Não siga escrevendo código em cima de vermelho, e não conserte o teste em vez do código.

## Formato do relatório

**Modo montar:**
1. **O que já existe** e se roda hoje
2. **Nível proposto**, com o motivo
3. **Ferramenta** escolhida para esta stack, e por quê (uma linha; detalhe em `references/ferramentas.md`)
4. **Primeiros casos**, em lista: o que cada um protege
5. **Como os dados ficam isolados** da base real
6. **O comando** de rodar, e o que entra no `CONVENCOES.md`

Pare e peça aprovação antes de criar arquivo.

**Modo rodar:**
1. Uma linha por checagem, `ok` ou `FALHA`
2. Contagem final
3. Para cada falha: o que esperava, o que veio, e o arquivo e linha mais prováveis
4. O que **não** está coberto e apareceu no caminho

## Registro

No `CONVENCOES.md`, seção **Testes** — é o que mantém o padrão entre conversas:

```markdown
## Testes
- Nível: 3 — smoke no app compilado
- Rodar rápido: `npm run smoke`
- Rodar tudo: `npm test`
- Onde ficam: `scripts/` (smoke e telas), `tests/` (unidade)
- Dados: banco temporário em %TEMP%, modo só local, nunca a base real
- **Fluxo novo ganha caso na verificação.** Bug corrigido ganha caso que falhava antes.
```

Em `auditorias/testes.md`: data, nível, ferramenta, o que está coberto, o que não está (e é decisão consciente), e os casos que nasceram de bug — com o bug que cada um protege. Essa última lista é a que mostra, com o tempo, onde o projeto quebra mais.
