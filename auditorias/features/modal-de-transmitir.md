# Modal de transmitir: tela pela miniatura e live privada pela lista
Data: 2026-10-06

## O que foi adicionado
- **Transmitir** abre um modal só (`Views/BroadcastDialog`): miniaturas das telas (atualizam a
  cada 1 s, como no Discord), "Quem pode ver" com os amigos e a senha da sala
- Sem caixa "Live privada": ninguém marcado = pública; alguém marcado = só essas pessoas.
  `BroadcastSettings.PrivateLive` passou a ser derivado de `InvitedIps` — "privada sem ninguém"
  (live invisível para todos) deixou de existir
- Frase embaixo da lista diz o que vai acontecer ("Pública — todos os seus amigos veem" /
  "Privada — só Ana e Diego veem esta live"), com cadeado laranja quando privada
- Toda live começa sem ninguém marcado; a tela da última live vem escolhida
- Durante a live, botão **Trocar tela** (mesmo modal, só com as telas) no lugar do ComboBox
- O ComboBox "Tela:" saiu da barra (era branco, com cara de Windows); a caixa de marcar ganhou
  estilo escuro no `App.xaml` — vale para o app inteiro (Configurações, "Fechar o Radmin também")
- Cada tela mostra a resolução e qual é a principal do Windows (nesta máquina a principal é a Tela 2)
- `RoomPasswordDialog` voltou a ser só do viewer
- `--demo`: duas telas fixas, miniaturas desenhadas (nunca a tela de verdade)

## Decisões (2026-10-06, com o dono)
- Trocar tela durante a live: sim, botão ao lado do Parar
- Miniaturas: atualizam ~1 s enquanto o modal está aberto
- Convidados: não são lembrados entre lives (evita live privada sem perceber)
- Layout: um modal só (telas, quem pode ver, senha)
- Fora desta versão: transmitir uma **janela** específica (o WindowHelper só captura tela inteira; mexe no encoder)

## Auditorias reaplicadas (só o código novo)
| # | Item | Auditoria | Status | Evidência |
|---|---|---|---|---|
| 1 | Sem amigos salvos | 06-interface | ✅ | `TxtNoFriends` no lugar da lista e da frase |
| 2 | Nenhuma tela / miniatura que falha | 06-interface | ✅ | `TxtNoScreens` (Assertive); miniatura nula vira ícone + nome, mantém a anterior; `CaptureThumbnail(Rectangle.Empty)` testado |
| 3 | Transmitir sem tela escolhida | 06-interface | ✅ | botão desabilitado com dica "Escolha uma tela"; 1 monitor vem escolhido |
| 4 | Nome longo | 06-interface | ✅ | `CharacterEllipsis` + tooltip "Nome · IP" |
| 5 | Nomes para o leitor de tela | 10-acessibilidade | ✅ | UIA: "Tela 1, 1920 × 1080 · principal", caixas com o nome do amigo, campo "Senha da sala (opcional)", frase com `LiveAnnouncer` Polite |
| 6 | Ordem do Tab sem paradas vazias | 10-acessibilidade | ✅ | `ScrollViewer` da lista tirava um Tab sem nome — `Focusable="False"` (achado na conferência) |
| 7 | Teclado: foco inicial, setas, Enter, Esc | 10-acessibilidade | ⚠️ | Foco abre no card da tela (UIA); Enter escolhe o card focado, Enter numa caixa marca/desmarca, Esc fecha — conferido pelo código, sem envio de teclas (regra do /12: automação de teclado só com o PC liberado) |
| 8 | Contraste da caixa de marcar | 10-acessibilidade | ✅ | borda #6A6A75 sobre #1E1E24/#18181E ≥ 3:1; marcada #0078D4 com check branco |
| 9 | Custo das miniaturas | 11-performance | ✅ | Só com o modal aberto (timer para no `Closed`), fora da thread de UI, pula se a anterior não acabou; medido 66–136 ms por tela 1080p a cada 1 s |
| 10 | Regra pública/privada não vaza | 07-seguranca | ✅ | `BroadcastVisibilityTests`: convidado entra, não convidado é recusado; sem convidados todos os amigos entram |

Verificação completa: 269 testes (eram 254) + smoke do .exe, 0 avisos de build. Fluxo inteiro
conferido na tela em `--demo` por UI Automation (modal → marcar 2 → Tela 2 → selo PRIVADA →
Trocar tela abre com a Tela 2). Miniatura real conferida nas duas telas desta máquina com teste
temporário (não versionado, sem salvar imagem).

## Pendente
- GIF do README (`docs/images/demo.gif`) ainda mostra o modal antigo — Baixa. Foi gravado com o
  cursor de verdade; regravar mexe no mouse, então só com o PC liberado e o dono de acordo.
  Os 5 prints foram regravados
- Live real: não exigida (captura, encoder e rede não mudaram), mas a próxima live com amigo
  confere a privada com 1 convidado na prática

## Pré-lançamento
O veredito do /12 não é invalidado: a mudança é de interface e da origem do `PrivateLive`,
coberta por teste. Próxima versão: **menor** (2.3.0).
