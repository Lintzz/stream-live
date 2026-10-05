---
name: nova-feature
description: "Planeja e valida qualquer coisa nova adicionada a um projeto que ja passou pelas auditorias - uma pagina, uma secao, uma integracao de API, um formulario, login, pagamento, uma tela nova do app. Faz perguntas, monta o plano, implementa depois de aprovado e reaplica somente as auditorias que a novidade reabre, mantendo o padrao ja estabelecido. Use quando o usuario disser que quer adicionar, incluir, criar ou implementar algo novo em um projeto existente."
---

# Nova funcionalidade em projeto auditado

Leia primeiro `.claude/lz/FLUXO.md`: ele define a ordem das etapas, o protocolo de perguntas, o arquivo `auditorias/contexto.md`, o vocabulário de status e o formato do registro. As regras de lá valem aqui.

Esta skill existe porque um projeto auditado não pode perder o padrão quando cresce. Ela planeja o que vai ser feito, e depois confere o que foi feito **contra as auditorias que aquela novidade reabre** — não todas, só as que fazem sentido.

Você **não escreve código antes de eu aprovar o plano**.

## Fase 0: Entenda o projeto e o que já foi decidido

Leia, na ordem:

1. `auditorias/contexto.md` — o que o projeto é, o que já existe, o que estava planejado
2. `PLANO.md` — stack e escopo definidos
3. `CONVENCOES.md` — estrutura de pastas e nomenclatura que a novidade precisa respeitar
4. Todos os registros em `auditorias/` — o que já foi corrigido, o que ficou pendente e o que decidi não corrigir
5. `auditorias/features/` — funcionalidades adicionadas depois

Se a pasta `auditorias/` não existir, avise: o projeto nunca passou pelo fluxo, então não há padrão registrado para manter. Recomende rodar `00-diagnosticar` antes — ele descreve o padrão que o código já segue. Se o usuário quiser seguir assim mesmo, siga as convenções que o código já usa.

Em seguida, olhe o código para entender **as convenções já usadas**: estrutura de pastas, nomes, como os componentes são escritos, como a validação é feita hoje, onde ficam os tokens de cor e tipografia, como os formulários existentes tratam erro. A novidade tem que parecer parte do mesmo projeto.

## Fase 1: Entenda o pedido e pergunte

Reformule com suas palavras o que eu pedi, para eu confirmar que entendemos a mesma coisa.

Depois siga o protocolo de perguntas do FLUXO: no máximo 5, todas de uma vez, com opções e recomendação marcada. Pergunte só o que muda o resultado. Conforme o caso:

- Essa novidade precisa guardar alguma informação? Qual?
- Quem vai poder ver ou usar isso: qualquer visitante, só usuário logado, só administrador?
- Coleta algum dado pessoal novo?
- Precisa de serviço externo novo? Tenho conta nele?
- É para esta entrega ou pode ficar para depois?
- Onde isso entra na navegação do site?

Grave as respostas em `auditorias/contexto.md`.

## Fase 2: Análise de impacto

Diga **quais auditorias esta novidade reabre**, e apenas essas. Use a tabela abaixo; se a novidade se encaixar em mais de uma linha, some as auditorias.

| O que está sendo adicionado | Reabre |
|---|---|
| Página ou tela nova | interface (carregando, vazio, erro, conteúdo extremo), conversão (CTA e hierarquia), seo (title, description, canonical, sitemap, links internos), acessibilidade (semântica, headings, foco), performance (imagens) |
| Seção ou bloco de conteúdo | conversão, seo (headings, alts), acessibilidade (contraste, ordem) |
| Formulário ou campo novo | interface (erro inline, botão em loading, sucesso, `inputmode`, mostrar senha), segurança (validação no servidor, XSS, bot), lgpd (dado coletado, minimização, aviso de privacidade), acessibilidade (label, erro, autocomplete), conversão |
| Integração de API ou serviço externo | segurança (chave no servidor, CORS, rate limit, resposta enxuta), performance (tempo de resposta, script de terceiro), git (nova variável de ambiente, `.env.example`) |
| Lista, tabela ou painel com dados | interface (skeleton, estado vazio, paginação, texto longo), performance (consulta e paginação) |
| Ação de excluir, cancelar ou desfazer | interface (confirmação ou desfazer), segurança (autorização no servidor, IDOR) |
| Tabela ou coleção nova no banco | **skill `banco`** (padrão de nomes, tipos, constraints, índices, regra de acesso), segurança (query parametrizada, RLS, acesso por dono, IDOR), lgpd (dado pessoal, retenção) |
| Login, cadastro ou perfil | segurança (autorização no servidor, sessão, rate limit), lgpd (termos, exclusão de conta, correção de dados), acessibilidade (fluxo por teclado) |
| Pagamento, assinatura ou doação | segurança (valor no servidor, assinatura do webhook, idempotência), lgpd (recorrência clara, cancelamento, arrependimento) |
| Upload de arquivo | segurança (tipo real, tamanho, nome, onde grava), lgpd se o arquivo tiver dado pessoal |
| Imagem, vídeo ou mídia nova | performance (formato, tamanho, lazy, dimensão), seo (alt, nome do arquivo) |
| Biblioteca ou dependência nova | **skill `dependencias`** (aviso da instalação, advisory, nome do pacote, `postinstall`), performance (peso no bundle) |
| Analytics, pixel ou chat | lgpd (consentimento antes de carregar), performance (script bloqueando), conversão (evento de conversão) |
| Só texto ou troca de imagem | seo, conversão |
| Blog ou conteúdo publicável | seo (estrutura, schema de artigo), performance, acessibilidade |

Para cada auditoria reaberta, diga **em uma linha o que exatamente vai ser verificado** depois — não a auditoria inteira, só o recorte que a novidade toca.

Se a novidade **muda uma decisão estruturante** do projeto — trocar onde os dados moram, passar de uso local para vários usuários, adicionar login onde não havia —, diga isso com todas as letras: não é só uma feature, é uma mudança de fundação. Nesse caso, siga a seção "Mudança grande" da Fase 3.

**Não recuse o que é dependência.** Mudanças assim arrastam outras junto, e isso é correto: migrar para um banco com RLS por usuário exige autenticação, porque sem usuário autenticado a policy não tem como isolar nada. Traga a dependência para o plano em vez de fingir que dá para fazer sem ela — e diga por que ela entrou.

Verifique também se a novidade **conflita com alguma decisão registrada** — inclusive a lista "Não vai ter" do `PLANO.md`, se o projeto nasceu da ideia: algo que eu decidi não fazer, um serviço que eu escolhi não usar, um escopo que ficou de fora. Se conflitar, avise antes de continuar.

## Fase 2.5: Carregue os padrões e o ambiente — sem o usuário pedir

Antes de planejar, carregue sozinho o que a novidade exige. **Não peça ao usuário para rodar comando nenhum.**

| Se a novidade envolve | Leia e siga |
|---|---|
| Banco de dados (tabela, coleção, migration, query, índice, RLS, regra do Firestore) | `.claude/skills/banco/SKILL.md` |
| Tela, lista, formulário, botão ou componente interativo | `.claude/skills/06-interface/SKILL.md` |
| Serviço externo novo, ou um MCP que o projeto ainda não usa | `.claude/skills/01-ambiente/SKILL.md`, **modo preparar** — conecte e teste a ferramenta agora, antes de planejar em cima dela |
| Biblioteca ou framework que o projeto ainda não usa, ou uma API que você não confirmou | `.claude/lz/DOCUMENTACAO.md` — confirme na documentação da versão **antes** de escrever o plano, não depois de o código falhar |
| Dependência nova a instalar | `.claude/skills/dependencias/SKILL.md` — logo depois de instalar, antes de escrever código em cima |
| Projeto de nível 2 ou 3 sem verificação automática | `.claude/skills/testes/SKILL.md`, **modo montar** — a verificação mínima entra no plano como primeira etapa, para a novidade já nascer com caso |

Diga em uma linha o que carregou e por quê. Se o ambiente não puder ser ligado, siga com o que der e registre o que ficou pendente.

Um ganho concreto de consultar na Fase 2.5 e não na hora do erro: o plano muda. Saber, antes de planejar, que a versão instalada do SDK pede outra forma de inicializar, ou que aquela biblioteca não é compatível com a versão do framework, evita um plano aprovado que não sobrevive à primeira etapa.

## Fase 3: Plano de implementação

Entregue:

- **Arquivos que vão ser criados ou alterados**, com o caminho
- **Como se encaixa nas convenções do projeto** — respeitando o `CONVENCOES.md`: onde os arquivos novos vão, com que nomes, usando os tokens e o padrão de validação existentes
- **Variáveis de ambiente novas**, só os nomes, e a atualização do `.env.example`
- **Serviços ou contas que preciso criar antes**, e em nome de quem
- **Etapas na ordem**, cada uma com algo verificável no final
- **O que pode quebrar** no que já existe
- **O que sugiro deixar de fora** desta versão

### Mudança grande: um plano, várias etapas

Quando a mudança é estruturante, o plano continua sendo **um só** e cobre tudo. O que muda é a execução: em etapas, com parada entre elas.

1. **Divida em etapas**, cada uma com algo verificável no fim e com a **dependência declarada** ("depende da etapa 2 porque a policy precisa do usuário autenticado"). Ordem típica de uma migração de banco: estrutura e regras de acesso → autenticação → migrar os dados existentes → trocar leitura e escrita do app → limpar o que ficou para trás.
2. **Mostre a lista** e pergunte: executar **uma etapa por vez, parando para eu conferir** (recomendado), ou tudo seguido?
3. **Ao fim de cada etapa:** rode a verificação do projeto (seção Testes do `CONVENCOES.md`; sem ela, suba o projeto como a skill `rodar` faz), confira o que aquela etapa entregou, faça os commits daquela etapa, e — no modo com parada — mostre o resultado e espere antes da próxima. Verificação vermelha trava a etapa seguinte.
4. **Dado que já existe é o ponto de maior risco.** Na etapa de migração: faça backup antes, **mantenha a origem intacta e funcionando** até a conferência passar, compare a contagem de registros no fim, e só então pare de usar a origem. Não apague nada sem eu aprovar.
5. **Se uma etapa falhar**, pare nela. Não siga para a próxima "para consertar depois".

Pare aqui e pergunte se aprovo. Só depois implemente.

## Fase 4: Verificação da novidade

Com a implementação pronta, aplique **apenas os recortes de auditoria listados na Fase 2**, olhando só o código novo ou alterado. Não refaça as auditorias inteiras.

Para cada item, use o padrão que o projeto já adotou como referência: se os formulários existentes validam com um esquema, o formulário novo valida do mesmo jeito; se as imagens existentes são WebP com dimensão declarada, a nova também é.

Verifique ainda, sempre:

- A novidade não expôs segredo nem adicionou variável pública com valor sensível
- O `.gitignore` continua cobrindo o que precisa
- Nada quebrou no que já estava funcionando: **a verificação completa do projeto passa**, além de links, build e console sem erro
- **A novidade entrou na verificação.** Fluxo novo ganha caso, no nível que o projeto adotou — é a regra da seção Testes do `CONVENCOES.md`. Feature sem caso é feature que a próxima mudança pode quebrar em silêncio
- Se há registro de pré-lançamento aprovado, diga se esta novidade **invalida o veredito** e precisa de nova rodada antes de publicar

Relate no formato: `# | Item | Auditoria de origem | Status | Evidência | Correção sugerida`, com os status e severidades do FLUXO.

Pare e pergunte o que devo corrigir.

## Registro

Depois das correções aprovadas:

1. Crie `auditorias/features/<nome-da-feature>.md` com: data, o que foi adicionado, decisões tomadas, auditorias reaplicadas e resultado, pendências e o que decidi não corrigir. **Em mudança grande**, o arquivo é um só, com uma seção por etapa, atualizada ao fim de cada uma — assim o registro existe mesmo se o trabalho parar no meio.
2. Atualize `auditorias/contexto.md`: mova a novidade de "Planejado" para "O que existe hoje" e acrescente serviços ou variáveis novas.
3. Diga que tipo de versão a novidade representa pelo `.claude/lz/VERSIONAMENTO.md` e pelos exemplos do `CONVENCOES.md` (correção, menor ou maior), para a próxima build de entrega. Escreva os commits no formato Conventional Commits.
4. Se a verificação encontrou pendência de uma auditoria já registrada, acrescente a pendência **também** ao registro daquela auditoria, com a data, para que ela não se perca.
