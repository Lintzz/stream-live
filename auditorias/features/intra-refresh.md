# Intra-refresh e teto de 5 Mbps (fim das travadinhas por perda de pacote)
Data: 2026-10-06

## Por que
Live real com a build de diagnóstico (18:00–18:37, 82 marcas de F8): vídeo pela Radmin, ping
15 ms, perda total 0,93%. 177 de 178 congelamentos foram perda → imagem retida esperando IDR,
~1,1 s cada (= teto de 1 s da retenção: o IDR pedido não chegava a tempo). A perda crescia com
a taxa do segundo: <1 Mbps 0%, 3–5 Mbps ~16%, 5–6 28%, 6–7 58%, 7+ 68–100%.

## O que mudou
- `MaxVideoKbps` 8000 → **5000**; `vbv-bufsize` de 1/2 para **1/5** do teto
- **Intra-refresh** (`keyint=30:intra-refresh=1`): sem IDR no meio do fluxo; a imagem se renova
  numa faixa que varre o quadro a cada 30 quadros
- Sai o keyframe periódico de 5 s e a rajada de IDR a cada 400 ms nos 4 s após alguém entrar;
  fica um IDR na conexão e os pedidos de quem ainda não decodificou nada
- Piso entre IDR sob demanda: 300 ms → **1 s**
- Viewer detecta o SEI recovery point (`ContainsRecoveryPointSei`) e, com host intra-refresh,
  não retém a imagem nem pede IDR na perda (`ShouldHoldForKeyFrameOnLoss`)
- Diagnóstico detalhado: `refresh=SIM/nao` na linha de cada segundo

## Compatibilidade (sem mudar o protocolo)
| host → viewer | comportamento |
|---|---|
| nova → nova | perda some sozinha na varredura, sem congelar |
| nova → 2.3.0 | viewer antigo retém e pede IDR, como hoje; host atende no piso de 1 s |
| 2.3.0 → nova | sem SEI: viewer novo faz o de antes (retém e pede) |

## Medições (1080p em movimento, 240 quadros)
| | CPU | taxa | quadro mediano | maior quadro |
|---|---|---|---|---|
| antes (IDR a cada perda) | 10,8 ms/quadro | 10,9 Mbps | 16 KB | 219 KB |
| depois | 11,0 ms/quadro | 5,0 Mbps | 10 KB | 21 KB |

Recuperação depois de um quadro perdido (FFmpeg de verdade): a imagem volta **idêntica** ao
fluxo limpo no fim da varredura que começa depois da perda — período 60: perda no 80, cura no
161; período 30: perda no 40, cura no 89. Pior caso, 2 períodos (1 s a 60 fps). Por isso 30.

## Auditorias reaplicadas
| # | Item | Auditoria | Status | Evidência |
|---|---|---|---|---|
| 1 | CPU do encoder | 11-performance | ✅ | 10,8 → 11,0 ms/quadro em 1080p (benchmark temporário, não versionado) |
| 2 | Bitrate real e rajadas | 11-performance | ✅ | 5,0 Mbps; maior quadro 219 → 21 KB; `IntraRefreshTests` trava "sem IDR no meio, nenhum quadro > 3× a mediana" |
| 3 | Opções efetivas do x264 | 11-performance | ✅ | `EncoderQualityTests`: keyint=30, intra_refresh=1, vbv_maxrate=5000, vbv_bufsize=1000 |
| 4 | Recuperação sem congelar | 11-performance | ✅ | `IntraRefreshTests.DecoderHealsByItselfAfterALostFrameWithoutStopping` |
| 5 | Live real com a host nova | 11-performance | ✅ | 2026-10-06 21:36–21:52, amiga transmitindo na versão nova, pela Radmin: refresh=SIM detectado; perda 0,93% → 0,00% (6 pacotes em 473 mil); congelamentos 178 de ~1,1 s → 2 de ~0,3 s; taxa mediana 3,8 Mbps (p95 4,8). Dono: "não travou nada"; qualidade cai "bem pouco" às vezes |

Verificação: 300 testes (eram 295). `PeriodicKeyFrameTests` removido junto com a regra.

## Pendente
- Nitidez a 5 Mbps: o dono nota queda pequena às vezes e aceitou (2026-10-06)
- Teto adaptativo (baixar sozinho quando quem assiste perde pacote): possível depois, por decisão do dono

## Versão
Menor (2.4.0): ganho perceptível na fluidez — exemplo do CONVENCOES. Commits `perf(video)`.

## Ajuste: 60 ou 30 fps conforme o encoder dá conta (2026-10-06)
Live do dono jogando Valorant (i5-10400F, captura DXGI ~53 fps): encoder a ~24 ms por quadro,
30 codificados e 23 pulados por segundo, **2,5 Mbps** (nenhum segundo no teto de 5) — quadradinhos
em todo movimento. Causa: declarado a 60, o controle de taxa dividia o teto em 60 fatias e só 30
saíam. Medido no encoder real (1080p, metade dos quadros): declarar 60 → 10 KB/quadro, 2,5 Mbps;
declarar 30 → 20 KB/quadro, 5,0 Mbps. (PSNR descartado: o conteúdo sintético não passa de 28 dB nem
parado, então não mede jogo.)
- `EncodeRateGovernor` (puro, 13 casos): desce para 30 com ≥25% pulados por 3 s; tenta 60 de novo
  com ≤13 ms/quadro por 20 s; tentativa que falha em até 30 s dobra a espera (até 16×)
- Captura a 30: um quadro a cada ~33 ms (`ShouldTakeFrame`), não mais ao acaso
- `EncoderBitrateTests.DeclaringThirtyGivesEachFrameTwiceTheBudget` trava o efeito no x264 real
- Log normal registra cada troca ("encoder não dá conta… codificando a 30 fps"); o detalhado
  mostra `alvo=60/30fps` na linha da host
- Pendente: live real jogando com a build nova, para ver a imagem em movimento
