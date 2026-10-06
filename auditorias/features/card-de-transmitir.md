# Card de transmitir na lista de amigos + janela maior ao abrir
Data: 2026-10-06

## O que foi adicionado
- A faixa do topo (só com o botão Transmitir quando parado, ~70 px o tempo todo) saiu. O
  Transmitir e todo o estado da live (AO VIVO, PRIVADA, tela, quem assiste, avisos, Trocar
  tela, prévia, Parar) moram num card no rodapé da lista de amigos, como o painel do Discord
- Selo "● AO VIVO · N" na barra de título durante a live: a lista recolhe sozinha com uma live
  de amigo aberta, e o card vai junto. Clicar no selo reabre a lista e põe o foco no Parar;
  com o aviso de saúde ativo o selo fica vermelho cheio
- Janela abre em 1200×760 (era 900×550, menor que o modal de transmitir), encolhida até a área
  útil em tela pequena (`WindowHelper.FitToWorkArea`)
- O modal de transmitir é puxado para dentro da tela do monitor em que abre
  (`WindowHelper.ClampToArea`) quando a janela foi diminuída à mão
- Sai o puxador "Esconder controles de transmissão" e o estado `_topPanelOpen`

## Ajuste depois de ver na tela (2026-10-06)
- O card de transmitir ficava dentro do card de amigos e parecia colado nele: agora são dois
  cards separados, com 8 px entre eles, e o de transmitir no mesmo estilo do de amigos
- "Gerenciar amigos" virou uma engrenagem no canto superior direito do card de amigos (nome e
  dica para leitor de tela e mouse); com a lista vazia, o "Adicionar amigos" no meio ficou
  grande e azul (botão primário). Estado vazio não conferido na tela: o `--demo` sempre tem
  amigos, e abrir o app de verdade subiria o Radmin, que estava fechado

## Decisões (2026-10-06, com o dono)
- Tamanho: sempre 1200×760 ao abrir, sem lembrar o último
- Card no rodapé da lista; "Gerenciar amigos" logo acima
- Selo na barra de título durante a live
- Fora: miniatura da própria tela dentro do card (o olho já abre a prévia)

## Auditorias reaplicadas (só o código novo)
| # | Item | Auditoria | Status | Evidência |
|---|---|---|---|---|
| 1 | Card parado / ao vivo / com aviso | 06-interface | ✅ | Conferido na tela em `--demo`; margens nas peças, card parado só com o botão |
| 2 | Porta 8080 ocupada | 06-interface | ⚠️ | Transmitir desabilitado com o motivo no tooltip, agora visível desabilitado (`ShowOnDisabled`); não reproduzido na tela (o `--demo` não sobe a 8080) |
| 3 | Texto longo no card (230 px) | 06-interface | ✅ | Avisos em grade com `TextWrapping`; "Esta janela está na tela transmitida" quebra em 2 linhas |
| 4 | Modal dentro da janela/tela | 06-interface | ✅ | UIA: modal 600×629 inteiro dentro da janela 1200×760; `ClampToArea` com 4 casos de teste (abaixo, 2ª tela à esquerda, maior que a tela) |
| 5 | Ordem do Tab | 10-acessibilidade | ✅ | UIA: amigos → Gerenciar → Trocar tela → prévia → Parar; selo é o primeiro da barra de título |
| 6 | Nomes de botões só com ícone | 10-acessibilidade | ✅ | "Trocar tela", "Ver o que está sendo transmitido", selo "Ao vivo, 2 pessoas assistindo. Mostrar amigos" |
| 7 | Foco depois do selo | 10-acessibilidade | ✅ | UIA: foco vai para "Parar" |

Verificação completa: 280 testes (eram 269) + smoke do .exe, 0 avisos. Prints do README
regravados em 1200×760.

## Pendente
- ~~GIF do README~~ regravado em 2026-10-06 (cursor desenhado nos quadros, sem mexer no mouse)
- GIF do README segue com a interface antiga (mesma pendência do modal-de-transmitir.md) — Baixa
- O `--demo` não mostra o aviso de saúde: o selo vermelho foi conferido só pelo código

## Pré-lançamento
Não invalida o veredito do /12: layout, sem mudança de rede ou captura. Entra na 2.3.0 (menor).
