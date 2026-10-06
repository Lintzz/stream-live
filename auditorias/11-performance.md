# Performance — registro
Data: 2026-10-05
Referência: app desktop, não site — Core Web Vitals não se aplicam. Medido o pipeline real (captura → encoder → decode) com benchmark temporário (não versionado) na máquina do dono: i5-10400F (6c/12t), RX 5500 XT, 2 telas 1920×1080 a 59 Hz, conteúdo animado a 60 fps em tela cheia.

## Métricas medidas (antes)
- Captura DXGI 1080p: ~59 fps em movimento, ~22% de 1 núcleo (com tela parada o reemit fica em ~31 fps)
- Captura + x264 ultrafast: ~59 fps, 8,4 ms/quadro (p95 9,6), **8,4 Mbps sem teto**, **131% de 1 núcleo** (10,9% da máquina) — inclusive sem ninguém assistindo
- Decode do viewer (software): 3,9 ms/quadro, ~23% de 1 núcleo por live a 60 fps
- Prévia escondida do host: 0,5–0,9 ms/quadro de cópia na thread de UI

## Corrigido
- Alto — Encode sem viewers: pula o encode sem peer conectado (rajada de keyframes ao conectar já existia). Estatística e aviso de saúde passam a olhar a captura. Teste: HostIdleEncodeTests (falhava antes). Ganho estimado: ~1,2 núcleo livre em live sem público
- Alto — Taxa declarada 30 vs 60 reais e sem teto: PrepareEncoder declara 60 fps antes de cada quadro (o ForceIdr recria o encoder); CRF 23 + VBV pelo x264-params (vbv-maxrate 8000, bufsize 4000 — opções "maxrate/bufsize" do contexto genérico eram ignoradas no priv_data). Teste: EncoderBitrateTests — fluxo entre keyframes de textura em movimento ~116 Mbps → dentro de 8 Mbps
- Médio — Prévia escondida: quadro só vai para a UI com a prévia visível. Conferido na tela (desliga/religa e continua atualizando)
- Achado no caminho: aviso CS8602 deixado no /10 (tratador de foco antes da criação do timer) — corrigido; build sem avisos

## Decidi não corrigir
- Encoder de hardware AMF: usa ~metade da CPU (74–80% de 1 núcleo), mas ignorou todo teto de bitrate (12–32 Mbps) e só existe em placa AMD. Reavaliar como nova-feature, com teste em NVIDIA/Intel dos amigos
- Decode por D3D11VA: mais lento (11,6 ms vs 3,9 ms/quadro) por causa da volta GPU→CPU
- Tirar DLLs do FFmpeg do instalador: FFmpegInit chama avdevice_register_all, e a avdevice depende da avfilter (124 MB)
- Áudio PCM (1,4 Mbps por amigo): decisão registrada no CLAUDE.md (Opus revertido)

## Verificar manualmente
- 🔍 Live real num jogo pesado (CPU perto de 100%): fps sustentado, imagem com o teto de 8 Mbps, uso de CPU do host com e sem amigo assistindo
- 🔍 Upload do host: ~8 Mbps de vídeo + 1,4 Mbps de áudio por amigo no pico

## Atualização 2026-10-06 (nova-feature qualidade-de-video)
- Encoder: ultrafast + deblock/AQ/8x8 + me=hex subme=2 + keyint=600. CPU ~116% → ~179% de 1 núcleo (teto de 1,5× aceito pelo dono); movimento rápido sai do teto (8,4 → 4,5 Mbps) e o pior trecho melhora ~5 dB. Detalhes em auditorias/features/qualidade-de-video.md
- Keyframe periódico 2 s → 5 s; o x264 não manda mais keyframe próprio a cada 1 s
- 🔍 Pendente, 2026-10-06: live real em jogo pesado com a 2.1.1 — fps, CPU do host e pacotes perdidos no viewer (somar à verificação manual acima)

## Pendências de domínio
- Não se aplica
