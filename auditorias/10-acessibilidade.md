# Acessibilidade — registro
Data: 2026-10-05
Referência: WCAG 2.2 AA aplicada a app WPF. Medição pela árvore de UI Automation (o que o Narrador/NVDA lê), com roteiros de Tab em `--demo` e contraste pela fórmula da WCAG.

## Corrigido
- Bloqueante — Abrir a live de um amigo só com mouse: card virou Button (FriendCardButton), nome com estado e ação (Friend.AccessibleName, testado). Conferido: Tab chega na Ana, Enter abre a live
- Alto — Com live aberta, a lista de amigos só voltava pelo puxador de mouse (achado durante a correção; o relatório tinha classificado como Baixo): puxadores viraram Buttons. Conferido: duas lives abertas só com teclado
- Alto — 8+ botões de ícone anunciados como glifo: AutomationProperties.Name em todos (inclusive "Remover Ana", "Sair da live de Ana"). Conferido: nenhum botão com nome de glifo
- Alto — Painel de configurações: foco entra ao abrir, Tab circula dentro, Esc fecha, foco volta à engrenagem. Conferido. Aba "Settings" → "Geral"
- Médio — Foco caindo em controles invisíveis do player: barra fora de cena sem live, acende com foco; listas deixaram de ser paradas vazias; puxador fora do Tab sem live. Conferido
- Médio — Contraste: cinzas 2,77–3,95:1 → #9A9AA4 (>6:1); branco no azul 3,22:1 → fundo #0078D4 (4,53:1); borda de campo 1,54:1 → #6A6A75 (3,1:1). Conferido visualmente
- Médio — Rótulos de campo soltos: LabeledBy/Name em todos os campos. Conferido pelos nomes na árvore
- Médio — Mensagens dinâmicas mudas: LiveAnnouncer (LiveRegionChanged). Conferido: evento recebido com o texto do erro do IP
- Médio (bug geral, achado aqui) — ShowTransientStatus escrevia na prévia escondida; avisos nunca apareciam. Agora aviso flutuante no rodapé. **Não conferido na tela**: o --demo não dispara esses avisos
- Baixo — Foco pouco visível: AppFocusVisual (contorno azul 2 px) em todos os estilos; botão de sair do quadro aparece com foco. Conferido
- Alto (achado no /12) — Com live aberta, os cards da lista de amigos ficavam com o par de automação velho (o ItemsControl guarda um peer por item e não o troca quando os containers são recriados): o Tab chegava neles, mas o leitor de tela anunciava "Stream Live". LiveItemsControl calcula os filhos da árvore visual atual. Conferido: "Ana, assistindo. Enter para sair da live" depois de reabrir a lateral. O "Diego pelo Tab: True" relatado antes era falso positivo
- Baixo — Link do GitHub só de mouse: Hyperlink "Código no GitHub". Conferido (focável, papel de link)

## Pendente
- 🔍 Teste humano com o Narrador ligado (roteiro abaixo) — Médio
- 🔍 Texto do Windows em 150% (Acessibilidade → Tamanho do texto) — Baixo

## Decidi não corrigir
- Temas de alto contraste do Windows: cores fixas no XAML; adaptar é trabalho grande e à parte

## Roteiro de teste manual
1. Ctrl+Win+Enter (Narrador). Sem mouse: abrir a live de um amigo, tirar o som, tela cheia, sair com Esc, mostrar amigos, abrir uma segunda live, abrir e fechar configurações
2. Errar o IP em "Gerenciar amigos" e ouvir o erro
3. Tamanho do texto a 150% e conferir cortes

## Pendências de domínio
- Não se aplica
