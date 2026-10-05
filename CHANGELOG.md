# Changelog

Todas as mudanças relevantes do Stream Live ficam registradas aqui.
Formato: [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) · Versões: [SemVer](https://semver.org/lang/pt-BR/).

## [Não lançado]

### Adicionado
- Remover um amigo por engano agora tem volta: aparece "Desfazer" por alguns segundos.
- Fechar o app no meio de uma live com amigos assistindo pede confirmação antes de derrubar a transmissão.

### Corrigido
- Se a transmissão não consegue iniciar, a tela volta ao normal e os amigos não veem mais você "ao vivo" numa tela preta.
- O IP do amigo é conferido ao adicionar e ao editar; um IP digitado errado mostra como corrigir em vez de deixar o amigo sempre offline.
- O botão de atualizar mostra a porcentagem do download e, se falhar, permite tentar de novo em vez de travar em "Baixando...".
- Mensagens de erro dizem o que fazer, sem texto técnico.
- Apelidos longos não cobrem mais o vídeo da live.
- Uma live não pode mais ser derrubada por um pacote de rede malformado enviado por alguém da VPN (atualização do componente de WebRTC e do FFmpeg).

### Alterado
- O instalador passa a mostrar autor e links do projeto em "Aplicativos instalados" do Windows.

## [1.0.38] - 2026-10-04

Estado do projeto ao adotar o kit. O histórico anterior está nas [releases do GitHub](https://github.com/Lintzz/stream-live/releases).

[Não lançado]: https://github.com/Lintzz/stream-live/compare/v1.0.38...HEAD
[1.0.38]: https://github.com/Lintzz/stream-live/releases/tag/v1.0.38
