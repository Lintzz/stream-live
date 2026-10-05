# Radmin VPN pela tela do app + ícone novo
Data: 2026-10-05 · Commits: `4d4877d` (ícone), `a1fcaeb` (Radmin, **revertido** em `f22d446`), `10b1617` (Radmin abre na bandeja) · Próxima versão: 2.1.0 (menor)

## Mudança de rumo (2026-10-05, depois do teste do dono)
- Teste do dono: com a janela do Radmin fechada, os amigos **não** aparecem online — a janela precisa rodar
- Decisão: entrar na rede pelo app saiu (aba, UI Automation, DPAPI, opções de fechamento e o pacote ServiceController); entrar na rede é manual, pelo Radmin
- No lugar: na abertura, Radmin fechado → o app o abre com `/minimized`, direto na bandeja, sem janela na tela; o aviso só aparece se não está instalado ou não abriu
- Conferido no PC do dono: Radmin aberto pelo app em ~1 s com `/minimized`, nenhuma janela visível além do contêiner de 22×22 que o Radmin sempre mantém na bandeja, adaptador Up, sem aviso; o Radmin continua aberto depois de fechar o Stream Live
- Caso novo: `BuildStartInfo_OpensRadminStraightToTheTray` (escrito antes, falhou, depois passou). 225 testes verdes
- Depois (commit `8080856`): o Radmin quase sempre sobe off-line (volta no estado em que foi fechado). Com `PowerOn=0`, o app liga sozinho: clique postado no botão de energia da janela escondida e, como reserva, "Ficar on-line" do menu da bandeja (pisca ~180 ms perto do relógio; o Esc depois é obrigatório, o Invoke não fecha o menu). Medido no PC do dono: on-line 3,2 s depois de abrir o app, nenhum menu preso. Descartados por teste: TogglePattern, janela transparente, região vazia, redimensionar a janela escondida, gravar no registro (só leitura sem admin). 239 testes
- As seções abaixo descrevem a versão removida e ficam como referência; os AutomationId medidos estão em `auditorias/contexto.md` (seção Radmin VPN) e o código está no commit `a1fcaeb`

---


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
- Teste com um amigo na build da 2.1.0: abrir o Stream Live com o Radmin fechado e ver os amigos online sem o Radmin aparecer na tela — Média
- (sem efeito: a aba Radmin saiu) entrar com nome e senha certos, Desconectar real, opções de fechamento
- O veredito do `12-pre-lancamento` da 2.0.0 não cobre esta mudança

## Decidi não corrigir
- (versão removida) Senha fica em memória como string enquanto o app usa (o PasswordBox do WPF não evita) — Baixa
- Com o Radmin na bandeja, a janela dele aparece por um instante ao conectar — não há como abri-lo já escondido pela UI Automation
