---
name: limpeza
description: "Encontra codigo morto e sobra acumulada no projeto: arquivo, funcao e export que ninguem mais usa, dependencia instalada e nao usada, CSS e imagem orfas, variavel de ambiente que sumiu do codigo, teste de coisa que foi apagada, codigo comentado, rota e coluna de banco sem uso. Usa a ferramenta certa por stack (Knip em JS e TS, vulture e ruff em Python, analisadores do Roslyn em .NET, deadcode em Go), separa o que e de fato morto do que so parece, e propoe a remocao em commits reversiveis. Nunca apaga sozinho. Use quando o usuario falar em codigo morto, sobra, faxina, limpar o projeto, funcao que nao e usada ou arquivo orfao."
---

# Limpeza: código morto e sobra acumulada

Leia primeiro `.claude/lz/FLUXO.md`. Ferramenta e comandos por stack em `references/ferramentas.md`.

Projeto real acumula sobra porque o trabalho é assim: entra funcionalidade, sai funcionalidade, troca-se a biblioteca, refaz-se a tela. O que sai raramente sai inteiro — ficam a função que ninguém chama, o arquivo que ninguém importa, a dependência que ninguém usa e o teste daquilo que foi apagado.

**A distinção que governa esta skill inteira: a ferramenta reporta *inalcançável*, não *morto*.** Quando o Knip aponta um export, ele está dizendo uma coisa exata e verdadeira — não conseguiu chegar lá partindo de um arquivo de entrada. Isso **não** significa que o código não é usado: significa que o uso, se existe, acontece por um caminho que a análise estática não vê. Tratar inalcançável como morto e apagar é o jeito mais rápido de quebrar um projeto que estava funcionando.

**Regra principal: propõe, nunca apaga sozinho.** Todo achado passa pelo relatório e pela sua aprovação. Em nenhuma hipótese esta skill remove código porque a ferramenta disse que dá.

**Esta skill não muda como o trabalho normal acontece.** Quando o usuário pede para trocar o cabeçalho, refazer uma tela ou substituir uma função, o que sai **sai na hora**, sem pergunta e sem lista de órfãos: aquilo é a tarefa, e o Git guarda o que foi apagado. Aqui é outra coisa — varredura do projeto inteiro, por conta própria, atrás do que passou batido em alguma mudança antiga. É só neste contexto que a aprovação existe, porque aqui ninguém pediu nada.

## Fase 0: Rode a ferramenta

Detecte a stack pelo `DETECCAO.md` e rode o que corresponde (comandos em `references/ferramentas.md`). Trabalhe **a partir da saída da ferramenta**, sem sair lendo arquivo por arquivo: em projeto grande isso custa caro e não acha mais nada.

Em projeto grande, rode por área ou módulo e trate uma área por vez. Relatório de 300 achados não é revisado por ninguém.

Se a ferramenta pedir configuração para entender o projeto (pontos de entrada, aliases de caminho), **configure em vez de silenciar**. Esconder um resultado não é resolvê-lo — e um `ignore` posto no lugar errado apaga justamente o achado que importava.

## Fase 1: As sete formas de parecer morto sem estar

Cada achado passa por esta lista antes de virar proposta. É a fase que dá valor à skill:

1. **Referência dinâmica.** `require("./handlers/" + nome)`, `import()` com template, tabela de rotas montada por string, busca por nome, injeção de dependência, reflexão. A análise estática não enxerga nada disso.
2. **Ponto de entrada por convenção.** Arquivo que o framework carrega pelo lugar onde está, não por import: página e rota do Next, arquivo do Expo Router, `preload` do Electron, service worker, migration, seed, script de CLI, função serverless, handler de webhook. Nunca é importado por ninguém — e é essencial.
3. **API pública.** Em biblioteca ou pacote, o que é exportado para fora é "não usado" dentro do próprio repositório por definição. Isso é o produto, não sobra.
4. **Usado fora do código.** Nome de classe CSS montado por string, `onclick` no HTML, chave lida de um JSON de configuração, nome de variável de ambiente, imagem referenciada no CMS, campo citado numa query escrita à mão.
5. **Usado só por teste.** Duas leituras opostas: ou a função morreu e o teste dela morre junto, ou é API de verdade e o teste é o único consumidor dentro do repositório. **Decida caso a caso e diga qual das duas** — nunca apague os dois no automático.
6. **Chamado do outro lado.** Coluna que só o painel administrativo usa, endpoint que o gateway de pagamento chama, deep link, rota que só um app antigo ainda acessa, campo que alimenta um relatório fora do repositório. O repositório não tem como saber.
7. **Recente, ainda não ligado.** Entrou nesta semana e o resto ainda não foi escrito. Confira a data no `git log`: o que nasceu há poucos dias é obra em andamento, não sobra. Confira também "Planejado, ainda não feito" no `auditorias/contexto.md`.

**Verificação obrigatória antes de propor qualquer remoção:** procure o nome como **texto puro em todo o repositório**, não só nos imports — inclua HTML, JSON, YAML, CSS, templates, migrations, configs de deploy e documentação. Um `grep` do nome custa um segundo e é o que separa proposta de acidente.

## Fase 2: Confiança, por achado

| Confiança | Quando | O que propor |
|---|---|---|
| **Alta** | Nenhuma ocorrência do nome em nenhum arquivo do repositório, não é ponto de entrada por convenção, e não é API pública | Remover |
| **Média** | Só a definição aparece, mas há referência dinâmica na área, ou é usado apenas por teste | Remover **com a pergunta respondida** antes: quem usava isso, e por que parou? |
| **Baixa** | Ponto de entrada, API pública, ou qualquer uso fora do código | **Não propor.** Listar como "parece órfão, e não é", com o motivo em uma linha |

Achado de confiança baixa vai no relatório de propósito: é o que impede a próxima rodada de levantar a mesma suspeita de novo.

## Fase 3: O que não é lixo

Não proponha remover, mesmo que a ferramenta aponte:

- **`.env.example`** — é documentação; a variável ali existe justamente para não estar em uso
- **Migrations já aplicadas** — são histórico, e apagar quebra quem for recriar o banco do zero
- **`CHANGELOG.md`, `LICENSE`, `CONVENCOES.md`, registros em `auditorias/`**
- **Arquivos gerados** (build, tipos gerados, lockfile) — saem regenerando, não apagando
- **Polyfill e tipo de biblioteca** que só o compilador usa
- **Compatibilidade mantida de propósito** — se o `CONVENCOES.md` ou um registro diz que aquilo fica por um motivo, fica

## Fase 4: Apagar sem se arrepender

O que torna a remoção segura não é a análise: é o Git. Feito assim, qualquer erro volta com um comando:

1. **Um commit por grupo coeso**, nunca um commit gigante de faxina. `refactor: remove tela de relatorio antiga`, `chore: remove dependencias nao usadas`. Grupo pequeno = `git revert` cirúrgico.
2. **Remoção não se mistura com outra coisa.** Nunca apague e refatore no mesmo commit; nunca apague e conserte um bug no mesmo commit. O commit de remoção precisa ser reversível sem levar mais nada junto.
3. **Depois de cada commit**, rode a verificação do projeto (seção Testes do `CONVENCOES.md`) e gere a build. Remoção que quebra a build volta na hora, e você sabe exatamente qual grupo causou.
4. **Uma área por vez**, com o projeto rodando entre elas. Apagar trinta coisas e depois descobrir que uma quebrou dá um dia de trabalho para achar qual.
5. **Código comentado vai embora sem cerimônia.** O Git já guarda o que foi apagado — bloco comentado "para não perder" é ruído que atrapalha a leitura e engana a busca. Isso é confiança alta por natureza.

Se em algum momento você não souber dizer **quem usava aquilo e por que parou**, o achado não está pronto para ser proposto. Volte para a Fase 1.

## Além do código

O mesmo raciocínio vale para o resto, e é onde costuma haver mais peso acumulado:

| Tipo | Como achar | Cuidado |
|---|---|---|
| Dependência instalada e não usada | Knip, `deptry`, `dotnet list package` | Ferramenta de build e tipo aparecem como não usados sem serem. Isto é diferente de vulnerabilidade: aquilo é a skill `dependencias` |
| Script no `package.json` que ninguém roda | leitura + `grep` no CI | Script chamado por outro script ou pelo CI |
| CSS e token de design órfãos | PurgeCSS, aba Coverage do navegador | Classe montada por string; estado que só aparece em erro |
| Imagem, fonte e mídia sem referência | `grep` do nome do arquivo | Referência pelo CMS ou por caminho montado |
| Variável de ambiente | comparar `.env.example`, código e painel da hospedagem | Diga também o contrário: usada no código e **ausente** do `.env.example` |
| Teste de código apagado | a suíte roda e aquele arquivo não testa mais nada que exista | Ligado à skill `testes` |
| Rota, endpoint e página sem link | `grep` de links internos e do mapa do site | Acesso direto por URL, e o que está no `sitemap.xml` |
| Feature flag ligada ou desligada há meses | `grep` da flag | Decida: virou padrão, ou o caminho morre. Flag eterna é dívida |
| Chave de tradução sem uso | ferramenta de i18n ou `grep` | Chave montada por string |
| Coluna e tabela de banco | ver a seguir | Destrutivo. Regra própria |

## Banco de dados: nunca em uma etapa

Coluna e tabela têm **dado dentro**, e apagar não volta com `git revert`. Carregue a skill `banco` e siga:

1. **Nunca no mesmo passo.** Primeiro pare de escrever naquela coluna e publique. Espere pelo menos um ciclo de uso real. Só numa versão seguinte, se nada reclamou, remova.
2. **Backup antes**, sempre, e confirmado antes de qualquer `drop`.
3. **Confirme com o dono do dado.** Coluna que "ninguém usa" no código pode alimentar um relatório, um painel administrativo ou uma exportação que você não vê.
4. Em projeto de cliente com dados de pessoas, coluna não usada também é assunto de LGPD: dado guardado sem finalidade deveria sair. Diga isso, e deixe o registro na `08-lgpd`.

## Quando esta skill roda

É **comando**, não padrão: não rode faxina a cada mudança, porque código em construção está legitimamente pela metade, e apontar isso como sobra só gera ruído.

Os momentos que valem:

- **Quando o usuário pedir** — "faz uma faxina", "tem código morto?" — a qualquer momento
- **Dentro da `14-revisao-geral`**, que roda as Fases 0 a 2 desta skill e traz a sobra no relatório dela. É o momento natural: antes do `projeto-limpo`, o projeto entra mais leve e mais fácil de ler
- **Quando a build ou o repositório engordou** sem motivo aparente

## Formato do relatório

1. **Resumo em números**: achados por tipo, e quanto representam (arquivos, linhas, peso em disco)
2. **Tabela:** `Item | Tipo | Onde | Confiança | Última alteração | Por que parece morto`
3. **Proponho remover** — agrupado em commits, com a mensagem de cada um pronta
4. **Precisa da sua resposta** — os de confiança média, cada um com a pergunta exata
5. **Parece órfão e não é** — os de confiança baixa, com o motivo
6. **Banco** — separado, com o plano de duas etapas, se houver
7. **Ferramenta**: o que ela não cobre nesta stack

Pare e pergunte o que remover. Nunca comece a apagar dentro do próprio relatório.

## Registro

Salve `auditorias/limpeza.md` com: data, ferramenta e comando, o que foi removido (com o commit de cada grupo), o que **decidi manter** e por quê, e os itens de confiança baixa.

A seção **decidi manter** é a que faz a próxima rodada valer a pena: sem ela, a mesma discussão sobre o mesmo arquivo volta a cada passada, e a resposta de antes some.
