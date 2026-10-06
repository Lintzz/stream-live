# Changelog

Todas as mudanças relevantes do Stream Live ficam registradas aqui.
Formato: [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) · Versões: [SemVer](https://semver.org/lang/pt-BR/).

## [Não lançado]

## [2.3.0] - 2026-10-06

### Adicionado
- Ao clicar em Transmitir, você escolhe a tela vendo o que está nela, como no Discord: cada tela aparece em miniatura, com a resolução e qual é a principal. Nada de adivinhar se é a "Tela 1" ou a "Tela 2".
- Durante a live, o botão "Trocar tela" muda a tela transmitida sem derrubar ninguém.
- Selo "AO VIVO" na barra de título enquanto você transmite, com quantas pessoas estão assistindo. Ele continua à vista quando você abre a live de um amigo; clique nele para ver os controles da sua transmissão.

### Alterado
- Live privada ficou mais simples: não existe mais a caixa "Live privada". Se você não marcar ninguém em "Quem pode ver", a live é para todos os seus amigos; se marcar alguém, só essas pessoas veem. Toda live começa sem ninguém marcado.
- O botão Transmitir e tudo da sua live (ao vivo, privada, quem assiste, parar) foram para um card próprio embaixo da lista de amigos. A faixa que ficava no topo da janela saiu e o vídeo ganhou esse espaço.
- "Gerenciar amigos" virou uma engrenagem no canto da lista de amigos.
- O app abre maior (1200×760), para a janela de transmitir caber dentro dele.
- Caixas de marcar e escolha de tela no tema escuro do app, sem o visual branco padrão do Windows.

## [2.2.0] - 2026-10-06

### Melhorado
- Imagem bem mais nítida quando algo se move rápido na tela: menos borrão e menos blocos, gastando cerca de metade da internet que antes nesses momentos.
- Menos "quadradinhos" em quem assiste: o app manda a imagem completa com muito menos frequência, e essas rajadas eram o que mais se perdia na VPN.
- Quem transmite usa um pouco mais de processador (cerca de meio núcleo a mais). O jogo continua com prioridade sobre a live.

## [2.1.0] - 2026-10-05

### Adicionado
- O Stream Live abre a Radmin VPN sozinho, direto na bandeja, quando ela está fechada — sem janela do Radmin na tela.
- Se a Radmin VPN abrir off-line, o app a deixa on-line sozinho. Às vezes o menu do Radmin pisca por uma fração de segundo perto do relógio; se não der certo, um aviso no rodapé explica como fazer à mão.
- Ao fechar o app com a Radmin VPN aberta, a confirmação traz a caixa "Fechar o Radmin VPN também". Vem marcada; quem usa o Radmin para jogar pode desmarcar, e a escolha fica lembrada.

### Alterado
- Ícone novo.

### Corrigido
- Algumas configurações voltavam ao padrão toda vez que o app abria.

## [2.0.0] - 2026-10-05

> ⚠️ **Atualize junto com seus amigos.** A sala com senha mudou a forma de conferir a senha: quem estiver numa versão anterior não consegue entrar numa sala com senha de quem já atualizou (e vice-versa). O app avisa quando o outro lado está desatualizado. Salas sem senha continuam funcionando entre versões.

### Adicionado
- Remover um amigo por engano agora tem volta: aparece "Desfazer" por alguns segundos.
- Fechar o app no meio de uma live com amigos assistindo pede confirmação antes de derrubar a transmissão.

### Acessibilidade
- Dá para usar o app inteiro só com o teclado: assistir a um amigo, abrir mais de uma live, mostrar a lista de amigos com uma live aberta e abrir/fechar as configurações (Esc fecha).
- O leitor de tela (Narrador, NVDA) anuncia o nome de todos os botões, o estado de cada amigo ("Ana, ao vivo"), os campos dos formulários e as mensagens que mudam sozinhas.
- Contorno de foco bem visível e textos com mais contraste; o azul dos botões ficou um pouco mais escuro para o texto branco ser legível.

### Desempenho
- Transmitir sem ninguém assistindo não pesa mais no PC: o vídeo só é codificado quando alguém entra.
- O vídeo tem teto de 8 Mbps por amigo, com a mesma qualidade de antes: a imagem não trava mais por excesso de dados quando a internet de quem transmite é mais limitada.
- A prévia da sua transmissão só gasta processamento quando está aberta.

### Alterado
- Senha da sala mais resistente: cada sala tem sua própria proteção, e descobrir a senha de uma não adianta nada para as outras.
- O instalador passa a mostrar autor e links do projeto em "Aplicativos instalados" do Windows.

### Segurança
- Depois de 5 senhas erradas, o computador que errou fica 1 minuto sem poder tentar de novo.
- Numa sala com senha, quem assiste descarta qualquer mensagem ou som que não venha do host com a senha certa.
- Com "somente amigos" desligado, só entra quem está na Radmin VPN — a rede local e a internet continuam de fora.
- Mensagens grandes demais são recusadas antes de serem processadas.
- Uma live não pode mais ser derrubada por um pacote de rede malformado enviado por alguém da VPN (atualização do componente de WebRTC e do FFmpeg).

### Corrigido
- Se a transmissão não consegue iniciar, a tela volta ao normal e os amigos não veem mais você "ao vivo" numa tela preta.
- Trocar a senha entre uma live e outra (ou começar uma live com senha depois de uma sem) não deixa mais quem estava assistindo preso em "Transmissão encerrada".
- O IP do amigo é conferido ao adicionar e ao editar; um IP digitado errado mostra como corrigir em vez de deixar o amigo sempre offline.
- O botão de atualizar mostra a porcentagem do download e, se falhar, permite tentar de novo em vez de travar em "Baixando...".
- Mensagens de erro dizem o que fazer, sem texto técnico.
- Apelidos longos não cobrem mais o vídeo da live.
- Avisos como "conexão recusada" e "amigo com versão antiga" agora aparecem num aviso no rodapé — antes ficavam escondidos na prévia da transmissão.

## [1.0.38] - 2026-10-04

Estado do projeto ao adotar o kit. O histórico anterior está nas [releases do GitHub](https://github.com/Lintzz/stream-live/releases).

[Não lançado]: https://github.com/Lintzz/stream-live/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/Lintzz/stream-live/compare/v1.0.38...v2.0.0
[1.0.38]: https://github.com/Lintzz/stream-live/releases/tag/v1.0.38
