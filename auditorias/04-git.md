# Git e repositório — registro
Data: 2026-10-05

Veredito de segredos: 🟢 nada encontrado (arquivos rastreados, nomes no histórico inteiro, padrões de chave e de senha no diff de todo o histórico). Sem gitleaks/trufflehog na máquina; a varredura foi por padrões. Secret scanning e push protection do GitHub ligados.
Chaves rotacionadas: nenhuma (nada vazou).

## Corrigido
- .gitignore: `*.pem` e `*.key` fora do bloco do kit
- ci.yml: cache do LFS por id dos objetos (o CI baixava ~250 MB por execução; a cota gratuita de banda do LFS é 10 GiB/mês, consultada em 2026-10-05 em docs.github.com/en/billing/concepts/product-billing/git-lfs)
- ci.yml: `permissions: contents: read` explícito
- ci.yml: actions fixadas por SHA na linha v4 (checkout v4.4.0, cache v4.3.0, setup-dotnet v4.3.1)

## Pendente
- Resolvido em 2026-10-05: push feito com a release 2.0.0; CI verde, cache do LFS criado, AppSmokeTests passou no runner

## Decidi não corrigir
- `auditorias/` fica versionada no repositório público: hoje não há nada sensível nela. Revisar no /07-seguranca se aparecer falha ainda não corrigida
- E-mail pessoal nos commits: exposto por decisão (perfil do desenvolvedor)
- Actions continuam na linha v4 (há checkout v7, setup-dotnet v6, cache v6): subir major é mudança à parte

## Verificar manualmente
- Uso de banda do LFS no painel do GitHub (Settings → Billing) depois de algumas execuções do CI

## Pendências de domínio
- Não se aplica (sem domínio)
