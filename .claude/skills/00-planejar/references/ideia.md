# Guia: planejar a partir da ideia, sem design

Lido pelo `00-planejar` quando não há design em HTML — o usuário só descreveu o que quer. Substitui as Fases 0 e 1 da skill e ajusta a Fase 2. Da Fase 3 em diante (stack, estrutura, roteiro), o planejamento segue igual.

**O que este caminho combate: o escopo que explode.** Ideia vira projeto grande em três mensagens, cada "e também seria legal" custa dias, e o resultado é algo pela metade que nunca fica pronto. O trabalho principal aqui é **cortar**, não somar. Se ao fim o projeto parecer menor do que o usuário imaginou, funcionou.

## Passo 1: Repita a ideia de volta

Antes de qualquer pergunta, escreva **em um parágrafo** o que você entendeu: o que o projeto faz, para quem, e qual problema resolve. Peça confirmação e **pare**.

Parece pouco e é o que evita mais retrabalho: quase todo mal-entendido de projeto nasce ou morre aqui. Se o usuário corrigir, repita até ele dizer que está certo.

Se a descrição for curta demais para entender o que o projeto faz, peça para ele contar como imagina alguém usando, do abrir ao fechar.

## Passo 2: Perguntas

Estas entram na Fase 2 da skill, **junto** com as de stack — uma rodada só, no máximo 8 no total. As de ideia vêm primeiro, porque decidem as outras:

1. **Quem usa:** só você, ou outras pessoas também? (é o perfil A, B, C ou D do FLUXO)
2. **Onde roda:** site no navegador, app de celular, programa de computador? Se o usuário não souber, recomende pelo uso que ele descreveu — ferramenta que ele abre todo dia no PC não precisa ser site.
3. **Precisa guardar informação?** E se precisa: só no aparelho, ou sincronizando entre aparelhos e pessoas? **Esta é a pergunta mais cara do projeto.** Só no aparelho é um arquivo local; sincronizar puxa banco na nuvem, conta de usuário e login — três coisas, não uma.
4. **Tem login?** Só se a resposta anterior exigir. Login que ninguém precisa é semana jogada fora.
5. **Prazo:** tem data, ou é sem pressa? Muda o quanto cortar da v1.

Não pergunte o que a descrição já respondeu.

## Passo 3: Cortar o escopo

**Nomeie em uma frase a única coisa que este projeto tem que fazer bem.** Tudo o mais é acessório, e é dela que a v1 sai.

Depois, três listas, todas obrigatórias:

- **v1** — o mínimo que já é **útil de verdade**. Não é esqueleto nem demonstração: é a versão que o usuário abriria e usaria no dia seguinte. Se não dá para usar, cortou demais.
- **Depois** — o que é claramente bom e não é v1. Fica escrito, para não ser esquecido nem construído agora.
- **Não vai ter** — o que foi cortado de propósito. Precisa estar escrito, senão volta por acidente daqui a duas semanas.

Régua: **v1 com mais de 5 telas ou 7 funcionalidades está grande.** Diga isso e proponha o corte, com o motivo. Se o usuário insistir, registre e siga — mas diga uma vez o que costuma acontecer, e sugira que a v1 seja a primeira fatia útil.

## Passo 4: Derivar as telas

As telas saem do **fluxo principal**, nunca de uma lista de funcionalidades. Percorra a jornada: a pessoa abre o programa → o que ela vê → o que ela faz → o que acontece → e depois.

Para cada tela: **o que mostra**, **o que a pessoa faz ali** e **para onde vai depois**.

E as que todo mundo esquece ao imaginar, mas aparecem no primeiro minuto de uso:

- **Primeiro uso, sem nada cadastrado** — a tela vazia é a primeira que o usuário vê na vida, e é a que costuma não existir
- **Erro** — deu errado, e o que a pessoa faz a respeito
- **Carregando** — se algo demora
- **Configurações** — se houver o que configurar

O padrão desses estados é da skill `06-interface`, na hora de construir. Aqui basta dizer que existem.

**Se for site**, ofereça fazer o design em HTML antes de seguir: olhar e corrigir uma tela desenhada é mais barato que corrigir código. Se ele quiser, pare aqui; quando o design chegar, o `00-planejar` roda no caminho normal, com o escopo já cortado. **Se for app**, a descrição das telas basta: elas nascem direto no `03-desenvolver`.

## Passo 5: Esboçar os dados

Só o suficiente para decidir se precisa de banco, e de que tipo. Schema é trabalho da skill `banco`, depois.

- **Que coisas o projeto guarda?** Uma linha por tipo (tarefa, cliente, lançamento), com os campos principais
- **O que se relaciona com o quê**, em uma frase cada
- **Dado de pessoa?** Se sim, marque: puxa `08-lgpd` no roteiro
- **Fica no aparelho ou na nuvem?** Amarre com a pergunta 3

Se o projeto não guarda nada, diga com clareza: é ótima notícia, e corta banco, login e metade da auditoria de segurança.

## O que mostrar antes da stack

Com as respostas, mostre e **pare para confirmar**, antes de recomendar stack — a stack depende do escopo, e escopo que muda depois refaz o plano:

1. **O núcleo** em uma frase
2. **v1 / Depois / Não vai ter**
3. **Telas**, em tabela: `Tela | Mostra | A pessoa faz | Vai para`
4. **Dados**, em lista curta
5. **Riscos do escopo** — o que pode demorar mais do que parece: sincronização, pagamento, login, integração com serviço de terceiro, publicação em loja. Uma linha cada

Confirmado, siga para a Fase 3 da skill.

## Onde fica escrito

No `PLANO.md`, antes da stack, entram as seções **Núcleo**, **Escopo** (as três listas), **Telas** e **Dados**. É ali que o `03-desenvolver` lê as telas, no lugar do design, e é a seção "Não vai ter" que segura o escopo quando bater a vontade de acrescentar mais uma coisa.
