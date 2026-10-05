# Documentação: consultar antes de escrever

Padrão obrigatório do kit `lz`. Não é comando e não se pede aprovação para seguir — só para as mudanças em si.

## A regra

**O que você sabe de cor é de uma versão qualquer. O projeto usa uma versão específica.** Antes de escrever código com uma biblioteca ou framework, confirme a API na documentação **da versão instalada neste projeto**.

É o que evita o erro mais comum e mais chato de achar: código que parece certo, está bem escrito, e usa uma API que aquela versão não tem mais — ou que nunca teve, porque a resposta veio de memória.

Isso vale ao escrever e ao auditar. Não use "na versão X isso mudou" como suposição: confirme.

## Onde consultar, nesta ordem

1. **MCP do próprio serviço**, quando existir (Supabase, Firebase, Vercel, GitHub). É o único que responde sobre o **seu** projeto: schema real, policies ativas, variáveis configuradas, além da API atual.
2. **Context7** — documentação de bibliotecas e frameworks, por versão. É o padrão do kit para tudo que é biblioteca.
3. **Site oficial**, por busca, quando o Context7 não tem a biblioteca ou a resposta está incompleta.
4. **O próprio código do projeto** — quando o padrão já existe ali, siga o que está. É a consulta mais barata e mantém a consistência: uma segunda forma de fazer a mesma coisa é pior que uma forma desatualizada.

## Como usar o Context7

Duas ferramentas, nesta ordem:

1. `resolve-library-id` — `libraryName` (o nome da biblioteca) e `query` (o que você quer saber). Devolve o id, no formato `/vercel/next.js`, `/supabase/supabase`.
2. `query-docs` — `libraryId` (o id resolvido) e `query` (a pergunta).

Duas coisas mudam a qualidade da resposta:

- **Pergunte a tarefa, não o assunto.** "como configurar contextIsolation e preload com IPC seguro" traz o que serve; "documentação do Electron" traz um resumo genérico e gasta contexto à toa.
- **Diga a versão**, quando o projeto fixa uma. A versão está no `package.json`, no lockfile ou no `contexto.md`.

Se o Context7 não estiver disponível na sessão, a skill `01-ambiente` em modo preparar instala. Não siga de memória porque o MCP não está ligado: ligue.

## Quando consultar é obrigatório

- **Primeira vez** que este projeto encosta nessa biblioteca — a partir daí o padrão fica no código e no `CONVENCOES.md`
- **Configuração e setup**: arquivo de config, plugin, build, inicialização do SDK
- **Biblioteca que quebrou API entre versões maiores** (a lista abaixo)
- **A versão instalada é mais nova** do que a que você conhece
- **Regra de segurança de serviço**: RLS do Supabase, regras do Firestore, isolamento no Electron. Errar aqui é vulnerabilidade, não bug
- **Uma tentativa já falhou** com erro de API inexistente, assinatura diferente ou import que não resolve. Não tente a segunda vez de memória — consulte antes
- **Antes de aprovar um salto de versão major** em correção de dependência

## Quando não precisa

Consultar tudo gasta contexto sem ganhar nada. Não precisa para:

- JavaScript, CSS e HTML puros; lógica própria do projeto
- Algo que o projeto **já faz** em outro arquivo: copie o padrão de lá
- Mudança de texto, cor, espaçamento, conteúdo
- Refatoração que não muda chamada de biblioteca

## Por tecnologia: o que sempre conferir

| Tecnologia | O que confirmar na documentação antes de escrever |
|---|---|
| **Electron** | `contextIsolation`, `nodeIntegration`, `preload` e IPC seguro; empacotamento e assinatura; versão do Chromium daquela major |
| **Firebase** | SDK modular (v9+) x namespaced antigo — a sintaxe é outra; security rules; o que pode ficar no cliente |
| **Supabase** | versão do cliente e dos helpers de auth; RLS e `auth.uid()`; mudanças entre v1 e v2 |
| **React** | comportamento de hooks e effects na versão instalada; se é Next, o que é Server e o que é Client Component |
| **Next.js** | App Router x Pages Router; onde ficam metadados, rotas de API e cache — muda a cada major |
| **Expo / React Native** | a versão do SDK manda: cada biblioteca tem faixa compatível. Confira antes de instalar, não depois de quebrar |
| **Tailwind** | v3 com `tailwind.config` x v4 com configuração no CSS |
| **.NET / WPF / MAUI** | APIs por versão do target framework; o que existe em MAUI e não existe em WPF |
| **Gateway de pagamento** | versão da API e formato do webhook; nunca escreva webhook de memória |

## O que fazer quando não achar

- Diga que não achou, qual biblioteca e o que tentou. Não preencha o buraco com API plausível.
- Escreva o código pela forma mais conservadora, marque com `TODO(doc): confirmar <o que> em <biblioteca>` e **rode para ver funcionando**.
- Registre no registro da etapa, em "Verificar manualmente": trecho escrito sem confirmação na documentação.

## Registrar

No `auditorias/contexto.md`, na seção **Stack e serviços**, mantenha a versão das bibliotecas principais junto do nome (`Next.js 15`, `Expo SDK 52`, `Electron 33`). É o que permite a próxima conversa consultar a versão certa sem reabrir o `package.json`.

Quando a consulta revelar um padrão que o projeto vai repetir (a forma de criar cliente, de proteger rota, de registrar IPC), escreva no `CONVENCOES.md`. Aí a próxima vez não precisa consultar de novo.
