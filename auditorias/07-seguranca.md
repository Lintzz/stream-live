# Segurança — registro
Data: 2026-10-05
Perfil: app desktop P2P (WPF), servidor WebSocket em 0.0.0.0:8080, vídeo WebRTC, áudio/sinalização no WebSocket. Sem banco, login de conta, upload ou pagamento.
Atacante considerado: membros da rede Radmin (inclui desconhecidos em redes públicas da Radmin) e qualquer rede que alcance a porta 8080.

## Corrigido
- Média — Viewer usava o dado bruto quando a mensagem/áudio não decifrava (anulava o AES-GCM). Agora descarta, exceto controle em claro (AUTH_*, STATUS_RESPONSE, PONG). Autenticação passou a valer só para a senha em uso; STREAM_STARTED vai em claro para quem não autenticou; viewer esquece a chave no STREAM_STOPPED. Consertou também as transições sem senha → com senha e senha X → senha Y. Testes: ViewerDecryptionTests, ChangingThePasswordRequiresAuthenticatingAgain, NextLiveStartReachesTheViewerInClear (falhavam antes)
- Média — Sem limite de tentativas de senha. AuthThrottle: 5 erros por IP → bloqueio de 60 s (AUTH_LOCKED); viewer mostra o motivo e para de reconectar. Testes: AuthThrottleTests, FiveWrongPasswordsLockTheIpEvenAcrossReconnections (falhavam antes)
- Média — Com a lista de amigos desligada, qualquer IP entrava. Agora só 26.0.0.0/8 (Radmin) + loopback; amigo salvo de outra faixa continua entrando. Teste: WithFriendsListOffOnlyTheRadminRangeGetsIn (falhava antes)
- Média — Salt fixo do PBKDF2 e mesma chave no HMAC e no AES. Protocolo v2: salt por senha no desafio, HKDF divide em Auth/Enc, prova "v2:". Versão mista → AUTH_OUTDATED (host avisa, não conta no limite) / viewer avisa "versão antiga". **Quebra compatibilidade em sala com senha: próxima release é 2.0.0.** Testes: CryptoHelperTests, OldAppProofIsToldToUpdateAndDoesNotCountAsWrongPassword (escritos junto da mudança de protocolo)
- Baixa — Mensagem de texto sem limite de tamanho: acima de 64 KB derruba a conexão. A memória ainda é alocada pelo Fleck antes do teto. Teste: OversizedMessageClosesTheConnection (falhava antes)
- Baixa — Comentário do DiagnosticLog dizia que o settings.json guarda a senha da sala (não guarda)

## Pendente
- Alta (bloqueia release) — Teste de live real entre duas máquinas na v2: live com senha, sem senha, troca de senha entre lives, senha errada 5x, amigo na 1.0.38 tentando entrar (deve ver/gerar o aviso de versão antiga)

## Decidi não corrigir
- Baixa — Instalador sem assinatura de código (Authenticode): custo anual, decisão do dono em 2026-10-05. O .sha256 protege contra download corrompido, não contra conta do GitHub tomada. Mitigação sem custo: 2FA na conta do GitHub

## Verificar manualmente
- 2FA ligado na conta GitHub Lintzz (Settings → Password and authentication)
- Regra do Firewall do Windows criada na primeira execução: perfil Público deveria ficar bloqueado

## Pendências de domínio
- Não se aplica
