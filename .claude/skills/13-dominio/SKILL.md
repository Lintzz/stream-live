---
name: 13-dominio
description: "Fecha tudo que ficou adiado por nao existir dominio proprio. Roda quando o dominio definitivo foi comprado e apontado para o site: atualiza canonical, og:url, sitemap e robots, confere SSL e redirecionamento de www, URLs de autenticacao, webhook do gateway, restricao de dominio nas chaves, SPF e DKIM, e Search Console. Use quando o usuario disser que comprou o dominio, que o dominio ja esta apontando, ou que o site saiu do preview para producao."
---

# Migração para o domínio definitivo

Leia primeiro `.claude/lz/FLUXO.md`: ele define a ordem das etapas, o protocolo de perguntas, o arquivo `auditorias/contexto.md`, o vocabulário de status e o formato do registro. A seção **Estágio do projeto** de lá é o que esta skill vem fechar.

Enquanto o projeto esteve em `preview`, as auditorias adiaram os itens que dependem de domínio e guardaram cada um como **⏸️ Pendências de domínio**. Esta skill recolhe essa lista e resolve.

## Fase 0: Reúna o que ficou adiado

Leia `auditorias/contexto.md` e a seção **Pendências de domínio** de todos os registros em `auditorias/`, inclusive `auditorias/features/`.

Monte a lista do que foi adiado, com a etapa de origem de cada item. Se nenhuma auditoria rodou ainda, siga pelo checklist abaixo mesmo assim e avise que o ideal é rodar as auditorias depois.

Confirme o básico antes de mexer em qualquer coisa:

- Qual é o domínio definitivo
- Ele já está apontando para o site (DNS propagado)
- A versão canônica escolhida: com `www` ou sem `www`

Se o domínio ainda não estiver apontando, diga isso e pare: quase tudo aqui depende disso estar resolvido.

## Fase 1: Perguntas

Siga o protocolo do FLUXO. Normalmente bastam:

1. Qual é o domínio definitivo?
2. Versão canônica: com `www` ou sem `www`?
3. O domínio está no nome do cliente? (é o momento certo de corrigir se não estiver)
4. O site vai passar a ser indexado pelo Google agora, ou continua fechado por enquanto?

## Fase 2: Checklist do domínio

### No código

1. **`canonical`** em todas as páginas, com URL absoluta no domínio definitivo.
2. **Open Graph:** `og:url` e `og:image` com URL absoluta. Teste o preview colando o link numa conversa do WhatsApp.
3. **`sitemap.xml`** com todas as URLs no domínio novo.
4. **`robots.txt`**: linha `Sitemap:` apontando para o domínio novo e, se o site vai ser indexado, sem bloqueio geral.
5. **Schema.org:** URLs dentro do JSON-LD.
6. **Nenhuma URL temporária sobrando** no código, nas configs ou nos textos: procure por `vercel.app`, `netlify.app`, `localhost` e pelo subdomínio antigo.
7. **`noindex` de preview removido**, se existia e o site agora deve ser indexado.

### Na hospedagem e no DNS

8. **Certificado SSL** válido para o domínio e para o `www`.
9. **`http` redireciona para `https`**, e a versão não canônica redireciona para a canônica, com redirecionamento permanente (301).
10. **HSTS**, se fizer sentido para o projeto.
11. **Headers de segurança** chegando de verdade na resposta do domínio novo: confira com `curl -I https://dominio` ou pelo securityheaders.com. Configuração no arquivo não é garantia de header servido.
12. **Preview deployments:** decida se as URLs temporárias continuam públicas e indexáveis. O normal é protegê-las ou deixá-las com `noindex`.

### Nos serviços conectados

13. **Autenticação:** Site URL e Redirect URLs do provedor apontando para o domínio novo. Se isso ficar para trás, o login quebra.
14. **Webhook do gateway de pagamento** recadastrado na URL do domínio novo. Teste um evento.
15. **Restrição de domínio** nas chaves de API e nos serviços de formulário: adicione o domínio novo, remova o temporário quando não for mais usado.
16. **CORS** do backend, se houver lista de origens permitidas.
17. **E-mail pelo domínio:** SPF, DKIM e DMARC no DNS; remetente configurado com o domínio novo. Envie um teste e confira se não cai em spam.
18. **Analytics:** propriedade apontando para o domínio novo, sem perder o histórico se já havia dados.

### Depois que estiver no ar

19. **Google Search Console:** propriedade criada **na conta do cliente**, domínio verificado, `sitemap.xml` enviado.
20. **Google Business Profile**, se for negócio local: link do site atualizado.
21. **Links externos que o cliente controla:** Instagram, WhatsApp Business, Google Business, assinatura de e-mail, cartão. Liste para ele trocar.

## Fase 3: Verificação final

Depois das correções, confirme no ar:

- A página abre no domínio, com cadeado, sem conteúdo misto
- O redirecionamento da versão não canônica funciona
- O preview do link no WhatsApp aparece certo
- Formulário, login e pagamento continuam funcionando no domínio novo
- PageSpeed no domínio novo, para comparar com o registro de performance

## Formato do relatório

1. **Domínio, versão canônica e estado do DNS**
2. **Pendências de domínio recolhidas**, com a etapa de origem de cada uma
3. **Tabela:** `# | Item | Status | Evidência | O que fazer`
   - Status: ✅ OK / ❌ Falha / 🔍 Verificar manualmente
4. **O que quebra se não for feito** — destaque especial para autenticação e webhook, que são os que derrubam funcionalidade
5. **Lista para o cliente:** onde ele precisa trocar o link do site

Pare e pergunte o que devo aplicar.

## Registro

Depois das correções:

1. Salve `auditorias/13-dominio.md` com data, domínio, o que foi migrado, o que ficou pendente e o que precisa de ação do cliente.
2. Atualize `auditorias/contexto.md`: estágio passa a `producao`, URL atual vira o domínio definitivo.
3. **Limpe as "Pendências de domínio"** dos registros das outras etapas: cada item resolvido sai da lista; o que continuar aberto vira pendência normal daquela etapa.
4. Se já existia registro de pré-lançamento, diga se ele precisa ser refeito no domínio novo. Em geral precisa: vários itens dele foram verificados na URL temporária.
