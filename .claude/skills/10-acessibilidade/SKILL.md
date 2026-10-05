---
name: 10-acessibilidade
description: "Audita acessibilidade segundo a WCAG 2.2 AA: contraste, navegacao por teclado, foco visivel, semantica, rotulos de botoes com icone, formularios, modais e movimento. Use quando o usuario pedir auditoria de acessibilidade, revisao para leitores de tela ou conformidade com a Lei Brasileira de Inclusao."
---

# Auditoria de Acessibilidade

Leia primeiro `.claude/lz/FLUXO.md`: ele define a ordem das etapas, o protocolo de perguntas, o arquivo `auditorias/contexto.md`, o vocabulario de status e o formato do registro. As regras de la valem aqui.

Você é um especialista em acessibilidade web revisando este projeto, tendo como referência a **WCAG 2.2 nível AA**. Seu trabalho nesta etapa é **analisar e relatar**, não corrigir. Não altere nenhum arquivo até eu aprovar.

Contexto: acessibilidade beneficia pessoas com deficiência visual, motora, auditiva e cognitiva, mas também quem usa o celular sob sol forte, com uma mão só ou com internet ruim. No Brasil, a Lei Brasileira de Inclusão (Lei 13.146/2015) prevê acessibilidade em sites de empresas com sede no país.

## Fase 0: Entenda o projeto

Monte um **Perfil** curto:

- **Tipo:** landing page / institucional / SPA / app com login / e-commerce
- **Stack** (HTML puro, React, Vue, Next...) e bibliotecas de componentes (algumas já resolvem boa parte da acessibilidade)
- **Componentes interativos presentes:** menu mobile, carrossel/slider, modal/popup, abas, acordeão/FAQ, dropdown, formulários, vídeo/áudio, mapas, animações, banner de cookies, botão flutuante

## Fase 0.5: Contexto e perguntas

Leia `auditorias/contexto.md` e siga o protocolo de perguntas do FLUXO: no máximo 5 perguntas **no total**, todas de uma vez, com opções e recomendação marcada, e nada que já esteja no contexto. As perguntas comuns (site publicado e domínio definitivo, cliente ou projeto próprio, conteúdo final ou rascunho, o que está planejado e ainda não foi feito) só entram se o contexto não responder. Das específicas desta etapa, abaixo, fique com as que mudam o resultado:

1. Existe exigência formal de acessibilidade (órgão público, edital, contrato, política da empresa)?
2. As cores e a tipografia são fechadas pela marca do cliente ou posso ajustar para melhorar contraste?

## Fase 1: Decida o que se aplica

Só audite os grupos cujo gatilho existe. O resto vai para a lista N/A com motivo.

## Fase 2: Checklist

### Sempre

1. **Idioma:** `<html lang="pt-BR">` (ou o correto).
2. **Estrutura semântica:** uso de `<header>`, `<nav>`, `<main>`, `<footer>`, `<section>` com títulos; um `<h1>` e hierarquia de headings lógica, sem pular níveis por estilo.
3. **Imagens:** `alt` descritivo em imagens informativas; `alt=""` em decorativas; imagens com texto dentro têm o texto no `alt`; SVGs informativos com título ou `aria-label`.
4. **Contraste:** texto normal ≥ 4,5:1, texto grande ≥ 3:1, ícones e bordas de campos ≥ 3:1. Verifique especialmente texto sobre imagem no hero, texto cinza claro e placeholders. Aponte as combinações de cor que falham.
5. **Cor não é a única informação:** erros, estados e links não dependem só da cor.
6. **Teclado:** tudo que é clicável funciona com Tab, Enter e Espaço; a ordem do Tab segue a ordem visual; nada prende o foco.
7. **Foco visível:** nenhum `outline: none` sem um substituto visível.
8. **Elementos certos:** ações são `<button>`, navegação é `<a href>`. Aponte `<div>` ou `<span>` com `onClick` sem semântica e sem suporte a teclado.
9. **Botões só com ícone** (menu, redes sociais, WhatsApp, fechar, carrinho) têm `aria-label` ou texto oculto.
10. **Textos de link** fazem sentido fora do contexto (evitar vários "Saiba mais" / "Clique aqui" iguais sem complemento).
11. **Zoom:** a página não bloqueia zoom (`user-scalable=no` ou `maximum-scale=1`) e continua usável com texto a 200%.
12. **Área de toque:** alvos clicáveis com no mínimo 24×24px (recomendado 44×44px no celular) e espaçamento suficiente entre eles.
13. **Link "pular para o conteúdo"** em páginas com menu extenso (prioridade baixa em landing page simples).

### Se tem formulário

14. Cada campo tem `<label>` associado (placeholder não substitui label); campos obrigatórios indicados de forma que não seja só cor; mensagens de erro em texto, próximas ao campo e anunciadas para leitores de tela; `autocomplete` nos campos comuns (nome, e-mail, telefone); captcha com alternativa acessível.

### Se tem animações, carrossel ou movimento

15. Respeita `prefers-reduced-motion`; carrossel com troca automática tem botão de pausar e controles acessíveis por teclado; nada pisca mais de 3 vezes por segundo.

### Se tem modal, popup, menu mobile ou dropdown

16. Ao abrir, o foco vai para dentro; Tab fica dentro enquanto aberto; Esc fecha; ao fechar, o foco volta para o botão que abriu; botões de abrir usam `aria-expanded`.

### Se tem abas, acordeão/FAQ ou componentes customizados

17. Padrões ARIA corretos (ou elementos nativos como `<details>`/`<summary>`); estado aberto/fechado comunicado.

### Se tem vídeo ou áudio

18. Controles acessíveis; nada com som tocando automaticamente; legendas em vídeos com fala; vídeo de fundo decorativo sem áudio e com opção de pausar.

### Se é SPA (troca de rota sem recarregar a página)

19. Ao mudar de página, o título da aba muda e o foco ou um anúncio indica a nova página para leitores de tela.

### Se é e-commerce ou app com login

20. Fluxo completo (buscar, adicionar ao carrinho, finalizar compra, login, cadastro) feito só com teclado; mensagens dinâmicas ("item adicionado", erros) anunciadas via `aria-live`; tempo limite de sessão com aviso.

## Como investigar

- Cite **evidência concreta**: componente/arquivo, linha e o trecho.
- Se conseguir, rode uma ferramenta automática (axe-core, Lighthouse) e inclua o resultado, lembrando que ela detecta só parte dos problemas.
- O que exigir teste humano, marque **🔍 Testar manualmente** e diga como: navegar só com teclado; usar leitor de tela (NVDA no Windows, VoiceOver no iPhone/Mac, TalkBack no Android); zoom de 200%.

## Formato do relatório

1. **Perfil do projeto**
2. **Resumo:** os problemas que mais impedem alguém de usar o site
3. **Tabela:** `# | Item | Status | Impacto | Evidência | Correção sugerida`
   - Status: ✅ OK / ⚠️ Parcial / ❌ Falha / 🔍 Testar manualmente
   - Impacto: **Bloqueante** (a pessoa não consegue usar) / Alto / Médio / Baixo
4. **Roteiro de teste manual** curto (teclado + leitor de tela no celular)
5. **Itens N/A** com motivo

Depois do relatório, pare e pergunte o que devo corrigir.

## Registro

Depois que eu aprovar e as correções forem aplicadas, salve um resumo em `auditorias/10-acessibilidade.md` (na raiz do projeto, fora da pasta pública) com: data, itens corrigidos, pendências (com impacto) e itens que decidi não corrigir, com o motivo. Esse arquivo é lido pelo pré-lançamento, pela revisão geral e, na entrega ao cliente, pelo `projeto-limpo`. Se o repositório for público, siga a ressalva do FLUXO sobre `auditorias/`: avise e ofereça ignorar a pasta, sem decidir sozinho.
