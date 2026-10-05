# Interface — registro
Data: 2026-10-05
Modo: auditoria. Verificação na tela por UI Automation em `--demo` (roteiros no scratchpad da sessão, não versionados).

## Corrigido
- Alta — Transmitir: live que falhava ao iniciar ficava anunciada aos amigos e a janela seguia em AO VIVO. HostBroadcast anuncia só depois da captura; na falha a janela volta ao estado parado com mensagem. Teste: HostBroadcastStartTests (falhava antes da correção)
- Média — Remover amigo: imediato e sem volta. Barra "Fulano removido · Desfazer" por 8 s, devolve na mesma posição (conferido na tela)
- Média — Atualizar agora: botão travava em "Baixando..." se o download ou o .sha256 falhasse. Agora mostra a porcentagem e volta como "Tentar de novo". Não conferido na tela: só aparece com release nova
- Média — Fechar durante a live: ConfirmDialog só quando há gente assistindo, foco em Cancelar, não pergunta no fechamento pelo instalador (conferido na tela: sem live fecha direto; cancelar mantém; confirmar fecha)
- Média — IP do amigo: qualquer texto era aceito. Regra estrita FriendsService.IsValidFriendIp (16 casos em FriendIpValidationTests); erro abaixo do campo ao adicionar; na lista, ValidationRule com borda vermelha (conferido na tela)
- Baixa — Mensagens com texto de exceção e MessageBox sem título: trocadas por texto do que fazer; detalhe no DiagnosticLog
- Baixa — Apelido longo cobria o vídeo no quadro da live: corte com "…" e tooltip, também na lateral (conferido na tela)

## Pendente
- Conferir o botão de atualização (progresso e "Tentar de novo") na próxima release — Média

## Decidi não corrigir
- Nenhum item

## Não se aplicam
- Skeleton (status chega em < 300 ms; lives já mostram "Conectando…"), máscaras e teclado móvel (desktop), 404, formato de moeda

## Pendências de domínio
- Não se aplica
