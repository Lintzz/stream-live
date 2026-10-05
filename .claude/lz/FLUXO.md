# Fluxo LZ — regras comuns a todas as etapas

Este arquivo define como todas as skills do kit `lz` se comportam. Leia-o antes de executar qualquer etapa e siga estas regras junto com as instruções específicas da skill.

## As etapas

Numeradas na ordem em que rodam num **projeto novo**, que parte de um design em HTML ou só da ideia descrita na conversa:

| Nº | Skill | Fase | Quando |
|---|---|---|---|
| 00 | `00-planejar` | Início | Antes de existir código, com o design em HTML **ou** só com a ideia: escopo, stack, estrutura e roteiro |
| 01 | `01-ambiente` | Início | Stack escolhida: conectar e testar os MCPs (inclusive o do GitHub, que a próxima etapa usa) |
| 02 | `02-configurar` | Início | Metadados, Git, repositório, política de versão e trava de commits |
| 03 | `03-desenvolver` | Construção | Executa o roteiro do `PLANO.md` etapa por etapa |
| 04 | `04-git` | Auditoria | Com o código pronto, **antes do primeiro push** |
| 05 | `05-conversao` | Auditoria | Hero, CTAs, prova social |
| 06 | `06-interface` | Auditoria | Estados de tela (também é padrão durante o `03-desenvolver`) |
| 07 | `07-seguranca` | Auditoria | Chaves, XSS, regra de negócio no servidor, IDOR |
| 08 | `08-lgpd` | Auditoria | Dados pessoais, cookies, requisitos legais |
| 09 | `09-seo` | Auditoria | Com conteúdo e páginas definidos |
| 10 | `10-acessibilidade` | Auditoria | Com os componentes finais |
| 11 | `11-performance` | Auditoria | Por último, medindo o site com tudo que foi adicionado |
| 12 | `12-pre-lancamento` | Publicação | Logo antes de publicar ou mostrar ao cliente |
| 13 | `13-dominio` | Publicação | Quando o domínio for comprado — pode ser depois da entrega |
| 14 | `14-revisao-geral` | Final | Conferência do projeto inteiro: regressões, pendências consolidadas e código morto. Roda também sempre que quiser saber se continua tudo certo |

Depois da 14, se o projeto vai para um cliente, vem o `projeto-limpo`: a versão de entrega, sem o kit e sem rastro de IA, com a conferência de contas e credenciais. Ele não é numerado porque só roda quando há entrega.

**Projeto que já existe** começa por outra porta:

| Nº | Skill | Quando |
|---|---|---|
| 00 | `00-diagnosticar` | No lugar do `00-planejar`: raio-x do projeto, riscos imediatos, contexto reconstruído e o roteiro de auditorias para aquele projeto |

Depois dele vêm `01-ambiente` e `02-configurar` (em modo projeto existente), **pula-se o `03-desenvolver`**, e as auditorias seguem na ordem que o diagnóstico recomendar — em projeto já em uso com dados de pessoas, segurança vem antes. Termina na `14-revisao-geral`, como no projeto novo. Para continuar construindo depois, use `nova-feature`.

**Comandos** que o usuário chama a qualquer momento:

| Skill | Quando |
|---|---|
| `rodar` | Ver o projeto na tela, sem gerar build. Confere as dependências uma vez por dia |
| `build` | Gerar o arquivo, subir a versão e publicar a release |
| `nova-feature` | Acrescentar algo a um projeto que já passou pelas auditorias |
| `limpeza` | Achar código morto e sobra acumulada quando o usuário quiser. Também roda dentro da `14-revisao-geral` |
| `projeto-limpo` | Versão de entrega: cópia sem o kit e sem rastro de IA, e a conferência de entrega quando é para cliente |

**Padrões**, que você carrega sozinho quando a situação aparece. O usuário **não** deve precisar chamá-los:

| Situação | Carregue |
|---|---|
| Escrever código com biblioteca ou framework | `.claude/lz/DOCUMENTACAO.md` |
| Terminou uma mudança, corrigiu um bug, ou um fluxo novo ficou pronto | `testes`, modo rodar |
| Projeto de nível 2 ou 3 ainda sem verificação automática | `testes`, modo montar (no `03-desenvolver`, no `02-configurar` ou na `nova-feature`) |
| Instalação reportou vulnerabilidade, entrou dependência nova, ou a checagem diária do `rodar` achou Crítica ou Alta em produção | `dependencias` |
| Mexer em banco: tabela, coleção, migration, query, índice, RLS, regra do Firestore | `banco` |
| Construir tela, lista, formulário, botão ou componente | `06-interface` |
| A mudança traz um serviço externo ou MCP novo | `01-ambiente`, modo preparar |

`testes`, `dependencias` e `banco` ficam fora do menu `/`: o usuário não chama, você carrega. Quando ele pedir o relatório de uma dessas áreas com palavras ("audita o banco", "vê as dependências", "monta os testes"), carregue a skill no modo auditoria ou montar. `06-interface` e `01-ambiente` são etapas numeradas e também padrões. Em todos os casos, esperar o usuário chamar para aplicar o padrão é erro.

A ordem não é obrigatória, mas é a que evita retrabalho: primeiro o que decide, depois o que constrói, depois o que refina, por último o que confere. O `01-ambiente` vem antes do `02-configurar` para que o MCP do GitHub já esteja conectado quando for criar o repositório. O `04-git` vem depois do desenvolvimento porque é quando existe histórico para conferir, e antes do push porque é aí que um segredo ainda não vazou.

## Perfil do projeto: o que se aplica

Nem toda auditoria faz sentido em todo projeto. O perfil é decidido no `00-planejar` ou no `00-diagnosticar`, fica no `auditorias/contexto.md` e define o roteiro.

| Perfil | O que é |
|---|---|
| **A — Pessoal, uso próprio** | Ferramenta sua, roda no seu computador ou celular, ninguém além de você usa |
| **B — Pessoal, publicado** | Seu, mas outras pessoas usam: app em loja, site público, projeto de portfólio, código aberto |
| **C — Cliente, site** | Landing page ou site institucional de alguém |
| **D — Cliente, sistema** | Site ou app de cliente com login, banco, dados de pessoas ou pagamento |

| Skill | A | B | C | D |
|---|---|---|---|---|
| `04-git` | Sim | Sim | Sim | Sim |
| `05-conversao` | Dispensa | Se quer que usem | Sim | Sim |
| `06-interface` | Se tem tela | Sim | Sim | Sim |
| `07-seguranca` | Só chaves e dependências | Sim | Sim | Sim, prioridade |
| `08-lgpd` | Dispensa | Se coleta dado de alguém | Se coleta dado ou usa analytics | Sim, prioridade |
| `09-seo` | Dispensa | Se quer ser achado no Google | Sim | Se tem parte pública |
| `10-acessibilidade` | Dispensa | Recomendada | Sim | Sim |
| `11-performance` | Rápida | Sim | Sim | Sim |
| `12-pre-lancamento` | Se instala em outro lugar | Sim | Sim | Sim |
| `13-dominio` | Dispensa | Se tem domínio | Se tem domínio | Se tem domínio |
| `14-revisao-geral` | Sim | Sim | Sim | Sim |

"Dispensa" não é proibição: se o usuário pedir, rode. Mas não é oferecida no roteiro, e o motivo fica escrito.

O `projeto-limpo` entra no fim do roteiro nos perfis C e D (entrega ao cliente). Nos perfis A e B, só se o usuário quiser publicar ou mandar o projeto a alguém.

O perfil também sugere o **nível de teste** (detalhe na skill `testes`): perfil C e perfil A simples ficam no nível 1, só a checagem que o `12-pre-lancamento` já faz — suíte ali é desperdício. Perfis B e D pedem nível 2: unidade nas regras puras e um caminho fim-a-fim por fluxo que vale dinheiro ou dado. App instalável (Electron, mobile, desktop) pede nível 3: smoke que abre o app compilado de verdade.

Em projeto A, quando `07-seguranca` rodar em versão reduzida, verifique apenas: chave ou senha escrita no código, `.env` versionado e dependências com vulnerabilidade. O resto vira N/A com o motivo "uso próprio, sem usuários e sem exposição".

## Roteiro do projeto

O `00-planejar` e o `00-diagnosticar` gravam `auditorias/roteiro.md`: a lista completa das etapas **daquele** projeto, na ordem, com o comando exato de cada uma. É o passo a passo que o usuário acompanha do começo ao fim.

```markdown
# Roteiro do projeto
Perfil: C — Cliente, site · Estágio: preview · Atualizado em: AAAA-MM-DD

## Próximo
- [ ] `/04-git` — segredos e .gitignore, antes do primeiro push

## Depois
- [ ] `/05-conversao` — hero, CTAs, prova social
- [ ] `/07-seguranca` — chaves, validação do formulário
- [ ] `/12-pre-lancamento` — veredito antes de mostrar ao cliente
- [ ] `/14-revisao-geral` — conferência final e código morto
- [ ] `/projeto-limpo` — versão de entrega para o cliente

## Concluídas
- [x] `/00-planejar` — 22/09
- [x] `/01-ambiente` — 22/09

## Não se aplicam neste projeto
- `/08-lgpd` — só link de WhatsApp, não coleta dado nem usa analytics
- `/13-dominio` — por enquanto: o cliente ainda não comprou o domínio
```

Regras:

- **Toda skill do fluxo, ao terminar, atualiza o roteiro**: marca a própria linha como concluída com a data, move a seguinte para "Próximo" e, na última frase da resposta, diz **o comando exato** da próxima etapa. Nunca deixe o usuário perguntar "e agora?".
- Se algo mudar (o projeto ganhou banco, o cliente comprou domínio, o perfil mudou), atualize o roteiro e diga o que entrou ou saiu.
- Se o roteiro não existir e o usuário rodar uma auditoria direto, tudo bem: rode, e ofereça criar o roteiro ao final.

## Arquivos que o fluxo usa

Tudo fica na pasta `auditorias/` na raiz do projeto.

- **`auditorias/roteiro.md`** — o passo a passo deste projeto, com o que já foi feito, o que vem agora e o que não se aplica.
- **`auditorias/contexto.md`** — o que se sabe sobre o projeto. Toda skill lê antes de perguntar qualquer coisa e escreve nele o que aprender.
- **`auditorias/<n>-<área>.md`** — o registro de cada etapa concluída (`00-diagnostico.md`, `03-desenvolvimento.md`, `04-git.md`, `05-conversao.md`, `06-interface.md`, `07-seguranca.md`, `08-lgpd.md`, `09-seo.md`, `10-acessibilidade.md`, `11-performance.md`, `12-pre-lancamento.md`, `13-dominio.md`, `14-revisao-geral.md`, e sem número `banco.md`, `dependencias.md`, `testes.md`, `limpeza.md` e `projeto-limpo.md`).
- **`auditorias/features/<nome>.md`** — o registro de cada funcionalidade adicionada depois.
- **`PLANO.md`** — na raiz: stack escolhida, roteiro e escopo. Projeto que nasceu da ideia tem também o escopo em v1 / Depois / **Não vai ter**, as telas e os dados — a lista "Não vai ter" vale como decisão registrada.
- **`CONVENCOES.md`** — na raiz: estrutura de pastas, nomenclatura e padrões de código do projeto. Criado no `00-planejar` e consultado sempre que algo novo for escrito.

**Tudo isso vai para o repositório**, junto com as skills em `.claude/skills/` e as regras em `.claude/lz/`. Não há modo nem escolha: clonar o projeto em outra máquina traz o kit, o contexto, as auditorias e a documentação, e o trabalho continua de onde parou.

A única ressalva é por projeto, não por configuração: `auditorias/07-seguranca.md` descreve vulnerabilidades encontradas. Se **este** repositório for público, diga isso ao usuário e ofereça ignorar essa pasta — decisão dele, tomada na hora, com o caso na frente. A outra saída é publicar a cópia do `projeto-limpo`, que vai sem `auditorias/` e sem histórico.

## Formato do `auditorias/contexto.md`

```markdown
# Contexto do projeto
Atualizado em: AAAA-MM-DD

## Projeto
- Tipo: (landing page / institucional / app com login / e-commerce...)
- Perfil: (A pessoal / B pessoal publicado / C cliente site / D cliente sistema)
- Cliente ou projeto próprio:
- Estágio: (rascunho / preview / producao)
- URL atual: (temporária ou definitiva)
- Domínio definitivo: (ou "ainda não comprado")
- Hospedagem:
- Repositório: (público / privado / não tem)

## Ambiente
- MCPs conectados e testados:
- MCPs que ajudariam e não estão conectados:
- Acessos que tenho: (painel da hospedagem, DNS, banco, gateway)

## Identidade
- Dono do produto: (eu / nome do cliente)
- Nome do produto:
- Id do app: (domínio invertido — imutável depois de publicado)
- Licença:

## Como rodar e gerar build
- (preenchido pelas skills `rodar` e `build` — formato em `DETECCAO.md`)

## Stack e serviços
- (uma linha por serviço: para que serve e se é gratuito ou pago)

## O que existe hoje
- (funcionalidades já implementadas)

## Planejado, ainda não feito
- (o que está previsto mas não existe — não é falha nas auditorias)

## Decisões
- (decisão + data + motivo, incluindo o que optei por não fazer)
```

Mantenha o arquivo enxuto: uma linha por informação, sem repetir o que já está lá.

## Estágio do projeto

Todo projeto está em um destes três estágios, e isso muda o que cobrar:

- **`rascunho`** — só na máquina, sem deploy
- **`preview`** — publicado numa URL temporária (`*.vercel.app`, `*.netlify.app`, subdomínio da hospedagem) para o cliente ver, sem domínio próprio
- **`producao`** — domínio definitivo apontando para o site

O estágio fica registrado em `auditorias/contexto.md`. Se não estiver lá, descubra pelo código e pelas configs, e pergunte em caso de dúvida. **A maioria dos projetos passa a maior parte do tempo em `preview`, e isso é normal — não é pendência.**

### Itens que dependem de domínio

Nos estágios `rascunho` e `preview`, os itens abaixo **não são falha**. Marque cada um como **⏸️ Adiado (sem domínio)** e junte todos numa seção **"Pendências de domínio"** no fim do relatório. Eles não entram nas pendências normais, não contam para severidade e nunca bloqueiam nada:

- `canonical`, `og:url` e `og:image` com URL absoluta
- `sitemap.xml` e a linha `Sitemap:` do `robots.txt`
- URLs dentro do schema.org
- Redirecionamento entre versão com e sem `www`, HSTS
- Certificado SSL do domínio próprio
- SPF, DKIM e DMARC no DNS
- URLs de redirecionamento do provedor de autenticação
- URL do webhook do gateway de pagamento
- Restrição de domínio em chaves de API e em serviços de formulário
- Google Search Console e Google Business Profile

Em `preview`, verifique **o que já dá para verificar** e diga o que fica para o domínio. Exemplo: a tag `canonical` existe e está bem formada, mas aponta para a URL temporária — isso é ⏸️, não ❌.

O que **continua valendo em qualquer estágio**: segurança do código, chaves expostas, validação, RLS, acessibilidade, performance, conteúdo, LGPD e headers configurados no arquivo da hospedagem.

Em `preview`, acrescente sempre duas verificações próprias do estágio:

- A URL temporária **não deve ser indexada** pelo Google. Confirme `noindex` ou proteção por senha enquanto for rascunho para o cliente ver.
- Se o site em preview já aponta para banco ou serviços de **produção**, avise: dado de teste indo para a base real é problema, e isso vale em qualquer estágio.

Quando o domínio chegar, a skill `13-dominio` lê as "Pendências de domínio" de todos os registros e fecha essa lista.

## Uso de MCPs

MCPs são conexões diretas com os serviços do projeto (banco, hospedagem, repositório, documentação). Quando disponíveis, dão resposta de verdade em vez de suposição.

Regras:

1. **Veja o que existe antes de precisar.** No começo da etapa, verifique quais ferramentas MCP estão disponíveis na sessão. Não anuncie a lista; só use.
2. **O MCP de documentação é obrigatório em todo projeto**, seja novo ou antigo. Se não estiver na sessão, o `01-ambiente` instala antes de qualquer código ser escrito. As regras de uso — quando consultar, quando não precisa e o que fazer quando não achar — estão em `.claude/lz/DOCUMENTACAO.md`.
3. **Prefira o MCP à suposição.** Se há MCP do banco, consulte as políticas de acesso reais em vez de inferir pelo código. Se há MCP da hospedagem, confira as variáveis de ambiente e os headers configurados lá. Se há MCP de documentação, consulte a documentação oficial da versão em uso em vez de responder de memória.
4. **Confirme que funciona antes de confiar.** Faça uma chamada de leitura simples primeiro. Se falhar, diga que o MCP está conectado mas não respondeu, e siga pelo código, marcando o item como 🔍 Verificar manualmente.
5. **Nunca invente resultado de MCP.** Se não chamou, ou a chamada falhou, o item é 🔍, nunca ✅.
6. **Só leitura, salvo aprovação.** Consultar, listar e ler pode. Alterar dados, aplicar migration, mudar configuração ou fazer deploy, só depois de eu aprovar.
7. **Falta de MCP não trava nada.** Sem MCP, audite pelo código e marque como 🔍 o que dependeria do painel. Se um MCP resolveria aquilo, diga em uma linha qual e o que ele responderia.
8. **Registre.** Os MCPs usados e os que faltam ficam em `auditorias/contexto.md`, na seção de ambiente.

A skill `01-ambiente` é quem levanta, testa e registra isso. As demais etapas só consomem o que estiver registrado.

## Protocolo de perguntas

Toda skill pergunta antes de trabalhar, mas só o que muda o resultado.

1. Leia `auditorias/contexto.md`. O que já estiver respondido lá **não se pergunta de novo**.
2. Descubra sozinho o que der para descobrir no código. Não pergunte o que está visível nos arquivos.
3. Faça no máximo **5 perguntas**, todas de uma vez, numeradas, cada uma com opções e uma **recomendação marcada**, com uma linha dizendo por quê.
4. Se o usuário não responder alguma, siga com a recomendação e registre no relatório como decisão assumida.
5. Grave as respostas em `auditorias/contexto.md`.

Criar ou atualizar `auditorias/contexto.md` é a única alteração que qualquer skill pode fazer sem aprovação explícita.

## Regras de execução

- **Termine dizendo o próximo comando.** Atualize `auditorias/roteiro.md` e feche a resposta com a próxima etapa, no formato `/nn-nome`, e o porquê em meia linha.
- **Não altere arquivos do projeto até o usuário aprovar.** Analise, relate, pergunte o que aplicar, e só então aplique. A exceção é o `contexto.md`.
- **Nunca reescreva histórico do Git** (`filter-repo`, `rebase`, `push --force`) por conta própria. Descreva o comando e deixe o usuário decidir.
- **Cite evidência concreta**: arquivo e linha, ou o trecho atual. Não chute.
- **Gatilhos**: cada checklist é agrupado por gatilho. Se o gatilho não existe no projeto, não analise o grupo — liste como N/A com o motivo em uma linha.
- **O que ainda não existe não é falha.** Registre como "Planejado" ou "Fora do escopo" e diga o que verificar quando for implementado. Um projeto simples e novo deve gerar relatório curto e cheio de N/A; isso é o esperado.
- **Siga o `CONVENCOES.md`.** Código novo respeita a estrutura de pastas e a nomenclatura registradas. Se o projeto não tem esse arquivo, siga as convenções que o código já usa e sugira criá-lo.
- **Estados de interface têm padrão próprio.** Ao construir tela, lista, formulário ou componente interativo, aplique a skill `06-interface`: carregamento, vazio, erro, sucesso, ação destrutiva, conteúdo extremo e campos de entrada.
- **Banco de dados tem padrão próprio.** Antes de criar ou alterar tabela, coleção, migration, query, índice ou regra de acesso, aplique a skill `banco`. Ela vale tanto para escrever quanto para auditar.
- **Confirme a API na documentação antes de escrever.** Biblioteca ou framework novo no projeto, arquivo de configuração, SDK, regra de segurança de serviço, ou uma tentativa que já falhou: consulte antes, pelo `.claude/lz/DOCUMENTACAO.md`. Código com API que aquela versão não tem é o erro mais caro de achar depois.
- **Mexeu, verifica.** Ao fim de qualquer mudança, rode a verificação rápida do projeto (comando na seção Testes do `CONVENCOES.md`; sem ela, suba o projeto como a skill `rodar` faz, e se o nível pede teste, monte a verificação com a skill `testes`). Não escreva código novo em cima de verificação vermelha. Bug ganha **primeiro** a verificação que falha, depois a correção. Nunca enfraqueça asserção nem atualize snapshot para o teste passar: se falhou, o culpado é o código até prova em contrário.
- **Aviso de dependência não passa batido.** Se uma instalação de pacotes reportar vulnerabilidade, ou se entrar uma dependência nova, aplique a skill `dependencias` na hora — sem esperar o usuário perguntar o que é aquele aviso vermelho. E o `rodar` confere as dependências uma vez por dia, em projeto novo ou antigo: advisory novo aparece só pelo tempo passar.
- **Não invente conteúdo do cliente**: nada de depoimentos, números, preços ou serviços que não estejam no site. Se faltar informação, liste as perguntas a fazer ao cliente.
- **Preços e limites de plano gratuito mudam.** Consulte a fonte oficial antes de citar valores e informe a data; sem acesso, marque como a confirmar em vez de chutar.

## Vocabulário

**Status:** ✅ OK · ⚠️ Parcial · ❌ Falha · 🔍 Verificar manualmente

**Severidade (segurança):** Crítica (explorável agora, vaza dados ou dinheiro) · Alta · Média · Baixa

**Impacto (acessibilidade):** Bloqueante (a pessoa não consegue usar) · Alto · Médio · Baixo

**Prioridade (demais):** Alta · Média · Baixa

## Formato do registro

Ao fim de cada etapa, depois das correções aprovadas, grave o registro da etapa com:

```markdown
# <Área> — registro
Data: AAAA-MM-DD

## Corrigido
- (item + o que foi feito)

## Pendente
- (item + severidade/impacto + por que ficou)

## Decidi não corrigir
- (item + motivo)

## Verificar manualmente
- (o que depende de painel, DNS ou teste humano)

## Pendências de domínio
- (itens adiados por ainda não haver domínio definitivo — a skill `13-dominio` fecha esta lista)
```

Esses registros são lidos pelo `12-pre-lancamento`, pela `nova-feature`, pela `14-revisao-geral` e pelo `projeto-limpo`. Registro que não existe significa etapa não rodada, e isso deve ser dito, não suposto.
