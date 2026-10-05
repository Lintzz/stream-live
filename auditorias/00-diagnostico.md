# Diagnóstico — registro
Data: 2026-10-05

## Raio-x
- App desktop WPF .NET 8 (net8.0-windows10.0.19041.0); SIPSorcery, FFmpeg, Fleck, NAudio, Vortice
- Roda: build Release ok; abre com `--demo` (janela "Stream Live (demonstração)")
- Testes: 153/153 xUnit em ~1 s, nenhum pulado; CI no GitHub Actions verde
- Git: 61 commits, remoto Lintzz/stream-live público, main; commits fora do Conventional Commits até aqui
- Versão: 1.0.38 só no csproj (gera version.iss); release v1.0.38 publicada
- Serviços externos: só a API de releases do GitHub (auto-update)
- Dependências: 9 pacotes; SIPSorcery, NAudio e Vortice atrasados

## Riscos imediatos
- SIPSorcery 8.0.23 (produção) com 2 advisories Altas: GHSA-28gm-jrmw-xx93, GHSA-jwjp-4649-v8jp — aviso escondido por NoWarn NU1903
- .gitignore do kit ignorava `build/` e `.claude/lz/` — corrigido em 2026-10-05 com exceções do projeto depois do bloco do kit
- Nenhum segredo no código nem `.env` no histórico

## Respostas
1. Transmissão de tela e áudio entre amigos na Radmin VPN, código aberto
2. Perfil B — Pessoal, publicado
3. Verificar e corrigir, depois continuar desenvolvendo
4. Consertar o .gitignore: sim (feito)
5. Nada quebrado; no futuro: trocar o ícone e garantir 1080p, som bom, fps estável sem pesar no PC

## Roteiro recomendado
dependencias → /02-configurar → /04-git → /06-interface → /07-seguranca → /10-acessibilidade → /11-performance → /12-pre-lancamento → /14-revisao-geral
