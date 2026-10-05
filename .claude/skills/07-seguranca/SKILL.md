---
name: 07-seguranca
description: "Audita seguranca do site conforme o que o projeto tem: chaves expostas, headers, validacao de entrada, XSS, RLS e chaves do banco, autenticacao, upload e webhooks de pagamento. Use quando o usuario pedir auditoria de seguranca, revisao de vulnerabilidades ou verificacao de chaves de API."
---

# Auditoria de Segurança

Leia primeiro `.claude/lz/FLUXO.md`: ele define a ordem das etapas, o protocolo de perguntas, o arquivo `auditorias/contexto.md`, o vocabulario de status e o formato do registro. As regras de la valem aqui.

Você é um auditor de segurança revisando este projeto web. Seu trabalho nesta etapa é **encontrar e relatar** problemas, não corrigir. Não altere nenhum arquivo até eu aprovar.

## Fase 0: Entenda o projeto antes de auditar

Explore o repositório (package.json ou equivalente, estrutura de pastas, configs de deploy, .env.example, rotas, pasta de API/functions) e monte um **Perfil do projeto** curto:

- **Tipo:** landing page estática / site institucional multipágina / SPA / app full-stack / e-commerce
- **Stack:** framework, linguagem, hospedagem (Vercel, Netlify, VPS, hospedagem compartilhada...)
- **Backend:** tem servidor próprio ou funções serverless? Sim/não
- **Banco de dados:** tem? Qual (Supabase, Firebase, Postgres, MySQL, Mongo...)?
- **Autenticação:** tem login? Própria ou de provedor (Supabase Auth, Firebase Auth, Clerk...)?
- **Formulários:** tem? Para onde vão os dados (backend próprio, EmailJS, Formspree, WhatsApp...)?
- **Upload de arquivos:** tem?
- **APIs de terceiros com chave:** quais?
- **Pagamentos / webhooks:** tem? Qual gateway?

Se algo for ambíguo, diga o que você assumiu e por quê.

## Fase 0.5: Contexto e perguntas

Leia `auditorias/contexto.md` e siga o protocolo de perguntas do FLUXO: no máximo 5 perguntas **no total**, todas de uma vez, com opções e recomendação marcada, e nada que já esteja no contexto. As perguntas comuns (site publicado e domínio definitivo, cliente ou projeto próprio, conteúdo final ou rascunho, o que está planejado e ainda não foi feito) só entram se o contexto não responder. Das específicas desta etapa, abaixo, fique com as que mudam o resultado:

1. Quais serviços externos o projeto usa que talvez não apareçam no código (Supabase, Firebase, gateway, e-mail)?
2. As chaves configuradas hoje são de teste ou de produção?
3. Tenho acesso aos painéis desses serviços para conferir as configurações que o código não mostra?

## Fase 1: Decida o que se aplica

Os itens abaixo estão agrupados por **gatilho**. Um grupo só é auditado se o gatilho existir no projeto. Itens de grupos que não se aplicam **não devem ser analisados**: apenas liste-os no final como N/A com o motivo em uma linha (ex.: "sem banco de dados").

## Fase 2: Checklist

### Sempre (qualquer projeto, inclusive landing page)

1. **Segredos no código:** chaves de API, tokens ou senhas escritos no código. Atenção especial a variáveis com prefixo público (`VITE_`, `NEXT_PUBLIC_`, `REACT_APP_`, `PUBLIC_`): tudo com esse prefixo vai parar no navegador e qualquer pessoa pode ler.
2. **Segredos no histórico do Git:** se já existe `auditorias/04-git.md`, não repita a varredura: apenas traga as pendências de lá e confirme se as chaves foram rotacionadas. Se não existe, verifique aqui mesmo:  `.env` já foi commitado alguma vez? Chaves em commits antigos? O `.gitignore` cobre `.env*`? Sugira varrer com `gitleaks` ou `trufflehog`. Se encontrar segredo vazado, a correção principal é **rotacionar a chave** no painel do serviço; apagar do histórico não basta.
3. **HTTPS forçado:** redirecionamento http→https, HSTS, nenhum conteúdo misto (scripts, imagens ou iframes carregados via `http://`).
4. **Security headers:** Content-Security-Policy, X-Content-Type-Options, Referrer-Policy, Permissions-Policy, `frame-ancestors` (ou X-Frame-Options). Indique onde configurar para a hospedagem detectada (`vercel.json`, `netlify.toml`, `_headers`, `.htaccess`, nginx...).
5. **Dependências:** a skill `dependencias` tem o padrão completo — triagem do `audit` separando produção de desenvolvimento, direta de transitiva, e a checagem de pacote suspeito (typosquatting, nome inventado por IA, `postinstall` estranho, falta de lockfile). Se ela já rodou (`auditorias/dependencias.md`), **traga as pendências de lá** em vez de repetir a análise, e confirme que a lista de "Aceitas" continua válida. Se não rodou, carregue-a agora e execute a triagem aqui mesmo — não se resolve isso com a contagem do `npm audit` colada no relatório. O corte desta auditoria é: Crítica ou Alta em dependência de produção é pendência de severidade igual à do advisory; só em dependência de desenvolvimento entra como Baixa, com o motivo.
6. **Higiene geral:** links `target="_blank"` com `rel="noopener noreferrer"`; scripts de terceiros/CDN justificados e com versão fixa; source maps não publicados em produção; nenhum arquivo sensível na pasta pública (backups, `.sql`, `.env`, `.git`).

### Se tem formulário ou qualquer entrada do usuário

**Princípio:** valide na entrada e escape na saída, de acordo com o contexto. "Sanitizar todo input" é um equívoco comum: corrompe dado legítimo e ainda deixa brecha quando o contexto de saída é outro (HTML, atributo, URL, script). Sanitizar só entra quando o HTML é permitido de propósito.

7. **Validação de entrada no servidor** (tipo, tamanho, formato, lista de valores aceitos). Validação só no front-end é conveniência, não proteção. Sugira schema (zod, yup, pydantic...).
8. **XSS — renderização segura**, verificando cada contexto:
   - **HTML:** nada de `innerHTML`, `dangerouslySetInnerHTML`, `v-html`, `insertAdjacentHTML` ou `document.write` com dado do usuário.
   - **URL:** `href`, `src` e `action` montados com dado do usuário precisam aceitar só `https:` (e `mailto:`/`tel:` quando fizer sentido). `javascript:` num link passa por qualquer framework.
   - **Rich text e markdown:** se o site renderiza texto formatado vindo do usuário ou de um CMS, precisa de sanitizador com lista de tags permitidas (ex.: DOMPurify). O renderizador de markdown sozinho não protege.
   - **Script embutido:** dado do usuário dentro de `<script>` — inclusive o JSON-LD do schema.org — pode fechar a tag com `</script>`. Serialize com escape de `<`.
   - **Execução dinâmica:** nada de `eval`, `new Function` ou `setTimeout` com string montada a partir de entrada.
   - **Parâmetros de URL** exibidos na página (busca, mensagem de erro, nome) tratados como entrada de usuário.
   - **Templates do servidor:** saída com escape automático ligado; qualquer marcação de "não escapar" justificada.
   - A CSP do item 4 é a segunda linha de defesa, não a primeira.
9. **Proteção contra bots e spam:** honeypot, Cloudflare Turnstile / reCAPTCHA / hCaptcha, limite de envios.
10. **Serviço de formulário de terceiros** (EmailJS, Formspree...): a chave exposta é a pública? O serviço está restrito ao domínio do site?

### Se tem backend, API, server actions ou funções serverless

11. **Chaves de terceiros só no servidor:** o front chama o seu backend; nunca chama direto uma API paga com a chave secreta.
12. **Regra de negócio no servidor.** Todo valor que afeta **dinheiro, permissão ou estado** é calculado ou conferido no servidor, nunca aceito do cliente: preço, desconto, frete, quantidade, cargo, plano, status ("pago", "aprovado", "cancelado") e transições de status. Procure especificamente:
    - endpoint ou server action que recebe `userId`, `role`, `isAdmin`, `price` ou `status` do corpo da requisição e usa sem conferir — o usuário vem da sessão, não do corpo;
    - regra aplicada só no front (botão desabilitado, campo oculto, rota escondida do menu);
    - proteção de rota **só no middleware**: a checagem precisa se repetir na rota, na server action ou na camada de dados, porque middleware pode ser contornado e não cobre chamadas diretas.
13. **IDOR — dono do recurso em todo acesso por ID.** Liste **todos** os pontos que recebem um identificador de recurso — na URL (`/pedidos/123`), na query (`?id=`), no corpo ou num caminho de arquivo — incluindo rotas de API, server actions, funções serverless e caminhos do storage. Para cada um, confirme que a consulta **filtra pelo usuário da sessão** (ou por uma permissão explícita), e não só pelo ID. Relate em tabela: `Endpoint | ID recebido | Checa dono? | Evidência`. Observações:
    - UUID não resolve IDOR, só dificulta adivinhar. ID vaza em link, log e print.
    - "Buscar e depois verificar" também vale, desde que a verificação aconteça antes de devolver ou alterar.
    - Em Supabase com RLS correto, o banco já faz essa checagem — mas só se a consulta roda com a sessão do usuário. Com `service_role` no servidor, o RLS não se aplica e a checagem tem que estar no código.
    - Arquivos: URL pública de bucket e caminho previsível (`/uploads/<id-do-usuario>/`) são IDOR de arquivo. Prefira bucket privado com URL assinada de curta duração.
14. **Respostas da API enxutas:** retornar só os campos necessários (sem hash de senha, dados de outros usuários, campos internos). Mensagens de erro sem stack trace nem detalhes do banco.
15. **Mass assignment:** o backend não pode aceitar o body inteiro direto em insert/update (ex.: usuário enviando `role: "admin"` ou `preco: 0`). Deve haver lista explícita dos campos permitidos.
16. **CORS:** origens restritas; nunca `*` junto com credenciais.
17. **Rate limit** em endpoints sensíveis ou caros.

### Se tem banco de dados

A skill `banco` tem o padrão completo, de organização e de segurança, incluindo o **teste de isolamento do RLS com dois usuários**. Se ela já rodou (`auditorias/banco.md`), traga as pendências de lá em vez de repetir a análise; se não rodou, verifique os itens abaixo e recomende rodá-la.

18. **Queries parametrizadas** ou ORM; nenhuma concatenação de string com input do usuário (SQL/NoSQL injection).
19. **Só a chave pública no cliente:** ex.: no Supabase, apenas a `anon`/`publishable` key no front. `service_role`, admin SDK ou credenciais de banco **nunca** no front-end.
20. **Row-Level Security:** RLS ativo em **todas** as tabelas expostas (Supabase/Postgres) ou Security Rules restritivas (Firebase), com policies que isolam por usuário (`auth.uid()`) e não algo como `using (true)`. O isolamento precisa ser **testado**, não só lido — ver skill `banco`.
21. **Dados sensíveis:** CPF, telefone, endereço, documentos. Guardar só o necessário (LGPD), criptografar o que for crítico, nunca logar esses dados.

### Se tem autenticação

22. **Autorização checada no servidor** em toda rota ou ação protegida. Esconder um botão no front não é proteção. (Ver também o item 12.)
23. **Hash de senhas** com bcrypt ou argon2; nunca MD5/SHA puro nem texto plano. Se a autenticação é de um provedor, marque como "delegado ao provedor".
24. **Rate limit / bloqueio** em login, cadastro e recuperação de senha.
25. **Cookies de sessão:** `HttpOnly`, `Secure`, `SameSite`; evitar token em `localStorage` quando possível; proteção CSRF quando a sessão usa cookie.
26. **Recuperação de senha e verificação de e-mail** com token de uso único e expiração.
27. **Permissão nunca vem de dado editável pelo usuário.** Cargo e plano não podem ser lidos de metadados que o próprio usuário altera (ex.: `user_metadata` no Supabase). Use tabela própria protegida ou claims definidos só pelo servidor.

### Se tem upload de arquivos

28. Tipo validado pelo conteúdo (não só pela extensão), limite de tamanho, nome do arquivo gerado pelo servidor, armazenamento fora da pasta pública ou em bucket com permissão correta, e nunca servir SVG/HTML enviado pelo usuário de forma que execute no navegador. Acesso ao arquivo com checagem de dono (item 13).

### Se processa pagamentos ou recebe webhooks

29. **Valor definido no servidor**, nunca vindo do front-end (caso particular do item 12).
30. **Webhook valida a assinatura** do gateway e é idempotente (receber o mesmo evento duas vezes não duplica nada).
31. **Dados de cartão nunca passam pelo seu servidor:** usar checkout ou tokenização do próprio gateway.

## Como investigar

- Cite **evidência concreta**: arquivo e linha. Não chute.
- Se algo não puder ser verificado pelo código (painel do Supabase, configurações da hospedagem, DNS), marque **🔍 Verificar manualmente** e diga exatamente onde olhar.
- Se conseguir rodar ferramentas (`npm audit`, etc.), rode. Se não, informe o comando.

## Formato do relatório

1. **Perfil do projeto** (da Fase 0)
2. **Resumo:** X críticos, Y altos, Z médios, W baixos
3. **Tabela:** `# | Item | Status | Severidade | Evidência | Correção sugerida`
   - Status: ✅ OK / ⚠️ Parcial / ❌ Falha / 🔍 Verificar manualmente
   - Severidade: **Crítica** (explorável agora, vaza dados ou dinheiro) / **Alta** / **Média** / **Baixa** (boa prática)
4. **Top 3 correções** em ordem de prioridade (impacto × esforço)
5. **Itens N/A:** lista curta com o motivo de cada um

Depois do relatório, pare e pergunte quais correções devo aplicar.

## Registro

Depois que eu aprovar e as correções forem aplicadas, salve um resumo em `auditorias/07-seguranca.md` (na raiz do projeto, fora da pasta pública) com: data, itens corrigidos, pendências (com severidade) e itens que decidi não corrigir, com o motivo. Esse arquivo é lido pelo pré-lançamento, pela revisão geral e, na entrega ao cliente, pelo `projeto-limpo`. Se o repositório for público, siga a ressalva do FLUXO sobre `auditorias/`: avise e ofereça ignorar a pasta, sem decidir sozinho.
