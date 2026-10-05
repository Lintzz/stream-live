---
name: 09-seo
description: "Audita SEO tecnico: titles, meta descriptions, headings, alts, Open Graph, canonical, robots, sitemap, dados estruturados Schema.org, SPA sem HTML inicial e negocio local. Use quando o usuario pedir auditoria de SEO, otimizacao para o Google, meta tags ou schema."
---

# Auditoria de SEO

Leia primeiro `.claude/lz/FLUXO.md`: ele define a ordem das etapas, o protocolo de perguntas, o arquivo `auditorias/contexto.md`, o vocabulario de status e o formato do registro. As regras de la valem aqui.

Você é um especialista em SEO técnico revisando este projeto web. Seu trabalho nesta etapa é **analisar e relatar**, não corrigir. Não altere nenhum arquivo até eu aprovar.

(Performance e Core Web Vitals têm uma auditoria própria. Aqui, só mencione performance se algo for gritante.)

## Fase 0: Entenda o projeto

Explore o repositório e monte um **Perfil do projeto** curto:

- **Tipo:** landing page de uma página / site institucional multipágina / blog / e-commerce / sistema com login
- **Stack e forma de renderização:** HTML estático, SSG, SSR ou SPA renderizada só no navegador (CSR). Isso importa: uma SPA pura pode entregar HTML quase vazio para o Google.
- **Negócio:** do que se trata, qual o público, qual a ação principal que o visitante deve tomar
- **Negócio local?** Atende uma cidade/região (clínica, loja, prestador de serviço)?
- **Idiomas:** um ou vários?
- **Páginas:** liste as páginas/rotas existentes
- **Domínio:** está definido em algum lugar (config, sitemap, canonical)?

Se algo for ambíguo, diga o que assumiu.

## Fase 0.5: Contexto e perguntas

Leia `auditorias/contexto.md` e siga o protocolo de perguntas do FLUXO: no máximo 5 perguntas **no total**, todas de uma vez, com opções e recomendação marcada, e nada que já esteja no contexto. As perguntas comuns (site publicado e domínio definitivo, cliente ou projeto próprio, conteúdo final ou rascunho, o que está planejado e ainda não foi feito) só entram se o contexto não responder. Das específicas desta etapa, abaixo, fique com as que mudam o resultado:

1. O negócio atende uma cidade ou região específica?
2. Por quais termos o cliente quer ser encontrado no Google?
3. O cliente já tem Google Business Profile e Search Console?
4. Existe intenção de manter um blog ou publicar conteúdo com frequência?

## Fase 1: Decida o que se aplica

Os grupos abaixo têm gatilhos. Só audite o grupo se o gatilho existir. Itens que não se aplicam vão para a lista de N/A no final com o motivo em uma linha.

## Fase 2: Checklist

### Sempre

1. **Title por página:** único em cada página, ~50–60 caracteres, termo principal no começo e marca no final. Nada de "Home", "Document" ou "Vite + React".
2. **Meta description por página:** única, ~140–160 caracteres, descreve o que a pessoa encontra e termina com um convite à ação. Se faltar, **escreva sugestões** para cada página com base no conteúdo real dela.
3. **Headings:** exatamente um `<h1>` por página, com o assunto principal; hierarquia `h2`/`h3` lógica, sem pular níveis só por estilo.
4. **Imagens:** todas com `alt` descritivo (imagens puramente decorativas com `alt=""`); nomes de arquivo legíveis (`corte-masculino.webp`, não `IMG_2931.jpg`).
5. **Open Graph e Twitter Card:** `og:title`, `og:description`, `og:image` (1200×630), `og:url`, `og:type`. É o que aparece quando o link é compartilhado no WhatsApp e redes sociais, então trate como prioridade alta.
6. **Básico técnico:** `<html lang="pt-BR">` (ou o idioma correto), meta viewport, favicon e apple-touch-icon, `<link rel="canonical">` com URL absoluta.
7. **Indexação:** `robots.txt` existe e não bloqueia o site por engano; `sitemap.xml` existe e está referenciado no robots; nenhum `noindex` esquecido de ambiente de teste.
8. **URLs e links:** URLs legíveis e em minúsculas, links internos entre páginas relevantes, textos de link descritivos (não "clique aqui"), página 404 útil.

### Dados estruturados (Schema.org)

9. Escolha os tipos **de acordo com o negócio detectado** na Fase 0, em JSON-LD:
   - Qualquer empresa ou marca: `Organization` (ou `Person` para portfólio pessoal) + `WebSite`
   - Negócio local: `LocalBusiness` ou o subtipo mais específico (`Dentist`, `HairSalon`, `Restaurant`...) com nome, endereço, telefone, horário e área atendida
   - Página com perguntas frequentes visíveis: `FAQPage`
   - Produtos à venda: `Product` com `Offer`
   - Artigos de blog: `Article` / `BlogPosting`
   - Site com várias camadas de navegação: `BreadcrumbList`
   
   Regra: só marque em schema o que **realmente aparece** na página. Ao final, indique validar em https://search.google.com/test/rich-results e https://validator.schema.org.

### Se é SPA renderizada só no navegador (CSR)

10. Verifique se o HTML inicial (antes do JavaScript) já contém título, description e conteúdo. Se não, recomende pré-renderização, SSG ou a opção mais simples para a stack detectada, e garanta que cada rota tenha seu próprio title/description.

### Se tem várias páginas

11. Títulos e descriptions não duplicados entre páginas; cada página focada em um assunto/serviço diferente; sitemap listando todas.

### Se é negócio local

12. Nome, endereço e telefone escritos da mesma forma no site inteiro (e iguais ao Google Business Profile, que deve ser **verificado manualmente**); cidade/região mencionada naturalmente nos textos, title e schema; mapa incorporado ou link para ele.

### Se tem mais de um idioma

13. `hreflang` entre as versões e `lang` correto em cada uma.

### Se é e-commerce

14. Title/description únicos por produto e categoria; schema de produto com preço e disponibilidade; filtros e ordenações que não geram milhares de URLs indexáveis duplicadas (usar canonical ou noindex).

### Blog (avaliação, não implementação)

15. **Não crie o blog automaticamente.** Avalie se faz sentido: o negócio tem assuntos que o público pesquisa antes de comprar? O cliente terá como produzir conteúdo com frequência? Se sim, proponha: estrutura de URL (`/blog/slug`), template de post com os itens acima e **5 pautas de topo de funil** (dúvidas reais que o público pesquisa no Google), cada uma com título sugerido e intenção de busca. Se não fizer sentido, diga por quê.

## Como investigar

- Cite **evidência concreta**: arquivo e linha, ou o trecho atual.
- Quando o problema é texto ausente ou fraco (title, description, alt, h1), **entregue a sugestão já escrita**, baseada no conteúdo real da página. Não invente serviços, preços ou informações que não estão no site.
- O que depende de ferramentas externas (Search Console, Google Business Profile), marque **🔍 Verificar manualmente** e diga onde olhar.

## Formato do relatório

1. **Perfil do projeto**
2. **Resumo:** principais problemas em 3–5 linhas
3. **Tabela:** `# | Item | Status | Prioridade | Evidência | Correção sugerida`
   - Status: ✅ OK / ⚠️ Parcial / ❌ Falha / 🔍 Verificar manualmente
   - Prioridade: Alta / Média / Baixa
4. **Textos sugeridos:** title e description por página (e alts, se faltarem), prontos para colar
5. **Schema sugerido:** o JSON-LD proposto
6. **Itens N/A** com motivo

Depois do relatório, pare e pergunte o que devo aplicar.

## Registro

Depois que eu aprovar e as correções forem aplicadas, salve um resumo em `auditorias/09-seo.md` (na raiz do projeto, fora da pasta pública) com: data, itens corrigidos, pendências e itens que decidi não corrigir, com o motivo. Esse arquivo é lido pelo pré-lançamento, pela revisão geral e, na entrega ao cliente, pelo `projeto-limpo`. Se o repositório for público, siga a ressalva do FLUXO sobre `auditorias/`: avise e ofereça ignorar a pasta, sem decidir sozinho.
