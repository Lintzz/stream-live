---
name: 00-planejar
description: "Ponto de partida de projeto novo, antes de existir codigo. Parte de um design em HTML (exportado do Claude Design) ou so da ideia descrita pelo usuario - nesse caso repete a ideia, corta o escopo em v1, depois e nao vai ter, e deriva as telas e os dados. Depois transforma isso em plano de projeto: analisa o design ou a ideia, pergunta o que decide a stack, recomenda plataforma, framework, banco, autenticacao, e-mail e pagamento, e entrega roteiro de execucao com custos. Use no inicio do projeto, antes de existir codigo, quando o usuario disser que vai comecar um site ou app, que teve uma ideia, que quer criar algo do zero, converter um design, montar a stack ou planejar um projeto novo."
---

# Planejamento do Projeto (a partir do design ou da ideia)

Leia primeiro `.claude/lz/FLUXO.md`: ele define a ordem das etapas, o protocolo de perguntas, o arquivo `auditorias/contexto.md`, o vocabulario de status e o formato do registro. As regras de la valem aqui.

Você vai me ajudar a transformar o que eu tenho em um **plano de projeto**: qual plataforma, qual stack, quais serviços externos, em que ordem construir e quanto isso custa por mês.

**Duas entradas possíveis — descubra qual antes de começar:**

| O que eu tenho | Caminho |
|---|---|
| **Design pronto em HTML** (exportado do Claude Design) | Fases 0 e 1 abaixo: inventário do design |
| **Só a ideia**, descrita na conversa | Leia `.claude/skills/00-planejar/references/ideia.md` e siga os passos de lá no lugar das Fases 0 e 1: repetir a ideia, perguntar, cortar o escopo, derivar telas e dados |

Se eu chamei o comando sem design e sem descrever nada, pergunte: tenho um design em HTML, ou vou contar a ideia? Se já existe código, esta não é a porta: é `/00-diagnosticar`.

Da Fase 2 em diante os dois caminhos se juntam. No caminho da ideia, a "Fase 4: plano de conversão do design" não existe: as telas saem da descrição direto no `03-desenvolver`.

Nesta etapa você **não escreve código do projeto** e não cria arquivos, exceto os entregáveis do final, depois que eu aprovar. Seu papel é analisar, perguntar, recomendar e me entregar um roteiro.

Regra que vale para todas as recomendações: **a menor complexidade que resolve o problema**. Landing page de um cliente pequeno não precisa de framework, banco de dados nem painel administrativo. Só suba de degrau quando houver um motivo concreto que eu confirme.

## Fase 0: Leia o design e faça o inventário (caminho do design)

Leia o HTML do design e monte um **Inventário**, sem inventar nada que não esteja lá:

**Estrutura**
- Seções na ordem em que aparecem (hero, serviços, depoimentos, FAQ, rodapé...)
- Elementos que se repetem e virariam componentes (cards, itens de lista, botões)
- Páginas ou telas: é uma página só, ou o design sugere várias?

**Interatividade**
- Formulários, com todos os campos e tipos
- Botões e links, e para onde apontam
- Menu, modal, carrossel, abas, acordeão, animações
- Qualquer coisa que dependa de JavaScript para funcionar

**Conteúdo que parece dinâmico** (o mais importante desta fase)
- Listas que provavelmente mudam com o tempo: produtos, serviços, posts, eventos, galeria, depoimentos
- Áreas de área logada, painel, carrinho, agendamento, busca, filtro
- Números e textos que parecem vir de algum lugar (estoque, preço, disponibilidade)
- Marque o que é **claramente placeholder**: lorem ipsum, imagem genérica, dado fictício

**Visual**
- Cores e fontes usadas (vão virar tokens no projeto)
- Imagens: embutidas em base64, referenciadas por URL externa, ou faltando?
- Recursos que podem dar trabalho ao converter: animações complexas, `100vh`, CSS muito específico, ids repetidos

Ao final, classifique cada parte em **estático** (só HTML e CSS), **interativo no navegador** (só JavaScript, sem servidor) ou **precisa de backend** (guardar, autenticar, cobrar, enviar).

## Fase 1: Diagnóstico inicial (caminho do design)

Com base só no inventário, diga:

1. **Que tipo de produto isto parece ser:** landing page, site institucional de várias páginas, site com blog, aplicação web com login, e-commerce, painel interno, aplicativo mobile ou desktop.
2. **O design é de tela grande, de celular, ou dos dois?**
3. **O que no design já indica necessidade de backend** e o que pode viver sem ele.
4. **O que provavelmente falta no design** para o site funcionar de verdade (página de obrigado, estado de erro do formulário, página 404, política de privacidade, versão mobile de alguma seção).

Diga também o que você **assumiu** e não tem como saber pelo HTML.

## Fase 2: Pergunte o que decide o projeto

Agora faça as perguntas. No caminho da ideia, as perguntas de `references/ideia.md` entram aqui, primeiro, e contam no total. No máximo **8**, todas de uma vez, numeradas, agrupadas por tema, cada uma com opções e a sua **recomendação marcada**, com uma linha dizendo por quê. Se eu não responder alguma, siga com a recomendação e registre isso como decisão assumida.

Pergunte só o que muda o plano. Exemplos do que costuma importar:

**Plataforma**
- É site (abre no navegador), aplicativo instalável no celular, ou programa de computador? Se o design é claramente de site, confirme rápido em vez de gastar uma pergunta.
- Precisa funcionar offline ou acessar câmera, notificações, arquivos do aparelho?

**Conteúdo**
- O conteúdo muda com que frequência, e quem muda: eu, o cliente, ou ninguém?
- Se o cliente edita: ele topa mexer num painel, ou prefere me pedir?

**Funcionalidades**
- Precisa de login? Quantos tipos de usuário?
- Precisa guardar alguma informação entre visitas? Qual?
- O formulário só precisa **avisar** alguém (e-mail, WhatsApp) ou precisa **ficar registrado**?
- Vai receber pagamento? Avulso ou recorrente?

**Perfil e alcance**
- **É projeto seu ou de cliente? Outras pessoas vão usar?** Use os perfis A, B, C e D do FLUXO — é o que decide quais auditorias entram no roteiro. Se for ferramenta sua, de uso próprio, diga que SEO, LGPD, conversão e acessibilidade saem do roteiro, e por quê.

**Restrições**
- Prazo.
- Posso usar serviços pagos ou o projeto precisa caber em planos gratuitos?
- Quem vai manter o projeto depois: eu, o cliente, ou ninguém?

**Domínio e hospedagem** (assuma que não existe nada até eu dizer o contrário)
- O cliente já comprou domínio? Se não, tudo bem: o projeto começa em `preview`, numa URL temporária, e o domínio entra depois.
- Já existe hospedagem contratada, ou eu escolho?
- Precisa ter e-mail no domínio (`contato@...`)? É serviço à parte.

**Sobre mim**
- Se houver mais de um caminho bom, considere que eu quero aprender, mas que entregar funcionando vem primeiro. Pergunte se prefiro o caminho mais simples ou o mais interessante de estudar, quando essa escolha existir de verdade.

## Fase 3: Recomende a stack

Com as respostas, recomende uma stack. Sempre **duas opções**: a **recomendada** e uma **alternativa mais simples**, dizendo o que se perde. Justifique cada escolha em uma frase, ligada a uma necessidade real que eu confirmei — nunca "porque é moderno".

Cubra, quando fizer sentido:

- **Front-end / framework:** do mais simples ao mais complexo, subindo só com motivo. HTML, CSS e JS puro (ou Astro) para site estático; Next.js quando houver várias páginas com SEO, rotas e backend leve no mesmo projeto; React com Vite quando for aplicação logada sem necessidade de SEO.
- **Estilo:** manter o CSS do design, ou converter para Tailwind ou outra abordagem. Considere o custo de retrabalho: o design já vem com CSS pronto.
- **Backend:** funções serverless antes de servidor próprio. Diga o que precisa rodar no servidor e por quê.
- **Banco de dados:** só se houver dado para guardar. Compare as opções pelo que o projeto pede (relacional, tempo real, autenticação junto, facilidade de painel para o cliente).
- **Autenticação:** provedor pronto em vez de login feito à mão, e quais formas de entrar fazem sentido aqui.
- **Envio de e-mail:** separe dois casos, porque são coisas diferentes: formulário de contato simples (serviço de formulário resolve, sem backend) e e-mail transacional do sistema (confirmação, recuperação de senha), que precisa de serviço próprio e configuração de domínio.
- **Pagamentos:** considere meios de pagamento locais (Pix, boleto, parcelamento) quando o público for brasileiro, e o suporte a recorrência quando for assinatura. Diga o que o gateway exige do cliente (CNPJ, conta bancária, verificação) — isso costuma atrasar o projeto.
- **CMS:** só se o cliente for editar sozinho. Se não for, conteúdo no código é mais simples e mais barato.
- **Hospedagem e domínio.** Leia `.claude/skills/00-planejar/references/hospedagem.md` e siga o critério de decisão de lá. Se não houver domínio ainda, recomende começar em `preview` na URL temporária da plataforma e deixe o domínio como etapa separada — não trate a ausência de domínio como problema.
- **Analytics.**

Para cada serviço externo, informe: **para que serve neste projeto**, se tem plano gratuito, **qual limite do plano gratuito pode ser estourado** e o custo estimado se passar.

**Importante:** preços, limites de plano gratuito e nomes de plano mudam com frequência. Não confie na sua memória — consulte a página oficial de preços de cada serviço que você recomendar e diga a data da consulta. Se não conseguir consultar, marque **🔍 Confirmar preço** em vez de chutar um número.

## Fase 3.5: MCPs que valem a pena para esta stack

Com a stack definida, diga **quais MCPs ajudariam neste projeto** e o que cada um resolveria de concreto: banco, hospedagem, repositório, documentação oficial das bibliotecas escolhidas, gateway de pagamento, monitoramento de erros.

Não teste nem conecte nada aqui — quem faz isso é a skill `01-ambiente`, que deve rodar logo depois desta. Apenas deixe a lista pronta para ela, ordenada por utilidade real.

## Fase 4: Plano de conversão do design para o projeto (só no caminho do design)

Explique como o HTML do design vira o projeto de verdade:

1. **Tokens:** cores, fontes, espaçamentos e raios extraídos do design para um lugar só.
2. **Componentização:** quais trechos repetidos viram componentes, com os nomes sugeridos.
3. **Conteúdo:** o que sai do HTML e vira dado (arquivo de conteúdo, banco ou CMS) e o que continua sendo markup.
4. **Imagens:** o que precisa ser substituído por imagem real, exportado em formato adequado, e o que fazer com imagens embutidas em base64 (costumam ter que sair).
5. **Pontos de atenção da conversão:** animações que podem não sobreviver, CSS que vai conflitar, ids repetidos, responsividade que o design não cobriu.
6. **O que ainda falta desenhar** antes de programar (estados de erro, página de obrigado, 404, telas de e-mail).

## Fase 4.5: Estrutura de pastas e convenções

Leia `.claude/skills/00-planejar/references/estrutura.md` e proponha a organização do projeto **para a stack escolhida**: a árvore de pastas com uma linha explicando cada uma, as convenções de nome, o idioma do código, onde entra cada tipo de arquivo novo e onde ficam os tokens de design.

Decida, não liste opções. Se o framework já tem convenção própria, siga a dele em vez de inventar outra.

Se o projeto usa banco de dados, acrescente a convenção de nomes de tabelas e colunas, seguindo a skill `banco`.

## Fase 5: Roteiro de execução

Monte as etapas na ordem de construção, cada uma com um **entregável verificável**. A primeira etapa deve chegar rápido a algo visível no ar. Algo como:

1. Projeto criado, design convertido (ou, no caminho da ideia, a primeira tela do fluxo principal funcionando), layout no ar na URL temporária (estágio `preview`, com `noindex` enquanto for rascunho para o cliente ver)
2. Conteúdo real no lugar dos placeholders
3. Formulário funcionando de ponta a ponta
4. Funcionalidade principal (login, catálogo, pagamento, o que for)
5. Ajustes e as auditorias
6. Domínio comprado e apontado (skill `13-dominio`), revisão geral (`14-revisao-geral`) e, se for de cliente, a versão de entrega (`projeto-limpo`)

Inclua também:

- **Contas e serviços a criar antes de começar**, dizendo **quem cria**: o que deve estar no nome do cliente desde o início (domínio, gateway de pagamento, contas do Google) para não precisar transferir depois.
- **Variáveis de ambiente previstas**, só os nomes.
- **Custo mensal estimado**, somando os serviços.
- **Verificação automática:** o nível de teste pelo perfil (tabela no FLUXO) e a etapa em que ela nasce — a primeira com regra de negócio, dado ou login. O `03-desenvolver` monta sozinho nessa etapa; aqui é só deixar escrito no roteiro.
- **Riscos:** o que pode travar o projeto (cliente demorar para mandar conteúdo, verificação do gateway, foto de qualidade ruim, recurso que o design pede e é caro de fazer).
- **O que eu recomendo deixar de fora desta versão** e por quê. Cortar escopo é parte do plano.

## Como conduzir

- Pergunte antes de recomendar. Não monte a stack em cima de suposição.
- Prefira o simples. Se a resposta certa for "isso não precisa de banco de dados", diga isso com todas as letras.
- Não recomende tecnologia que o projeto não vai usar de verdade.
- Ao citar preço ou limite de plano, consulte a fonte oficial e diga a data, conforme a Fase 3.

## Formato da resposta

**Caminho do design:**

1. **Inventário do design** (Fase 0)
2. **Diagnóstico** (Fase 1)
3. **Perguntas** (Fase 2) — e **pare aqui** para eu responder

**Caminho da ideia:**

1. **A ideia em um parágrafo** — e pare para eu confirmar
2. **Perguntas** (as da ideia e as da Fase 2, numa rodada só) — e pare
3. **Núcleo, escopo, telas, dados e riscos** (`references/ideia.md`) — e pare para eu confirmar o escopo

Depois, nos dois caminhos:

4. **Stack recomendada**, em tabela: `Camada | Recomendado | Alternativa mais simples | Por quê | Custo`
5. **Estrutura de pastas e convenções**
6. **Plano de conversão do design** (só no caminho do design)
7. **Roteiro de execução** por etapas
8. **Contas a criar, custos, riscos e o que cortar**

Ao final, pare e pergunte se aprovo o plano.

## Registro

Depois que eu aprovar, crie quatro arquivos:

1. **`PLANO.md`** na raiz do projeto: a stack escolhida, o roteiro por etapas e o que ficou de fora. No caminho da ideia, antes da stack, também as seções **Núcleo**, **Escopo** (v1 / Depois / Não vai ter), **Telas** e **Dados** — são elas que o `03-desenvolver` usa no lugar do design.
2. **`auditorias/roteiro.md`** no formato do FLUXO: perfil escolhido, todas as etapas deste projeto na ordem, com o comando exato de cada uma, e a lista do que não se aplica com o motivo. Mostre esse roteiro na resposta, não só no arquivo.
3. **`CONVENCOES.md`** na raiz: a estrutura de pastas e as convenções da Fase 4.5, escritas como decisão do projeto. É o arquivo que a skill `nova-feature` lê para manter o padrão quando o projeto crescer.
4. **`auditorias/contexto.md`**: data, perfil, tipo de projeto, cliente ou projeto próprio, estágio (`rascunho`, `preview` ou `producao`), URL atual, domínio previsto ou "ainda não comprado", hospedagem, serviços escolhidos, o que existe hoje, o que está planejado e ainda não foi feito, e as decisões que eu tomei nas perguntas. Uma linha por informação.

No `auditorias/contexto.md`, preencha também as seções **Identidade** (dono do produto, nome, id do app — avisando que o id é imutável depois de publicado em loja) e **Como rodar e gerar build** com os comandos previstos para a stack escolhida, seguindo `.claude/lz/DETECCAO.md`. Depois do planejamento aprovado, recomende a sequência: `01-ambiente` (conectar os MCPs), `02-configurar` (metadados, Git e política de versão) e então `03-desenvolver`.

O `auditorias/contexto.md` é lido por todas as auditorias seguintes, então elas não vão repetir estas perguntas. Mantenha-o atualizado conforme o projeto andar.
