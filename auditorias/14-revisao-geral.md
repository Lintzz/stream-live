# Revisão geral — registro
Data: 2026-10-05
Veredito: 🔴 não publicar ainda — código dentro do padrão, sem regressão; falta validar em live real as correções Altas (segurança, dependências, performance) e ver o CI verde

## Regressões
- Nenhuma. 22 correções conferidas no código (todas as de segurança do /07, teclado e leitor de tela do /10, teto de bitrate e encode só com público do /11, AppId, permissões do CI, PathMap)

## Etapas
- Todas as aplicáveis rodaram; nenhuma desatualizada (mudanças posteriores a cada registro vêm das etapas seguintes e não tocam a área auditada)
- Não se aplicam: 03, 05, 08, 09, 13, banco, projeto-limpo

## Pendências consolidadas
### Impede publicar
1. Teste de live real entre duas máquinas (roteiro em 12-pre-lancamento.md)
2. Push + CI verde (cache do LFS e AppSmokeTests no runner)
### Deveria ser resolvido
3. Teste com o Narrador · 4. Botão "Atualizar agora" na primeira atualização pós-2.0 · 5. 2FA no GitHub e firewall em rede pública
### Aceito conscientemente
- Instalador sem Authenticode · AMF e decode por GPU · DLLs do FFmpeg mantidas · áudio PCM · temas de alto contraste · Fleck sem atualizações

## Inconsistências corrigidas nesta revisão
- PLANO.md (próximos passos), contexto.md (o que existe hoje) e dependencias.md (tamanho do FFmpeg decidido no /11)

## Recomendado refazer
- /14-revisao-geral depois da release 2.0.0, para fechar em 🟢
