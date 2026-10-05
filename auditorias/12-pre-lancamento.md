# Pré-lançamento — registro
Data: 2026-10-05
O que foi testado: build de produção da próxima release (publish self-contained win-x64 + instalador Inno Setup gerado em pasta temporária). App desktop distribuído por release no GitHub — sem URL, domínio ou site.

## Veredito
🟡 Lançar a 2.0.0 depois de: (1) teste de live real entre duas máquinas; (2) push com CI verde.

## Métricas do build
- Publish: 509 arquivos, 453 MB · instalador: 121 MB (v1.0.38: 105 MB — FFmpeg 8)
- Suíte: 224/224, nenhum caso pulado · dependências: 0 vulneráveis · build sem avisos
- Executável publicado abre em --demo (166 MB de RAM); metadados certos (versão ainda 1.0.38 — o /build sobe)
- Próxima versão: 2.0.0 (29 commits feat/fix/perf desde v1.0.38, um com `!`)

## Corrigido
- A — Caminho da máquina de build no executável (D:\Projetos\...\StreamLiveApp.pdb): PathMap → /_/src/...
- B — Prints e GIF do README regravados pelo --demo (1200×720 e 900×540)
- C — README: quadro de proteção atualizado para a 2.0 e aviso para atualizar junto com os amigos
- Achado no caminho (Alto, acessibilidade) — cards de amigos sem nome para o leitor de tela com live aberta: LiveItemsControl (detalhe em 10-acessibilidade.md)

## Pendente
- Teste de live real (roteiro abaixo) — pendências Altas de dependencias, 07-seguranca e 11-performance. A 2.0.0 foi publicada antes dele por decisão do dono (2026-10-05); fazer o quanto antes com a versão publicada
- Resolvido: push + CI verde (224 testes, AppSmokeTests no runner, cache do LFS)

## Pendente (não bloqueia)
- Narrador (roteiro em 10-acessibilidade.md) · botão "Atualizar agora" só se exercita a partir da 2.0.x · 2FA no GitHub · firewall em rede pública

## Decidi não corrigir
- Assinatura Authenticode do instalador (custo) — ver 07-seguranca.md

## Roteiro de testes manuais
1. Instalar por cima da 1.0.38: uma entrada só em Aplicativos instalados; amigos preservados
2. Live com amigo, ambos na 2.0: sem senha, com senha, troca de senha entre lives; vídeo, som, reconexão; jogo pesado (fps e qualidade com teto de 8 Mbps)
3. Senha errada 5×: "Muitas tentativas de senha"; após 1 min entra
4. Amigo na 1.0.38 tentando sala com senha: aviso de versão antiga no rodapé
5. Fechar o app com amigo assistindo: confirmação

## Não se aplicam
- Domínio, HTTPS, headers, indexação, sitemap, Open Graph, 404, PageSpeed, responsivo, formulário, WhatsApp, analytics, e-mail, banco, login, pagamento

## Incidente durante a verificação
- Um roteiro de automação de interface mandou Tab/Shift+Tab/Enter para janelas de outros apps do usuário ("Início da sessão", "Proposta de instalador") enquanto ele usava o PC. Desde então toda tecla enviada confere se o Stream Live está em primeiro plano; automação de tela só com o PC liberado
