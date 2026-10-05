# Guia: estrutura de pastas e convenções de código

Use este guia para escrever o `CONVENCOES.md` do projeto. O objetivo é que qualquer arquivo tenha **um lugar óbvio**, e que eu e o agente coloquemos as coisas sempre no mesmo lugar.

Regra geral: adote a estrutura que o framework escolhido já espera. Inventar organização própria em cima de um framework que tem convenção só cria atrito.

## Princípios que valem para qualquer stack

1. **Uma pasta, um propósito.** Se você precisa pensar para decidir onde um arquivo vai, a estrutura está errada.
2. **Nada de pasta-depósito.** `utils/` e `helpers/` viram lixeira. Prefira nomes pelo domínio: `formatarMoeda` vai em `lib/formato.ts`, não em `utils/index.ts`.
3. **Proximidade.** O que muda junto fica junto. Estilo, teste e subcomponentes de um componente ficam perto dele.
4. **Agrupe por tipo enquanto for pequeno, por funcionalidade quando crescer.** Landing page: `components/`, `lib/`, `styles/`. Aplicação com várias áreas: `features/agendamento/`, `features/pagamento/`, cada uma com seus componentes, hooks e serviços dentro.
5. **Profundidade pequena.** Passar de três ou quatro níveis é sinal de que a divisão está fina demais.
6. **Nada de código morto.** Arquivo comentado inteiro, componente não usado e pasta `old/` não entram no repositório: o histórico do Git já guarda isso.

## Nomes

Escolha uma convenção e mantenha em todo o projeto:

- **Arquivos e pastas:** `kebab-case` (`form-contato.tsx`). É o mais seguro, porque evita problema entre sistemas que diferenciam maiúsculas e os que não diferenciam.
- **Componentes React/Vue:** `PascalCase` no nome do componente. Se o arquivo também usa PascalCase, mantenha em todos.
- **Funções e variáveis:** `camelCase`. **Constantes globais:** `MAIÚSCULO_COM_UNDERLINE`.
- **Tipos e interfaces:** `PascalCase`.
- **Booleanos:** comece com `e`/`tem`/`is`/`has` (`estaCarregando`, `temEstoque`).
- **Idioma:** escolha um. Misturar `getUsuario` com `buscarUser` é o erro mais comum. Para projeto de cliente brasileiro, português no domínio (`pedido`, `cliente`) e inglês nos termos técnicos (`handleSubmit`, `useState`) funciona bem — só deixe a regra escrita.
- **Nada de abreviação obscura:** `usr`, `qtd2`, `temp3`.

## Estruturas por tipo de projeto

### Site estático ou landing page

```
/
├── index.html
├── public/            imagens, fontes, favicon, robots.txt, sitemap.xml
├── src/
│   ├── styles/        tokens.css (cores, fontes, espaçamentos) + estilos
│   ├── scripts/       JavaScript
│   └── partials/      pedaços reaproveitados, se houver build
└── README.md
```

Mesmo aqui, extraia **tokens** de cor, fonte e espaçamento do design para um arquivo só. É o que evita seis tons de azul espalhados pelo CSS.

### Site ou app com framework (Next.js e similares)

```
src/
├── app/ (ou pages/)   rotas — segue a convenção do framework
├── components/
│   ├── ui/            botão, input, card: genéricos, sem regra de negócio
│   └── secoes/        hero, depoimentos, faq: blocos da página
├── features/          só quando o projeto cresce: uma pasta por área
├── lib/               integrações e funções de apoio (cliente do banco, formatação)
├── hooks/
├── types/             tipos compartilhados
├── styles/
└── config/            constantes, dados do site (contato, links, serviços)
public/
```

Duas regras que mais evitam bagunça:

- **`components/ui/` não conhece o negócio.** Um `Botao` não sabe o que é "agendamento".
- **Conteúdo fixo do site vai em `config/`**, não espalhado no JSX. Telefone, endereço, links de redes e lista de serviços num arquivo só: é o que permite trocar sem caçar pelo código.

### Projeto com backend próprio

```
src/
├── routes/ (ou api/)      entrada: recebe requisição, valida, responde
├── services/              regra de negócio
├── repositories/ (ou db/) acesso a dados
├── middlewares/
├── schemas/               validação de entrada e saída
├── lib/
└── config/
```

A regra que sustenta isso: **a rota não fala com o banco direto**. Rota valida e chama serviço; serviço decide; repositório consulta. Em projeto pequeno pode juntar serviço e repositório, mas a rota nunca monta query.

### Front e back no mesmo repositório

```
apps/
├── web/
└── api/
packages/
└── shared/     tipos e validações usados pelos dois
```

Só adote se front e back forem publicados separados. Para projeto pequeno, um framework que faz os dois é mais simples.

## Arquivos que todo projeto deve ter

- `README.md` — o que é, como rodar, como publicar, variáveis necessárias
- `.env.example` — nomes das variáveis, sem valores
- `.gitignore`
- `CONVENCOES.md` — este padrão, escrito para o projeto
- `PLANO.md` — stack, roteiro e escopo

## Onde ficam as coisas que costumam ficar soltas

- **Imagens** em `public/` (ou na pasta pública do framework), com nome legível: `equipe-joao.webp`, nunca `IMG_2931.jpg`
- **Textos do site**: em `config/` se forem fixos, no CMS ou banco se mudarem
- **Tipos** gerados a partir do banco: em `types/`, gerados por comando, nunca editados à mão
- **Segredos**: só em variável de ambiente
- **Migrations**: na pasta que a ferramenta do banco define, nunca soltas

## O que escrever no `CONVENCOES.md`

Só o que vale para **este** projeto, decidido, não uma lista de opções:

1. A árvore de pastas real, com uma linha explicando cada uma
2. As convenções de nome escolhidas, com um exemplo de cada
3. Idioma do código
4. Onde entra cada tipo de arquivo novo (componente, página, rota, tipo, imagem)
5. Onde ficam os tokens de design e a regra de não usar cor fora deles
6. Padrão de validação adotado
7. Padrão de commit: Conventional Commits (`feat:`, `fix:`, `docs:`, `refactor:`, `chore:`, com `!` para mudança que quebra). É o que permite à skill `build` decidir o próximo número sozinha
8. **Testes**: o nível do projeto, o comando de rodar, onde os casos ficam, e a regra de que fluxo novo ganha caso. É essa linha escrita que faz a verificação ser rodada sempre, em vez de só quando alguém lembra
9. O que **não** fazer neste projeto, com base no que já deu errado

Esse arquivo é o que a skill `nova-feature` lê para manter o padrão quando o projeto crescer.
