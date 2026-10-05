---
name: 12-pre-lancamento
description: "Verificacao final antes de publicar: confere as pendencias registradas das auditorias anteriores, dominio definitivo, segredos no build, headers na resposta real, formulario chegando ao destino, consentimento de cookies e testes manuais, e da um veredito de lancamento. Use quando o usuario disser que vai publicar, subir para producao ou entregar o site."
---

# Checklist de Pré-Lançamento

Leia primeiro `.claude/lz/FLUXO.md`: ele define a ordem das etapas, o protocolo de perguntas, o arquivo `auditorias/contexto.md`, o vocabulario de status e o formato do registro. As regras de la valem aqui.

Você está fazendo a verificação final antes de este site ser entregue ou publicado. O foco aqui não é a qualidade do código, e sim **se tudo funciona de verdade para quem vai usar** e se ficou alguma pendência grave das auditorias anteriores. Seu trabalho é **verificar e relatar**. Não altere nenhum arquivo até eu aprovar.

Este checklist vem depois das auditorias de conversão, segurança, LGPD, SEO, acessibilidade e performance. Ele **não refaz** essas auditorias: confere as pendências registradas e faz uma checagem rápida dos pontos críticos no site publicado.

## Fase 0: Entenda o projeto, o estado do deploy e o histórico

Monte um **Perfil** curto:

- **Tipo:** landing page / institucional / portfólio / SPA / app com login / e-commerce
- **Stack e hospedagem**
- **Já está publicado?** Qual URL? Domínio próprio ou subdomínio da hospedagem? Qual será o **domínio definitivo**?
- **Funcionalidades presentes:** formulário, botão/link de WhatsApp, login, banco de dados, pagamentos, envio de e-mails, analytics/pixel, upload
- **Roteiro:** leia `auditorias/roteiro.md`. Etapa que ficou como "não se aplica" não é pendência; etapa do roteiro ainda aberta é.
- **Auditorias anteriores:** existe a pasta `auditorias/` na raiz do projeto? Liste quais relatórios existem (`03-desenvolvimento`, `04-git`, `05-conversao`, `06-interface`, `07-seguranca`, `08-lgpd`, `09-seo`, `10-acessibilidade`, `11-performance`) e quais estão faltando. Se a pasta não existir, avise: significa que as auditorias não foram registradas, e este checklist fará apenas a checagem rápida do grupo "Pontos críticos em produção".

Se o site **ainda não está publicado**, verifique tudo que der pelo código e pelo build local, e marque os itens que dependem do site no ar como **🔍 Testar após publicar**, com o passo a passo do teste.

## Fase 0.5: Contexto e perguntas

Leia `auditorias/contexto.md` e siga o protocolo de perguntas do FLUXO: no máximo 5 perguntas **no total**, todas de uma vez, com opções e recomendação marcada, e nada que já esteja no contexto. As perguntas comuns (site publicado e domínio definitivo, cliente ou projeto próprio, conteúdo final ou rascunho, o que está planejado e ainda não foi feito) só entram se o contexto não responder. Das específicas desta etapa, abaixo, fique com as que mudam o resultado:

1. Qual é a data prevista de lançamento?
2. Tenho acesso ao painel da hospedagem, ao DNS e aos serviços conectados para testar?
3. Se houver pagamento, posso fazer uma transação real de valor baixo para testar e depois estornar?
4. Existe alguma pendência que você já sabe que vai ficar para depois do lançamento?

## Fase 0.6: Modo de lançamento

Leia o estágio do projeto no `auditorias/contexto.md` e decida o modo:

- **Modo produção** — existe domínio definitivo apontando. Roda o checklist inteiro.
- **Modo preview** — o site vai para uma URL temporária para o cliente ver, sem domínio. Isso **não é lançamento de verdade**: os itens de domínio ficam ⏸️ conforme o FLUXO e o veredito passa a ser sobre mostrar ao cliente, não sobre publicar.

No modo preview, o veredito só considera bloqueio o que expõe alguém agora: segredo no build, chave de produção em uso, dado de teste indo para banco de produção, formulário que não chega, e a URL temporária indexável pelo Google. Acrescente uma linha dizendo que o pré-lançamento precisa ser refeito quando o domínio entrar, porque vários itens foram verificados na URL temporária.

## Fase 1: Decida o que se aplica

Só verifique os grupos cujo gatilho existe. O resto vai para a lista N/A com motivo.

## Fase 2: Checklist

### Pendências das auditorias anteriores (se existe `auditorias/`)

1. Para cada pendência registrada, verifique no código ou no site se **ainda está aberta**.
2. Viram **bloqueio de lançamento**:
   - git: segredo exposto cuja chave ainda não foi rotacionada
   - segurança: pendência de severidade Crítica ou Alta
   - LGPD: rastreamento (analytics/pixel) ativo sem consentimento, ou coleta de dados pessoais sem política de privacidade
   - acessibilidade: pendência de impacto Bloqueante
   - conversão: CTA principal quebrado ou sem destino
   - interface: ação irreversível sem confirmação, ou envio que pode ser duplicado
   
   As demais pendências não bloqueiam; apenas liste.
3. Itens que registrei como "decidi não corrigir" **não bloqueiam**. Apenas liste-os: eles entram nas recomendações não aplicadas do `ENTREGA.md`, se o projeto for para cliente.

### Pontos críticos em produção (sempre)

Estes itens repetem, de forma rápida, o essencial de cada auditoria, porque algumas coisas só aparecem no site publicado.

4. **Domínio definitivo em tudo:** `canonical`, `og:url`, `og:image` (URL absoluta), `sitemap.xml`, a linha `Sitemap:` do `robots.txt` e URLs do schema apontam para o domínio final, e não para `localhost`, `*.vercel.app`, `*.netlify.app` ou domínio de teste.
5. **Nenhum segredo no build:** procure no código gerado (`dist/`, `build/`, `.next/`, `out/`) padrões de chaves secretas (`sk_`, `service_role`, `secret`, `private`, tokens de API). Qualquer ocorrência é bloqueio.
6. **HTTPS e security headers na resposta real:** confira com `curl -I https://dominio` ou https://securityheaders.com que os headers configurados estão chegando de verdade, não só no arquivo de configuração. Certificado válido; `http` redireciona para `https`; versão com e sem `www` redireciona para uma só.
7. **Indexação liberada:** nenhum `noindex` ou `robots.txt` bloqueando tudo que tenha sobrado do desenvolvimento.
8. **Performance real:** PageSpeed Insights (mobile) na URL de produção. Registre LCP, INP, CLS e a nota. Se piorou em relação a `auditorias/11-performance.md`, aponte o provável motivo (script novo, imagem trocada...).
9. **Acessibilidade rápida:** Lighthouse ou axe na URL de produção; registre a nota e erros novos.

### Sempre

10. **Conteúdo provisório:** nenhum "lorem ipsum", "TODO", "teste", "Título aqui", imagem placeholder, telefone/e-mail fictício ou texto do template original. Procure também todo `TODO(conteudo)` deixado no `03-desenvolver` (lista em `auditorias/03-desenvolvimento.md`): cada um que sobrou é bloqueio.
11. **Links:** nenhum link quebrado, `href="#"` sem função, link para `localhost`/`127.0.0.1` ou para o ambiente de teste. Links de redes sociais apontam para os perfis certos do cliente.
12. **Dados de contato corretos e consistentes** em todo o site: telefone, e-mail, endereço, horário, nome da empresa. Liste todos que aparecem para eu conferir com o cliente.
13. **Build de produção** roda sem erros; sem `console.log` de debug esquecido; sem erros no console do navegador.
14. **Dependências de produção:** rode a visão de produção do `audit` (`npm audit --omit=dev` ou o equivalente do gerenciador). **Crítica ou Alta em dependência de produção é bloqueio** — é código de terceiro indo para o navegador ou para o binário do usuário. Confira antes a seção "Aceitas" de `auditorias/dependencias.md`: item já aceito com motivo válido não é bloqueio, é registro. Vulnerabilidade só em dependência de desenvolvimento não bloqueia. Também confirme que o **lockfile está versionado**: sem ele, o que foi auditado não é necessariamente o que foi publicado.
15. **Verificação automática:** se o projeto tem suíte, ela passa inteira — e sem caso pulado (`.only`, `skip`, asserção comentada) mascarando o resultado. Suíte vermelha é bloqueio; suíte verde rodando três casos de trinta é pior, porque engana. Projeto de nível 1 (landing page) não tem suíte, e isso não é pendência: os itens deste checklist são a verificação dele.
16. **`TODO(doc)` pendentes:** trecho escrito sem confirmação na documentação (padrão `.claude/lz/DOCUMENTACAO.md`) precisa estar testado funcionando, ou vira pendência explícita no veredito.
17. **Variáveis de ambiente** necessárias estão configuradas na hospedagem (liste quais o projeto espera) e apontam para os serviços de **produção**, não de teste.
18. **Identidade:** favicon, title de cada página e preview de compartilhamento (Open Graph) aparecendo corretamente. Sugira testar colando o link numa conversa do WhatsApp.
19. **Página 404** existe e tem caminho de volta.
20. **Responsivo:** revise o layout nas larguras ~360px, ~768px e ~1280px. Aponte textos cortados, elementos sobrepostos, rolagem horizontal, botões pequenos demais.
21. **Navegadores:** sinalize recursos que podem falhar no Safari/iOS (ex.: `100vh` em mobile, alguns recursos de CSS recentes, autoplay de vídeo). Teste manual em iPhone e Android fica como **🔍**.
22. **Rodapé:** ano do copyright atual (ou automático), links de política de privacidade/termos funcionando se existirem.

### Se tem formulário

23. O envio **chega de fato** ao destino certo (e-mail do cliente, planilha, banco, CRM). Descreva o teste: enviar, conferir recebimento e caixa de spam.
24. Mensagens de sucesso e de erro aparecem; o botão não permite envio duplo; o formulário funciona no celular.

### Se tem link ou botão de WhatsApp

25. Número correto no formato `55 + DDD + número`, mensagem pré-preenchida funcionando, abre no app do celular e no WhatsApp Web.

### Se tem analytics, Tag Manager ou pixel

26. Instalado com o ID de **produção** do cliente; eventos principais (envio de formulário, clique no WhatsApp, compra) disparando; não carrega em ambiente de desenvolvimento.
27. **Consentimento funcionando no site publicado:** antes de aceitar os cookies, nenhuma requisição para Google Analytics, Meta Pixel ou similares aparece na aba Rede do navegador; depois de aceitar, aparecem. Se falhar, é bloqueio.

### Se envia e-mails pelo domínio (formulário, cadastro, pedidos)

28. Remetente configurado com o domínio certo; registros SPF, DKIM e DMARC no DNS (**🔍 Verificar manualmente**); textos dos e-mails revisados, sem conteúdo de template.

### Se tem banco de dados

29. Migrations aplicadas em produção; dados de teste e usuários fictícios removidos; políticas de acesso (ex.: RLS) ativas **no banco de produção**, não só no de desenvolvimento.

### Se tem login

30. Cadastro, login, logout, recuperação de senha e confirmação de e-mail testados **no domínio de produção**. URLs de redirecionamento do provedor de autenticação (ex.: Site URL e Redirect URLs no Supabase, domínios autorizados no Firebase) apontam para o domínio final.

### Se tem pagamentos

31. Chaves de **produção** configuradas (não as de sandbox/teste); URL do webhook cadastrada no gateway apontando para produção; fluxo testado com uma transação real de valor baixo e estorno; e-mails e páginas de confirmação corretos.
32. Se for recorrente (assinatura/doação mensal): o cancelamento funciona e está claro para o usuário.

### Se é e-commerce

33. Preços, estoque, frete e cupons conferidos; e-mail de confirmação de pedido; páginas de troca/devolução e contato acessíveis antes da compra.

## Como verificar

- Cite **evidência concreta**: arquivo e linha, ou o que foi observado.
- Seja específico no que depende de teste manual: diga **o que fazer e o que deve acontecer**.

## Formato do relatório

1. **Perfil** (com o estado do deploy e quais auditorias foram registradas)
2. **Veredito:** 🟢 Pronto para lançar / 🟡 Lançar após corrigir os bloqueios / 🔴 Não lançar
3. **Bloqueios** (o que impede o lançamento), em lista curta, indicando de qual auditoria ou item vieram
4. **Tabela:** `# | Item | Status | Evidência | O que fazer`
   - Status: ✅ OK / ❌ Falha / 🔍 Testar manualmente
5. **Métricas de produção:** PageSpeed (LCP, INP, CLS, nota) e nota de acessibilidade
6. **Roteiro de testes manuais:** lista numerada, curta, para eu seguir no celular e no computador
7. **Itens N/A** com motivo

Depois do relatório, pare e pergunte o que devo corrigir.

## Registro

Depois das correções, salve um resumo em `auditorias/12-pre-lancamento.md` com: data, URL testada, veredito final, métricas de produção, pendências restantes e itens que decidi não corrigir, com o motivo. Esse arquivo é lido pela revisão geral e pelo `projeto-limpo`.
