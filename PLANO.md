# Stream Live — estado atual
Atualizado em: 2026-10-05

## O que faz
Transmite tela e áudio entre amigos na Radmin VPN; a mesma janela assiste a várias lives em grade.
Distribuído pelas releases do GitHub com auto-update. Versão atual: 1.0.38.

## Incompleto ou com risco
- Release 2.0.0 pronta no código, falta o teste de live real entre duas máquinas e o push com CI verde (ver auditorias/12-pre-lancamento.md)
- 1.x e 2.0 não se entendem em sala com senha: os amigos precisam atualizar juntos
- Sem teste automático de captura real nem de live entre duas máquinas (o resto tem 224 testes, incluindo smoke do executável)
- Histórico de commits fora do Conventional Commits até a v1.0.38 (vale só daqui pra frente)

## A seguir
1. Teste de live real com um amigo → `/build` da 2.0.0 → `/14-revisao-geral` de novo
2. Trocar o ícone (via `/nova-feature`; a arte original está em Assets/app_icon_source_1024.png)
3. Avaliar encoder de hardware (AMF/NVENC/QSV) com teste nas placas dos amigos — hoje descartado (ver auditorias/11-performance.md)
