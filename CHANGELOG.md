# Changelog

Todas as mudanças relevantes do Stream Live ficam registradas aqui.
Formato: [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) · Versões: [SemVer](https://semver.org/lang/pt-BR/).

## [Não lançado]

> ⚠️ **Atualize junto com seus amigos.** A sala com senha mudou a forma de conferir a senha: quem estiver numa versão anterior não consegue entrar numa sala com senha de quem já atualizou (e vice-versa). O app avisa quando o outro lado está desatualizado. Salas sem senha continuam funcionando entre versões.

### Adicionado
- Remover um amigo por engano agora tem volta: aparece "Desfazer" por alguns segundos.
- Fechar o app no meio de uma live com amigos assistindo pede confirmação antes de derrubar a transmissão.

### Acessibilidade
- Dá para usar o app inteiro só com o teclado: assistir a um amigo, abrir mais de uma live, mostrar a lista de amigos com uma live aberta e abrir/fechar as configurações (Esc fecha).
- O leitor de tela (Narrador, NVDA) anuncia o nome de todos os botões, o estado de cada amigo ("Ana, ao vivo"), os campos dos formulários e as mensagens que mudam sozinhas.
- Contorno de foco bem visível e textos com mais contraste; o azul dos botões ficou um pouco mais escuro para o texto branco ser legível.

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

[Não lançado]: https://github.com/Lintzz/stream-live/compare/v1.0.38...HEAD
[1.0.38]: https://github.com/Lintzz/stream-live/releases/tag/v1.0.38
