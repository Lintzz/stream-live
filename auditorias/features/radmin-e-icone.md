# Radmin VPN pela tela do app + ícone novo
Data: 2026-10-05 · Commits: `4d4877d` (ícone), `a1fcaeb` (Radmin) · Próxima versão: 2.1.0 (menor)

## O que foi adicionado
- Ícone novo (duas janelas sobrepostas). `build/make-icon.ps1` gera o .ico com ImageMagick; 16–32 px saem de um recorte central para não virar borrão
- Aba "Radmin VPN" nas configurações: nome da rede, senha (com mostrar/ocultar), Conectar, Desconectar, status, "Lembrar rede", e duas opções de fechamento
- `RadminUiAutomation` (única classe que toca a janela do Radmin), `RadminController` (detecção, serviço, adaptador, limpeza), `RadminCredentialStore` (DPAPI), `RadminConnectionViewModel`
- Aviso de abertura ganhou o atalho "Entrar numa rede do Radmin por aqui"; `VpnStatusService` acha o Radmin também pelo registro

## Pesquisa e testes no Radmin real (2.1.1 / 2.1.4951.1)
- Linha de comando: só `/minimized`. Sem argumento, protocolo de URL ou API para entrar em rede → UI Automation
- `rvpn2.db`/registro: não testado de propósito — a entrada é validada nos servidores da Famatech; dado local não faz ninguém entrar, e exigiria admin
- Entrar numa rede de nome aleatório (com a janela aberta e com o Radmin na bandeja): erro "Nome de rede ou senha inválidos" em ~2,3 s, diálogo fechado, janela devolvida como estava
- Rede inexistente e senha errada dão a mesma mensagem no Radmin
- Fechar a janela do Radmin (Sistema → Sair) mantém o serviço rodando e o adaptador Up com o IP 26.x; não deu para provar alcance a um amigo (ninguém online no teste)

## Decisões
- "Desconectar" = desligar pelo botão de energia do Radmin (continua membro das redes), com confirmação se há live aberta
- Serviço `RvControlSvc` parado → botão que pede UAC só no clique; o app não roda como admin
- Fechamento só desfaz o que o app fez na sessão; opções desligadas de fábrica
- Senha em `radmin_rede.dat` (DPAPI, CurrentUser), fora do settings.json e do relatório de diagnóstico; nem senha nem nome da rede no log
- Aba "Rede de jogos" e criar rede pelo app ficaram de fora

## Auditorias reaplicadas
| Auditoria | Resultado |
|---|---|
| 06-interface | ✅ estados, erro inline com foco, botão de largura fixa, mostrar/ocultar senha, texto longo — conferido em `--demo` |
| 07-seguranca | ✅ DPAPI testado (nada em claro no arquivo), log sem senha, limpeza só do que o app iniciou |
| 10-acessibilidade | ✅ roteiro de Tab pela UI Automation; corrigida parada invisível de Hyperlink em TextBlock recolhido (aba e aviso de abertura) |
| 11-performance | ✅ UI Automation fora da thread de UI, estado em ~200 ms, teto de 3 s no fechamento |
| dependencias | ✅ System.ServiceProcess.ServiceController 10.0.12, 0 vulnerabilidades |
| Verificação | 258 testes (34 novos), smoke do .exe verde |

## Pendente
- Teste manual antes da 2.1.0 — Alta: entrar com nome e senha certos, "ligar e entrar" com o Radmin desligado, Desconectar real, fechar o app com as duas opções ligadas. Não testados ao vivo para não derrubar a VPN do dono
- O veredito do `12-pre-lancamento` da 2.0.0 não cobre esta mudança

## Decidi não corrigir
- Senha fica em memória como string enquanto o app usa (o PasswordBox do WPF não evita) — Baixa
- Com o Radmin na bandeja, a janela dele aparece por um instante ao conectar — não há como abri-lo já escondido pela UI Automation
