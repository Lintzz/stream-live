---
name: 06-interface
description: "Padrao de estados e comportamentos de interface - carregamento, vazio, erro, sucesso, acoes destrutivas, conteudo extremo e campos de entrada. Consulte ao construir telas, listas, formularios, botoes e componentes interativos, e para auditar se a interface se comporta bem em todos os estados. Use quando o trabalho envolver skeleton ou loading, estado vazio, confirmacao de exclusao, mostrar senha, copiar para a area de transferencia, texto longo quebrando layout, erro de formulario, inputmode ou autocomplete."
---

# Estados e comportamentos de interface

Leia primeiro `.claude/lz/FLUXO.md`: ele define o protocolo de perguntas, o `auditorias/contexto.md`, o vocabulário de status e o formato do registro. As regras de lá valem aqui.

A `05-conversao` pergunta se o site convence o visitante. Esta skill pergunta outra coisa: **a interface se comporta bem em todos os estados**, e não só no estado ideal que aparece no design?

Como a `banco`, ela **não precisa ser chamada**: carregue sozinho ao construir tela, lista, formulário ou componente. Tem dois modos:

- **Modo consulta** — estou construindo uma tela, lista, formulário ou componente. Aplique estes padrões ao que for escrever, antes de escrever. Não peça relatório para seguir o padrão.
- **Modo auditoria** — quero saber se a interface existente está dentro do padrão. Aí vale o fluxo normal: analisar, relatar, pedir aprovação.

Siga o `CONVENCOES.md` do projeto: estados novos usam os componentes, tokens e padrões que já existem. Se o projeto já tem um componente de botão com loading, reuse; não crie outro.

## Gatilhos

Esta skill pesa conforme o projeto:

- **Landing page ou site institucional estático** — quase tudo é N/A. Aplica-se apenas: formulário (estado de envio, erro, sucesso), campos de entrada, texto que pode estourar, e a 404.
- **App com login, painel, área do cliente, e-commerce** — aplica-se praticamente tudo.

Não crie estado que o projeto não tem. Site sem lista não precisa de estado vazio; site sem ação destrutiva não precisa de modal de confirmação.

## Não duplique outras skills

Estes itens pertencem a outras etapas. Aqui só confira que existem, e cite a skill de origem:

- `alt` em imagens e labels de campos → `10-acessibilidade`
- `autocomplete` → `10-acessibilidade` (aqui só verifique no contexto dos campos)
- Página 404 → `09-seo` (aqui só o conteúdo e a ação de volta)
- Bloqueio de envio duplicado **testado no ar** → `12-pre-lancamento`

## Os estados

### 1. Carregando

1. **Skeleton** onde há dado vindo de fora e o layout já é conhecido: listas, cards, tabelas, perfil. O skeleton tem a forma do conteúdo final, para não causar salto de layout quando os dados chegam.
   - Use **só quando o carregamento passa de uns 300 ms**. Abaixo disso, o skeleton pisca e piora a sensação.
   - **Não use em conteúdo estático.** Site sem carregamento de dados não ganha nada com loader falso.
   - Respeite `prefers-reduced-motion` na animação de brilho.
2. **Botão em loading** em toda ação que espera resposta (salvar, enviar, pagar, entrar):
   - desabilitado enquanto processa, com indicador visual e texto de estado ("Enviando…");
   - **bloqueio de duplo clique** — a proteção de verdade é no servidor (idempotência), mas o botão não deve permitir o segundo envio;
   - largura fixa, para o botão não mudar de tamanho ao trocar o texto;
   - `aria-busy="true"` ou anúncio para leitor de tela.
3. **Carregamento longo** (upload, processamento): progresso real quando possível; se não, mensagem dizendo o que está acontecendo.

### 2. Vazio

4. **Estado vazio** em toda lista, tabela, busca ou tela que pode não ter dados. Diferencie os três casos, porque pedem mensagens diferentes:
   - **primeiro uso** ("Você ainda não tem pedidos") → explica e oferece a ação principal ("Fazer primeiro pedido");
   - **busca ou filtro sem resultado** ("Nenhum resultado para 'xyz'") → oferece limpar o filtro;
   - **tudo concluído** ("Nenhuma tarefa pendente") → confirma que está tudo certo.
   - Nunca uma área em branco nem uma tabela só com cabeçalho.

### 3. Erro

5. **Erro inline nos campos:**
   - validação **ao sair do campo** (blur), não a cada tecla — mostrar erro enquanto a pessoa ainda digita irrita;
   - depois que o erro apareceu, ele some **assim que o campo fica válido**, já durante a digitação;
   - mensagem **abaixo do campo**, em texto, dizendo como corrigir ("Informe um e-mail no formato nome@dominio.com"), e não só "inválido";
   - ao enviar com erro, foco vai para o primeiro campo inválido;
   - nunca apagar o que a pessoa já digitou por causa de um erro;
   - erro associado ao campo (`aria-describedby`, `aria-invalid`) — o detalhe de acessibilidade fica na skill `10-acessibilidade`.
6. **Erro de requisição** (servidor fora, sem internet, tempo esgotado): mensagem humana, sem código técnico nem stack trace, e uma ação ("Tentar de novo"). O que a pessoa preencheu continua lá.
7. **Página 404** com identidade do site, mensagem clara e caminho de volta (início, busca ou links principais).
8. **Erro inesperado de renderização:** em framework de componentes, um limite de erro evita que uma falha num pedaço derrube a tela inteira.

### 4. Sucesso e feedback

9. **Confirmação de ação concluída:** depois de salvar, enviar ou pagar, a pessoa sabe que deu certo — mensagem, redirecionamento para uma página de obrigado ou mudança visível no item.
10. **Feedback ao copiar:** o ícone troca para um check (ou aparece "Copiado!") por 1 a 2 segundos e volta ao normal. Anuncie para leitor de tela com `aria-live`. Se a cópia falhar (navegador sem permissão), avise em vez de fingir sucesso.
11. **Avisos temporários (toasts)** para confirmações, não para erros que exigem ação — erro importante fica visível até ser resolvido.

### 5. Ações destrutivas

12. **Confirmação antes de ação irreversível** (excluir conta, apagar registro sem lixeira, cancelar assinatura):
    - o modal diz **o que** vai ser perdido, com o nome do item ("Excluir o pedido #1042?");
    - o botão de confirmação repete a ação ("Excluir pedido"), não "OK" ou "Sim";
    - o botão destrutivo tem estilo de perigo, e o foco inicial fica em **Cancelar**;
    - para algo muito grave (excluir conta), peça para digitar o nome ou a palavra de confirmação.
13. **Para o que dá para desfazer, prefira "desfazer" a confirmar.** Arquivar, remover do carrinho, marcar como lida: faça na hora e ofereça "Desfazer" por alguns segundos. Modal em excesso ensina o usuário a clicar em OK sem ler — e aí a confirmação não protege nem quando importa.

### 6. Conteúdo extremo

O design mostra o caso bonito. Teste os extremos:

14. **Texto longo:** nome, título, e-mail e endereço grandes não podem quebrar o layout. Use truncamento com `…` (em uma ou mais linhas) **com um jeito de ver o texto completo** (tooltip, `title`, expandir ou a página de detalhe).
    - **Nunca trunque informação crítica**: preço, valor total, nome no checkout, dado que a pessoa precisa conferir.
    - Palavras sem espaço (URL, e-mail, hash) precisam quebrar (`overflow-wrap: anywhere`) em vez de estourar a tela.
    - Em flex e grid, o item precisa de `min-width: 0` para o truncamento funcionar.
15. **Conteúdo ausente:** usuário sem foto (avatar com iniciais), produto sem imagem (placeholder), campo opcional vazio (não mostrar rótulo solto).
16. **Números grandes e formatação:** valores em reais com `Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })`, datas no formato brasileiro, números grandes que não estouram o card.
17. **Muitos itens:** lista com centenas de itens tem paginação ou carregamento incremental; tabela larga tem rolagem horizontal própria no celular, sem rolar a página inteira.

### 7. Entrada de dados

18. **Teclado móvel correto** com `type` e `inputmode`:
    - e-mail → `type="email"`; telefone → `type="tel"`;
    - PIN, código de verificação, CEP e CPF → `inputmode="numeric"` (e não `type="number"`, que remove zeros à esquerda e mostra setas);
    - valores com centavos → `inputmode="decimal"`;
    - busca → `type="search"` ou `enterkeyhint="search"`.
19. **Preenchimento automático** com `autocomplete` nos campos comuns (`name`, `email`, `tel`, `postal-code`, `street-address`, `current-password`, `new-password`, `one-time-code`). O detalhe completo está na `10-acessibilidade`.
20. **Mostrar/ocultar senha:**
    - botão dentro do campo alternando entre `type="password"` e `type="text"`;
    - é um `<button type="button">` (para não enviar o formulário), com `aria-label` que muda ("Mostrar senha" / "Ocultar senha") ou `aria-pressed`;
    - não quebra o gerenciador de senhas — o campo mantém `name` e `autocomplete` corretos;
    - volta a ocultar ao enviar.
21. **Máscaras** (CPF, telefone, CEP) que não atrapalham colar, apagar nem o preenchimento automático, e que guardam o valor sem a formatação.

## Modo auditoria

1. Faça o inventário de **telas, listas, formulários e ações** do projeto.
2. Para cada um, verifique os estados que se aplicam — não os que não existem.
3. Quando possível, teste de verdade: simule rede lenta, lista vazia, texto enorme e falha de requisição, em vez de só ler o código.

Relate no formato `# | Tela/componente | Estado | Status | Evidência | Correção sugerida`, com os status do FLUXO e prioridade Alta (quebra uso ou causa ação errada, como exclusão sem confirmação ou envio duplicado), Média (confunde) ou Baixa (acabamento).

Depois do relatório, pare e pergunte o que devo aplicar.

## Registro

Em modo auditoria, depois das correções aprovadas, salve `auditorias/06-interface.md` com data, o que foi corrigido, pendências com prioridade e o que decidi não corrigir. Se o projeto adotou componentes padrão para esses estados (botão com loading, estado vazio, modal de confirmação), registre onde eles ficam no `CONVENCOES.md`, para que código novo os reutilize.

Em modo consulta, não crie registro.
