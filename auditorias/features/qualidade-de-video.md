# Qualidade de vídeo: pixelado no movimento, quadradinhos e fps
Data: 2026-10-06 (plano em 2026-10-05) · Commits: `103f281` (encoder), `0a52748` (keyframe periódico) · Próxima versão: 2.1.1 (correção: só `perf`)

## Pedido
Imagem pixelada quando algo se move rápido, "quadradinhos" às vezes, e fps que parece baixo.

## Diagnóstico (logs do dono, antes da mudança)
- Live de 2026-10-05 21:11–21:14 com 1 amigo: captura com média de 35 fps e vídeo com 31 fps, com trechos de 8–14 fps; bitrate mediano de ~3,6 Mbps. O teto de 8 Mbps não era o limite na média
- Viewer em 2026-10-04: até 1717 pacotes de vídeo perdidos em 10 s
- Causas no código: o `ultrafast` puro sem deblock, sem AQ e com busca de movimento mínima (o movimento rápido batia no teto); keyframe automático do x264 a cada 1 s somado ao forçado a cada 2 s (rajadas de ~190 KB); processo em BelowNormal (fps cai em jogo pesado)

## Decisões do dono
- O jogo continua na frente: prioridade do processo e das threads **não muda**. O fps em jogo pesado continua limitado pela CPU que sobra
- Preset escolhido por medição, com teto de ~1,5× a CPU de antes
- Seletor 1080p/720p fica para depois
- Etapa 4 (buffer UDP) descartada, ver abaixo

## Etapas
### 1. Medição (benchmark temporário, fora do repo)
1080p real (captura da tela em panorâmica), mesmo teto de 8 Mbps. Médias de 2–3 rodadas, CPU de 1 núcleo a 60 fps:

| Configuração | CPU | Rápido (24 px/q): bitrate | Rápido: pior 5% | Lento (6 px/q): bitrate |
|---|---|---|---|---|
| ultrafast (antes) | ~116% | 8,4–9,0 Mbps (teto) | 24,2–25,7 dB | 5,6–5,9 Mbps |
| + deblock/AQ/8x8 | ~126% | 7,9–8,4 Mbps | 27,0–28,1 dB | 3,4–3,7 Mbps |
| + hex/subme 1 ou dia/subme 2 | ~150–160% | 7,6–8,1 Mbps | 27,5–28,0 dB | 3,4 Mbps |
| **+ hex/subme 2 (escolhido)** | **~179%** | **4,5 Mbps** | **29,8–30,4 dB** | 3,5–3,6 Mbps |
| veryfast | ~173% | 4,5 Mbps | 30,5 dB | 3,5 Mbps |

veryfast descartado pelo mesmo custo e imagem: liga scenecut (keyframe extra em troca de cena).

### 2. Encoder (`103f281`)
`CreateH264Encoder`: `x264-params` com `deblock=0,0:aq-mode=1:8x8dct=1:partitions=i8x8,i4x4:me=hex:subme=2:keyint=600:min-keyint=600`. Teste `EncoderQualityTests` lê as opções efetivas no SEI do primeiro quadro (falhou antes, passa depois). CLAUDE.md atualizado

### 3. Keyframe periódico (`0a52748`)
2 s → 5 s. Decisão extraída em `ShouldForcePeriodicKeyFrame`; `PeriodicKeyFrameTests` (falhou antes, passa depois). Com o keyint=600, uma live sem perda passa de 1 keyframe/s para 1 a cada 5 s

### 4. Buffer UDP: descartada
Hipótese refutada por medição: o SIPSorcery 10.0.17 já abre o socket RTP com 1.000.000 bytes de recepção e envio (~6 keyframes). Subir não traria ganho. A perda vista nos logs aponta para o caminho de rede (Radmin em relé ou upload saturado), não para o app

### 5. Verificação
- 254 testes verdes, build sem avisos, app abre em `--demo`
- 🔍 Pendente (dono): live real com amigo e jogo pesado. Comparar no `diagnostico.log` a linha `[Live] captura=… video=…fps …kbps` (host) e `pacotes de video perdidos` (viewer) com os números do diagnóstico acima; olhar CPU do host no Gerenciador de Tarefas; conferir no Radmin se a conexão com o amigo é direta ou relé

## Auditorias reaplicadas (recorte)
| # | Item | Auditoria | Status | Evidência |
|---|---|---|---|---|
| 1 | CPU por quadro do encoder novo | 11-performance | ✅ dentro do teto aceito | ~179% vs ~116% de 1 núcleo, 11–12 ms/q (< 16,6 ms) |
| 2 | Teto de 8 Mbps mantido | 11-performance | ✅ | `EncoderBitrateTests` verde; `vbv_maxrate=8000` no SEI |
| 3 | Tamanho do keyframe | 11-performance | ✅ | ~190 → ~155 KB, de 1/s para 1/5 s |
| 4 | Recuperação de perda continua | testes | ✅ | `VideoLossRecoveryTests` verde (ForceIdr ainda gera IDR) |
| 5 | Comportamento em jogo pesado | 11-performance | 🔍 | só a live real mostra |
| 6 | Segredo, variável, .gitignore | sempre | ✅ N/A | nenhuma variável nem arquivo novo fora do código |

## Pendências e o que ficou para depois
- Teste com amigo (item 5)
- Se a cena lenta parecer mais mole: CRF 23 → 21 (gasta a banda que sobrou; o lento caiu de 5,8 para 3,5 Mbps)
- Se a perda continuar com conexão direta: espaçar o envio dos pacotes no host (pacing) ou intra-refresh — nova-feature
- Seletor 1080p/720p
