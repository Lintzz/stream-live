# Particularidades por plataforma

Leia a seção da plataforma que o projeto usa, junto com as regras gerais do SKILL.md. Detalhes de API e nomes de recurso mudam entre versões: quando houver MCP de documentação conectado, confirme a sintaxe na documentação oficial da versão em uso antes de escrever código.

## Supabase e Postgres

**Chaves.** No front, só a chave pública (`anon` / publishable). A `service_role` ignora RLS por definição: se ela vazar para o navegador, o banco inteiro está aberto — leitura e escrita — e nenhuma policy vai impedir. Ela só existe em função de servidor. Numa chave exposta por engano, trocar a chave no painel é a correção; apagar do código não basta.

**RLS.** Criar tabela não ativa RLS. Ative em toda tabela acessível pela API e escreva as policies:

- Uma policy por operação (`select`, `insert`, `update`, `delete`). Liberar leitura não libera escrita.
- A condição tem que amarrar o registro ao usuário: comparar a coluna de dono com o identificador do usuário autenticado. `using (true)` é tabela pública — só use quando o conteúdo for realmente público, como uma lista de serviços do site.
- No `insert`, a verificação vai no `with check`, senão o usuário insere registro em nome de outro.
- Teste como usuário comum, não como dono do projeto: no painel, o dono passa por cima das policies e tudo parece funcionar.
- Tabela sem RLS aparece como aviso no painel. Trate cada aviso como pendência, não como ruído.

**Armadilhas que passam despercebidas:**

- **`user_metadata` é editável pelo próprio usuário.** Policy que decide permissão lendo `auth.jwt() -> 'user_metadata'` (cargo, plano, empresa) pode ser burlada por qualquer usuário logado. Guarde permissão em tabela própria protegida por RLS, ou em `app_metadata`, que só o servidor altera.
- **Views ignoram RLS por padrão**, porque rodam com a permissão de quem as criou. Crie com `security_invoker = true` ou não exponha a view pela API.
- **Funções `security definer`** rodam com permissão elevada e passam por cima das policies. Toda função assim precisa checar o usuário por conta própria, fixar o `search_path` e não ser executável por `anon` sem necessidade.
- **`auth.uid()` é nulo para visitante não logado.** Uma policy como `dono_id = auth.uid()` não libera nada para anônimo (correto), mas `dono_id is null or ...` pode liberar registros órfãos para qualquer um.
- **Policy de `update` sem `with check`** deixa o usuário alterar a coluna de dono e "transferir" o registro para outra pessoa.
- **Performance:** envolva a chamada como `(select auth.uid())` na policy para ela ser avaliada uma vez por consulta, e indexe a coluna de dono.

**Teste de isolamento com dois usuários** (obrigatório no modo auditoria quando houver dado por usuário):

1. Crie ou use dois usuários de teste, A e B, cada um com pelo menos um registro em cada tabela protegida.
2. Autenticado como A, tente: listar tudo da tabela; ler o registro de B pelo ID; atualizar o registro de B; apagar o registro de B; inserir um registro com o dono igual a B; atualizar o próprio registro trocando o dono para B.
3. Repita como visitante não logado.
4. **Resultado esperado:** A vê e altera só o que é dele; toda tentativa sobre B retorna vazio ou erro; o anônimo não vê nada que não seja público.

Faça o teste pela API com a chave pública e a sessão do usuário — é como o atacante faria. Com MCP do banco, dá para simular no SQL definindo o papel `authenticated` e o `sub` do JWT de cada usuário na sessão. **Nunca teste pelo painel logado como dono do projeto**: o dono ignora RLS e tudo parece funcionar. Registre o resultado de cada tentativa.

**Outros pontos:** as funções com permissão elevada precisam de cuidado extra com o caminho de busca e com quem pode executá-las. Storage tem políticas próprias, separadas das tabelas — bucket público expõe arquivo a quem tiver a URL. Prefira migrations versionadas ao editor do painel, e gere os tipos a partir do banco por comando, sem editar à mão. Projeto no plano gratuito pode pausar por inatividade: péssimo para site de cliente com pouco tráfego.

## Firebase e Firestore

**Regras de segurança são a única barreira.** O cliente fala direto com o banco, então não existe "servidor confiável" no meio. Regra em modo de teste expira e costuma virar produção esquecida — é o vazamento mais comum de projeto Firebase.

- Comece negando tudo e libere caso a caso.
- Amarre o acesso ao usuário autenticado e ao dono do documento.
- Valide **o formato dos dados na própria regra**: campos permitidos, tipos e tamanho. Sem isso, qualquer cliente grava qualquer campo em qualquer documento que ele possa escrever.
- Regra de coleção não protege subcoleção automaticamente: escreva as duas.
- Regra de consulta é avaliada contra a consulta, não contra os documentos: uma consulta que poderia trazer documento proibido é recusada inteira. Isso confunde, e o sintoma é "funciona no painel e falha no site".
- Use o emulador e escreva testes das regras. É a única forma de ter certeza.

**Modelagem.** Documento tem limite de tamanho, então evite array que cresce sem fim; prefira subcoleção. Duplicar dado para evitar leitura extra é normal aqui, mas decida quem atualiza a cópia. Índice composto é exigido para várias consultas e o erro já traz o link para criar. Cobrança é por leitura de documento: consulta sem limite em tela muito acessada vira conta alta. O Admin SDK ignora todas as regras — só no servidor.

## MySQL, MariaDB e Postgres puro

Sem camada de API própria, a proteção fica no backend: o banco não deve ser acessível pela internet, só pela aplicação. Use usuário de banco com permissão mínima — a aplicação não precisa de privilégio administrativo. Sempre consulta parametrizada ou ORM. Em MySQL, prefira o mecanismo transacional padrão, escolha uma codificação que suporte emoji e acentuação sem perda, e cuide do fuso: guarde instante em UTC e converta na exibição. Backup automático é responsabilidade sua: hospedagem compartilhada costuma ter backup fraco ou inexistente.

## ORMs (Prisma, Drizzle e similares)

O schema do ORM é a fonte da verdade, e toda alteração vira migration versionada — nada de mexer no banco por fora. O ORM protege de injeção enquanto você não usa a saída de emergência para SQL cru; quando usar, parametrize. Tenha claro que o ORM **não** substitui RLS: se o cliente também fala com o banco direto (caso do Supabase), as policies continuam necessárias. Cuidado com o relacionamento carregado dentro de laço, que vira N+1, e com o `select` implícito que devolve todas as colunas, inclusive as sensíveis.

## MongoDB

Injeção existe aqui também, por operador vindo do usuário: valide a entrada e nunca passe objeto do cliente direto para a consulta. Defina esquema e validação, mesmo o banco permitindo documento livre. Nunca exponha o banco à internet sem autenticação e sem lista de IPs — instância aberta é varrida por bots em horas. Índice em tudo que é filtrado, e atenção ao documento que cresce sem limite.

## Planilha como banco

Aparece em projeto pequeno (formulário gravando em planilha). Serve para registro simples de contato, e só. Não use para dado pessoal sensível, autenticação ou qualquer coisa com concorrência. O controle de acesso é o compartilhamento do arquivo: uma planilha "com o link" é pública. Se o projeto precisa de login ou de mais de uma tabela relacionada, é hora de um banco de verdade.
