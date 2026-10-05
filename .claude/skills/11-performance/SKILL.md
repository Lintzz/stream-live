---
name: 11-performance
description: "Audita performance e Core Web Vitals: imagens, fontes, bundle de JavaScript, CSS, scripts de terceiros, estabilidade visual, cache e consultas ao banco. Use quando o usuario pedir para otimizar a velocidade do site, melhorar LCP, CLS ou nota do PageSpeed."
---

# Auditoria de Performance

Leia primeiro `.claude/lz/FLUXO.md`: ele define a ordem das etapas, o protocolo de perguntas, o arquivo `auditorias/contexto.md`, o vocabulario de status e o formato do registro. As regras de la valem aqui.

Você é um especialista em performance web revisando este projeto. Seu trabalho nesta etapa é **analisar e relatar**, não corrigir. Não altere nenhum arquivo até eu aprovar.

Metas de referência (Core Web Vitals, medidas no **celular**):
- **LCP** (maior elemento visível carregado) < 2,5 s
- **INP** (resposta a cliques e toques) < 200 ms
- **CLS** (layout pulando enquanto carrega) < 0,1

## Fase 0: Entenda o projeto

Monte um **Perfil do projeto** curto:

- **Tipo:** landing page / institucional / SPA / app full-stack / e-commerce
- **Stack, bundler e hospedagem** (Vite, Next, Astro, HTML puro... / Vercel, Netlify, VPS...)
- **Qual é o elemento principal da primeira dobra** (provável LCP): imagem do hero, vídeo, título?
- **Scripts de terceiros:** Google Analytics/Tag Manager, Meta Pixel, chat, widgets, mapas, embeds de vídeo
- **Tem backend ou banco?** (para decidir se o grupo de servidor se aplica)

Se possível, rode um build de produção e anote o tamanho dos arquivos gerados. Se houver URL publicada, sugira medir em https://pagespeed.web.dev (versão mobile).

## Fase 0.5: Contexto e perguntas

Leia `auditorias/contexto.md` e siga o protocolo de perguntas do FLUXO: no máximo 5 perguntas **no total**, todas de uma vez, com opções e recomendação marcada, e nada que já esteja no contexto. As perguntas comuns (site publicado e domínio definitivo, cliente ou projeto próprio, conteúdo final ou rascunho, o que está planejado e ainda não foi feito) só entram se o contexto não responder. Das específicas desta etapa, abaixo, fique com as que mudam o resultado:

1. Tenho as imagens originais em alta resolução para gerar versões otimizadas?
2. A hospedagem é plano gratuito ou pago? Isso muda o que dá para configurar (cache, CDN, compressão).
3. Posso trocar ou remover bibliotecas, ou o projeto precisa continuar como está?

## Fase 1: Decida o que se aplica

Só audite os grupos cujo gatilho existe. O resto vai para a lista N/A com motivo.

## Fase 2: Checklist

### Imagens (sempre que houver imagens)

1. **Formato:** WebP ou AVIF para fotos; SVG para ícones e logos; PNG só quando precisa de transparência e SVG não serve.
2. **Tamanho real:** nenhuma imagem muito maior do que é exibida (ex.: foto de 4000px exibida em 400px). Liste as imagens mais pesadas com o peso atual.
3. **Responsivas:** `srcset`/`sizes` ou o componente de imagem do framework, para o celular não baixar a versão de desktop.
4. **Lazy loading:** `loading="lazy"` em imagens **abaixo da dobra**. A imagem do hero **não** pode ter lazy; deve ter `fetchpriority="high"` (e preload, se for background em CSS).
5. **Dimensões declaradas:** `width` e `height` (ou `aspect-ratio`) em todas, para evitar CLS.

### Fontes (se usa fontes customizadas)

6. `font-display: swap`; poucos pesos e estilos; preferir self-host ou `preconnect` ao Google Fonts; preload da fonte usada acima da dobra; formato WOFF2.

### JavaScript e CSS

7. **Bundle:** tamanho do JS final; bibliotecas pesadas usadas para pouca coisa (moment, lodash inteiro, biblioteca de ícones inteira, jQuery só para um efeito); code splitting por rota se for SPA.
8. **Scripts de terceiros:** carregados com `defer`/`async` ou após a interação; nada bloqueando a renderização. Embeds de YouTube/mapas com "fachada" (imagem que só carrega o player ao clicar).
9. **CSS:** sem CSS enorme não utilizado (frameworks inteiros importados); nada bloqueando a primeira pintura sem necessidade.
10. **Animações:** preferir `transform`/`opacity`; bibliotecas de animação pesadas justificadas; nada que trave a rolagem no celular.

### Vídeo (se tem)

11. Vídeo de fundo comprimido, `muted`, `playsinline`, com `poster`, e considerar não carregar no celular. Vídeos longos hospedados em plataforma, não no próprio site.

### Estabilidade visual (CLS)

12. Nada empurrando o conteúdo depois de carregar: banners de cookie, fontes trocando de tamanho, imagens sem dimensão, embeds sem espaço reservado, conteúdo injetado no topo.

### Entrega (depende da hospedagem)

13. Compressão (Brotli/Gzip) ativa; cache longo para arquivos com hash no nome; HTML com cache curto; uso de CDN. Indique onde configurar para a hospedagem detectada ou marque **🔍 Verificar manualmente**.

### Se tem backend ou banco de dados

14. Tempo de resposta do servidor (TTFB); consultas repetidas em loop (N+1); ausência de índices em colunas filtradas; respostas que poderiam ser cacheadas; paginação em listas grandes.

### Se é e-commerce ou tem muitas páginas/itens

15. Listagens com paginação ou carregamento incremental; miniaturas em tamanho de miniatura; páginas de produto que não carregam o catálogo inteiro.

## Como investigar

- Cite **evidência concreta**: arquivo, linha, nome e peso do arquivo.
- Estime o ganho quando possível (ex.: "hero.jpg 2,3 MB → ~180 KB em WebP 1600px").
- Não proponha reescrever o projeto ou trocar de framework; foque em correções proporcionais ao tamanho do site.

## Formato do relatório

1. **Perfil do projeto** (incluindo o provável elemento LCP)
2. **Resumo:** os 3 maiores gargalos
3. **Tabela:** `# | Item | Status | Impacto | Evidência | Correção sugerida | Ganho estimado`
   - Status: ✅ OK / ⚠️ Parcial / ❌ Falha / 🔍 Verificar manualmente
   - Impacto: Alto / Médio / Baixo
4. **Plano em ordem:** correções rápidas de alto impacto primeiro
5. **Itens N/A** com motivo

Depois do relatório, pare e pergunte o que devo aplicar.

## Registro

Depois que eu aprovar e as correções forem aplicadas, salve um resumo em `auditorias/11-performance.md` (na raiz do projeto, fora da pasta pública) com: data, itens corrigidos, pendências, itens que decidi não corrigir (com o motivo) e as métricas medidas, se houver. Esse arquivo é lido pelo pré-lançamento, pela revisão geral e, na entrega ao cliente, pelo `projeto-limpo`. Se o repositório for público, siga a ressalva do FLUXO sobre `auditorias/`: avise e ofereça ignorar a pasta, sem decidir sozinho.
