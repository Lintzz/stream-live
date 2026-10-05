---
name: banco
description: "Padrao obrigatorio de banco de dados - organizacao e seguranca. Nao e um comando a chamar: carregue sozinho, sem o usuario pedir, ANTES de criar ou alterar tabela, colecao, schema, migration, query, indice, policy de RLS ou regra do Firestore, e ao modelar dados. Tambem audita o banco existente e diz o que esta fora do padrao. Use sempre que o trabalho envolver Supabase, Postgres, MySQL, Firebase, Firestore, MongoDB, Prisma, Drizzle, migrations, RLS, security rules ou modelagem de dados."
user-invocable: false
---

# Padrão de banco de dados

Leia primeiro `.claude/lz/FLUXO.md`: ele define o protocolo de perguntas, o `auditorias/contexto.md`, o vocabulário de status e o formato do registro. As regras de lá valem aqui.

**Esta skill não é um comando.** Você a carrega sozinho sempre que o trabalho tocar o banco — o usuário não precisa digitar nada. Ela tem dois usos, e você decide pelo contexto:

- **Modo consulta** — vou criar ou alterar algo no banco. Aplique este padrão ao que for escrever, antes de escrever. Não peça relatório nem aprovação para seguir o padrão: ele é o padrão.
- **Modo auditoria** — o usuário pediu, com palavras, para conferir o banco ("audita o banco", "as regras estão certas?"), ou o `07-seguranca` e o `00-diagnosticar` chamaram. Aí vale o fluxo normal: analisar, relatar, pedir aprovação.

Antes de qualquer coisa, descubra **qual banco e qual plataforma** o projeto usa (`auditorias/contexto.md`, `PLANO.md`, dependências, `.env.example`). Depois leia `.claude/skills/banco/references/plataformas.md` e siga a seção da plataforma correspondente, além das regras gerais abaixo.

Se houver MCP do banco conectado, use-o para ver o estado real em vez de deduzir pelo código, seguindo as regras de MCP do FLUXO: leitura livre, alteração só com aprovação.

## Regras inegociáveis

Valem em qualquer banco, qualquer projeto, sem exceção:

1. **Nada de chave administrativa no cliente.** `service_role`, admin SDK, string de conexão e senha de banco só existem no servidor. No front, apenas a chave pública.
2. **Toda tabela ou coleção exposta ao cliente nasce com regra de acesso.** No Postgres, RLS ativo com policy real, isolando por usuário (`auth.uid()` no Supabase) — e o isolamento é testado com dois usuários, não só lido. No Firestore, regra explícita. Regra que libera geral (`using (true)`, `allow read, write: if true`) é o mesmo que não ter regra.
3. **Autorização é no servidor.** Esconder botão não protege nada. Quem pode ler ou alterar cada registro é decidido no banco ou no backend.
4. **Nunca concatenar string com entrada do usuário para montar query.** Sempre parâmetro ou ORM.
5. **Nunca aceitar o corpo da requisição inteiro** em insert ou update. Liste explicitamente os campos permitidos, senão o usuário manda `role: "admin"` ou `preco: 0`.
6. **Dado sensível não vai para log.** CPF, telefone, endereço, token: nem em log, nem em mensagem de erro devolvida ao cliente.
7. **Migration aplicada não se edita.** Corrige-se com uma migration nova. E nunca se altera schema direto no painel de produção sem registrar no código.

## Organização e nomenclatura

Escolha uma convenção no início e registre em `CONVENCOES.md`. A recomendada para bancos SQL:

- **Tabelas:** `snake_case`, plural: `clientes`, `itens_pedido`
- **Colunas:** `snake_case`, singular: `nome`, `criado_em`
- **Chave primária:** `id`
- **Chave estrangeira:** `<tabela_singular>_id` → `cliente_id`
- **Booleanos:** prefixo claro — `ativo`, `pago`, `excluido`
- **Datas:** sufixo `_em` para instante (`criado_em`, `pago_em`) e `_data` para data pura
- **Tabela de ligação:** os dois nomes em ordem alfabética — `cliente_produto`
- **Índices e constraints:** nome descritivo, não o gerado automático
- **Idioma:** um só, igual ao do resto do código

Em bancos de documentos, a convenção muda mas o princípio é o mesmo: nome no plural para coleção, `camelCase` para campo, e a regra escrita.

Além dos nomes:

- **Toda tabela tem `criado_em` e `atualizado_em`** com fuso (`timestamptz`), preenchidos pelo banco.
- **Escolha o tipo da chave conscientemente:** `uuid` quando o id aparece em URL ou vem do cliente (evita adivinhar registro de outro usuário); numérico sequencial só quando o id não é exposto.
- **Use o tipo certo:** dinheiro é decimal, nunca float. Texto com opções fixas é enum ou tabela de apoio, não texto livre.
- **`not null` por padrão.** Nulo deve ser uma decisão, não um descuido.
- **Constraint no banco, não só no código:** unicidade, chave estrangeira e checagem de valor. O código erra; o banco não deixa passar.
- **Índice nas colunas que aparecem em filtro, junção e ordenação** — principalmente nas chaves estrangeiras.
- **Prefira exclusão lógica** (`excluido_em`) quando o registro tiver valor histórico, e deixe as consultas filtrando isso.

## Dados pessoais

- Guarde só o que a finalidade exige. Campo "porque pode ser útil" é risco, não recurso.
- Senha só como hash com algoritmo próprio para isso (bcrypt ou argon2). Nunca texto puro, nunca MD5 ou SHA simples. Se a autenticação é de um provedor, isso é dele.
- Dado crítico (documento, dado financeiro) merece criptografia no banco.
- Defina por quanto tempo cada dado fica e como o usuário pede exclusão. Isso alimenta a auditoria de LGPD.
- Ambiente de teste não usa dado real de cliente.

## Consultas

- Selecione as colunas necessárias, não tudo, principalmente em tabela com dado sensível.
- Nada de consulta dentro de laço (N+1): resolva com junção ou uma consulta só.
- Toda listagem tem paginação e limite máximo. Endpoint sem limite é convite a derrubar o site.
- A resposta da API devolve só o que a tela usa — nunca o registro inteiro do banco.

## Migrations e ambientes

- Toda alteração de schema vira migration versionada no repositório, com nome descritivo e data.
- Migration precisa ser reversível, ou trazer escrito como desfazer.
- Alteração destrutiva (apagar coluna ou tabela) é feita em passos: parar de usar, esperar, depois remover — e nunca sem backup.
- Ambiente de desenvolvimento e de produção são separados. Site em preview não escreve na base de produção.
- Seeds de teste ficam separados e nunca rodam em produção.
- Antes de qualquer migration em produção: backup confirmado e restauração testada pelo menos uma vez.

## Modo auditoria

Quando eu pedir a conferência, verifique nesta ordem e relate no formato `# | Item | Status | Severidade | Evidência | Correção sugerida`:

1. Chave administrativa exposta no cliente — **Crítica** se encontrada
2. Tabela ou coleção sem regra de acesso, ou com regra que libera geral — **Crítica**
3. Policy que existe mas não restringe por dono do registro — **Alta**
3a. **Teste de isolamento com dois usuários** (ver `references/plataformas.md`) quando houver dado por usuário. Relate cada tentativa do usuário A sobre o dado do B e o resultado. Qualquer tentativa que funcione é **Crítica**. Sem acesso para testar, marque 🔍 e deixe o roteiro do teste pronto
3b. Permissão lida de dado editável pelo usuário (`user_metadata`), view sem `security_invoker`, função `security definer` sem checagem do usuário — **Alta**
4. Concatenação de string em query, ou insert/update aceitando o corpo inteiro — **Alta**
5. Senha mal armazenada, dado sensível em log — **Alta**
6. Falta de constraint, índice ausente em coluna filtrada, tipo errado para dinheiro
7. Nomenclatura fora do padrão do `CONVENCOES.md`
8. Migrations: alterações feitas direto no painel e ausentes do repositório
9. Consultas sem paginação, N+1, resposta devolvendo campos demais
10. Backup: existe, com que frequência, e alguém já testou restaurar

Se houver MCP do banco, confirme as policies e o schema **reais**; sem ele, marque como 🔍 e diga o que conferir no painel.

## Registro

Em modo auditoria, depois das correções aprovadas, salve `auditorias/banco.md` com data, plataforma, o que foi corrigido, pendências com severidade, o que decidi não corrigir e o que depende do painel. Acrescente ao `CONVENCOES.md` as convenções de nome adotadas, se ainda não estiverem lá.

Em modo consulta, não crie registro: só acrescente ao `auditorias/contexto.md` as tabelas ou coleções novas que passaram a existir.
