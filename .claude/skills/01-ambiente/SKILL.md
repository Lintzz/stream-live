---
name: 01-ambiente
description: "Deixa o ambiente pronto para trabalhar: conecta e testa os MCPs que o projeto precisa. Em modo preparar, conduz a instalacao e a autenticacao de um MCP novo (Supabase, Vercel, GitHub, Firebase, documentacao) ate ele responder de verdade; em modo conferir, levanta o que ja existe e o que falta. Carregue esta skill sozinho, sem o usuario pedir, sempre que o trabalho trouxer um servico externo novo. Use tambem quando o usuario falar em MCP, conectar servico, token, autenticacao de ferramenta ou perguntar se as conexoes estao funcionando."
---

# Ambiente de trabalho: MCPs, acessos e ferramentas

Leia primeiro `.claude/lz/FLUXO.md`: ele define a ordem das etapas, o protocolo de perguntas, o arquivo `auditorias/contexto.md`, o vocabulário de status e o formato do registro. A seção **Uso de MCPs** de lá governa esta skill.

Sem isso, as outras etapas trabalham no escuro: leem o código e supõem o que está configurado no painel. Com os MCPs certos conectados, elas conferem o que realmente está lá.

Esta skill tem dois modos, e você decide pelo pedido:

- **Modo preparar** — o projeto vai ganhar um serviço novo (o usuário disse que vai usar Supabase, Vercel, Firebase...), ou uma skill como a `nova-feature` carregou esta aqui no meio do trabalho. Aqui você **não faz relatório**: você deixa a conexão funcionando e volta para o trabalho que estava sendo feito. Vá direto para a seção "Modo preparar".
- **Modo conferir** — o usuário quer o retrato do ambiente. Siga as fases 0 a 4.

Você pode ser carregada no meio de outra skill. Nesse caso, resolva a conexão e devolva o controle: não recomece o fluxo nem peça para o usuário rodar comando nenhum.

## Modo preparar: ligar um serviço novo

1. **Identifique o serviço** e veja se já existe MCP dele nesta sessão.
2. **Se já existe**, teste com uma chamada de leitura. Se responder, confirme qual projeto ou organização está selecionado, registre no contexto e siga o trabalho.
3. **Se não existe**, conduza a instalação, sem empurrar o problema para o usuário:
   - Use as ferramentas de busca e sugestão de conectores da sessão, se houver.
   - Senão, dê o comando pronto para o ambiente em uso (no Claude Code, `claude mcp add ...`, ou a edição do arquivo de configuração de MCPs), diga exatamente o que o usuário precisa ter em mãos (token de acesso, id do projeto, onde gerar cada um) e **espere ele confirmar** que fez.
   - Prefira sempre a permissão mínima: token só de leitura quando o trabalho for só ler, e escopo limitado ao projeto.
4. **Teste de novo.** Enquanto a chamada de leitura não responder, o MCP não está pronto: diga o erro e o próximo passo, em vez de seguir supondo.
5. **Diga o que muda no trabalho** agora que ele está ligado — por exemplo, que as policies passam a ser conferidas no banco real em vez de deduzidas pelo código.
6. **Se não der para conectar agora**, siga sem ele: o que dependeria do painel vira 🔍 no relatório, e isso fica escrito.

Em qualquer caso, registre o resultado em `auditorias/contexto.md`, seção Ambiente.

Rode em modo conferir depois do `00-planejar` e antes do `02-configurar`; o modo preparar acontece sempre que um serviço novo entra.

## MCP de documentação: obrigatório, em todo projeto

Este é o único MCP que **não** é opcional, e vale igual para projeto novo e para projeto antigo que nunca usou. Sem ele, todo código sai da memória do modelo, que é de uma versão qualquer da biblioteca — e o projeto usa uma versão específica.

O padrão do kit é o **Context7**. Faça, nesta ordem:

1. **Veja se já está na sessão**: existem as ferramentas `resolve-library-id` e `query-docs`?
2. **Teste**: resolva o id de uma biblioteca que o projeto realmente usa e peça uma resposta curta. Ferramenta listada não é conexão funcionando.
3. **Se não existe, instale** — não deixe para depois e não pergunte se o usuário quer:

   ```
   npx ctx7 setup --claude
   ```

   Ele autentica, gera a chave e registra tudo. Se esse comando falhar ou o ambiente não for o Claude Code, use o servidor remoto direto:

   ```
   claude mcp add --transport http context7 https://mcp.context7.com/mcp --header "Authorization: Bearer $CONTEXT7_API_KEY"
   ```

   A chave sai de `context7.com`; local, via `npx -y @upstash/context7-mcp`, também funciona. Se nenhuma dessas formas valer mais, confira a página de instalação atual em vez de insistir num comando que não roda.
4. **Se for um serviço com MCP próprio** (Supabase, Firebase, Vercel, GitHub), o MCP dele **não substitui** o de documentação: o do serviço responde sobre o seu projeto, o de documentação responde sobre a API. Instale os dois.
5. **Se não der para instalar agora**, siga — mas escreva no relatório que o código vai ser escrito sem confirmação na documentação, e que isso é a causa provável se aparecer erro de API inexistente.

Depois de ligado, as regras de uso estão em `.claude/lz/DOCUMENTACAO.md`: o que é obrigatório consultar, o que não precisa, e o que fazer quando a documentação não tem a resposta.

## Dependências na primeira instalação

Se esta é a primeira vez que os pacotes do projeto são instalados, ou se a instalação reportar vulnerabilidade, carregue `.claude/skills/dependencias/SKILL.md` e faça a triagem agora. Projeto que nasce com vulnerabilidade herdada custa muito menos para arrumar aqui do que na auditoria de segurança, quando já há código escrito em cima.

## Fase 0: Descubra o que o projeto precisa

Leia `auditorias/contexto.md` e `PLANO.md` para saber a stack. Se não existirem, descubra pelo código: `package.json`, `.env.example`, configs de deploy, imports.

Monte a lista de **serviços que o projeto usa ou vai usar**: banco, autenticação, hospedagem, repositório, e-mail, pagamento, analytics, CMS, storage.

## Fase 1: Veja o que já está conectado

Verifique quais ferramentas MCP existem nesta sessão e relacione cada uma com os serviços da Fase 0.

Para cada MCP relacionado ao projeto, **teste com uma chamada de leitura simples** — listar projetos, listar tabelas, ler um registro. Não confie na presença da ferramenta: ferramenta listada e conexão funcionando são coisas diferentes. Classifique:

- ✅ **Conectado e respondendo** — testado agora, com o que a chamada retornou
- ⚠️ **Conectado mas falhou** — diga o erro e o que provavelmente resolve (token expirado, projeto errado, permissão)
- ❌ **Não conectado** — existe MCP para esse serviço, mas não está nesta sessão

Se um MCP der acesso a mais de um projeto ou organização, confirme **qual** está selecionado. Apontar para o projeto errado é pior do que não ter o MCP.

## Fase 2: Recomende o que falta

Para cada serviço da Fase 0 sem MCP conectado, diga **o que o MCP resolveria neste projeto**, de forma concreta. Alguns exemplos do tipo de ganho que vale mencionar:

- **Banco de dados** — conferir se as políticas de acesso estão realmente ativas, ver o schema real, checar migrations aplicadas em produção. Sem ele, a auditoria de segurança só consegue inferir pelo código e marca vários itens como 🔍.
- **Hospedagem** — ver variáveis de ambiente configuradas, headers que estão realmente sendo servidos, qual branch publica, se preview deployments estão expostos.
- **Repositório** — issues, pull requests, configuração do Actions, visibilidade do repositório.
- **Documentação** — consultar a documentação oficial da versão que o projeto usa, em vez de responder de memória. É o que mais evita código desatualizado. **Este não entra na lista de recomendações: é obrigatório e já foi resolvido na seção acima.**
- **Pagamento** — conferir webhooks cadastrados, se as chaves são de teste ou produção, eventos recebidos.
- **Monitoramento de erros** — erros reais acontecendo no site publicado.

Ordene por utilidade real para **este** projeto, não em lista genérica. Um site estático sem banco não precisa de MCP de banco.

Para os que eu quiser conectar, use as ferramentas de busca e sugestão de conectores disponíveis na sessão. Se não houver, explique em uma linha onde se conecta e o que vou precisar ter em mãos (token, projeto, permissão).

Não insista: se eu disser que não quero conectar algum, registre a decisão e siga. Falta de MCP não trava nada, só aumenta o número de itens marcados como 🔍.

## Fase 3: Acessos e ferramentas locais

Confira e registre, sem expor nenhum valor:

- **Acessos que eu tenho**: painel da hospedagem, DNS, banco, gateway, contas do Google. As auditorias usam isso para decidir entre verificar de verdade ou marcar 🔍.
- **Variáveis de ambiente**: quais o código espera (`.env.example`) e quais estão definidas localmente — só os nomes, nunca os valores. Aponte as que faltam.
- **Ferramentas de linha de comando** que o projeto precisa e se estão instaladas (gerenciador de pacotes, CLI da hospedagem, CLI do banco, git).

## Fase 4: Perguntas

Siga o protocolo do FLUXO. Pergunte só o que não descobriu sozinho. Conforme o caso:

1. Quais desses MCPs você quer conectar agora?
2. Quando um MCP dá acesso a vários projetos, qual é o deste trabalho?
3. Você tem acesso ao painel da hospedagem e ao DNS, ou quem tem é o cliente?
4. Prefere que eu use os MCPs para **ler e conferir** apenas, ou também para **aplicar** mudanças quando você aprovar?

A resposta da pergunta 4 vale para o projeto todo. O padrão, se você não responder, é **somente leitura**.

## Formato do relatório

1. **MCP de documentação** — ligado e testado, ou o que faltou para ligar
2. **Serviços do projeto** e o MCP correspondente
3. **Tabela:** `Serviço | MCP | Status | Testado com | O que ele resolve aqui`
   - Status: ✅ Conectado e respondendo / ⚠️ Conectado mas falhou / ❌ Não conectado / — Não existe MCP
4. **Recomendo conectar**, em ordem de utilidade, com o ganho concreto de cada um
5. **Acessos e variáveis**: o que tenho, o que falta
6. **Impacto nas auditorias:** quais itens vão ficar como 🔍 por falta de conexão

Depois, pare e pergunte o que devo conectar.

## Registro

Atualize a seção **Ambiente** do `auditorias/contexto.md` com os MCPs conectados e testados, os que ficaram de fora e por quê, os acessos que eu tenho e o modo de uso (só leitura ou também aplicar). Não grave token, chave nem valor de variável — só nomes.

Quando outra etapa marcar um item como 🔍 por falta de MCP, ela deve citar qual conexão resolveria. Se isso se repetir, sugira rodar esta skill de novo.
