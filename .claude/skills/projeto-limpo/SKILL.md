---
name: projeto-limpo
description: "Gera a versao de entrega do projeto: uma copia sem nenhum rastro de IA nem do kit (sem .claude, CLAUDE.md, auditorias, PLANO.md, CONVENCOES.md, arquivos de Cursor, Gemini, Copilot, Aider e scripts de teste), que roda sozinha. Quando e para cliente, faz tambem a conferencia de entrega - titularidade das contas, chaves pessoais do desenvolvedor, README, credenciais - e escreve o guia de entrega com as recomendacoes nao aplicadas. A pasta original nao e tocada. Use quando o usuario for entregar o projeto ao cliente, mandar para alguem, publicar um repositorio sem seus arquivos de trabalho, ou pedir uma versao limpa, sem IA, sem o kit."
---

# Projeto limpo: a versão de entrega

Leia primeiro `.claude/lz/FLUXO.md`.

É o que se roda depois da `14-revisao-geral`, quando o projeto vai sair das suas mãos. Faz duas coisas:

1. **A cópia limpa** — sempre. O projeto sem o kit e sem rastro de IA, rodando.
2. **A conferência de entrega** — só quando a cópia vai para um cliente. O que ele precisa para ser dono do projeto sem depender de você.

Esta skill **não é a `limpeza`**. A `limpeza` procura código morto **dentro** do projeto — função que ninguém chama, dependência sem uso. Esta aqui tira o kit e o rastro de IA **de fora** dele: as skills, o `CLAUDE.md`, as auditorias, os arquivos de outras ferramentas de IA e os scripts de teste. O que sobra é o projeto funcional e nada mais.

Se o projeto é um site em HTML, CSS e JavaScript, o resultado é o HTML, o CSS, o JavaScript e os arquivos de que eles precisam. Nada além.

## O modelo de segurança: nada é apagado

**Ela nunca apaga na pasta de trabalho.** Ela **copia** o projeto para `../<nome-do-projeto>-entrega/` e limpa a cópia. A pasta original continua exatamente como está, com o kit instalado e funcionando.

Isso é de propósito: você continua trabalhando no original, e a cópia é o que você zipa, entrega ou publica. E se algo na limpeza ficou errado, o custo é apagar a cópia e rodar de novo.

Se o usuário pedir explicitamente para limpar **na própria pasta**, é possível — mas só com estas três condições, e diga as três:

1. Nada pendente no `git status` e tudo já commitado
2. Um commit antes de mexer, para o `git revert` funcionar
3. Ele confirmando que sabe que vai perder o kit naquele projeto (volta com `npx github:Lintzz/lz-kit`, mas as auditorias e o contexto se vão)

## Antes de copiar: é para cliente?

Leia o perfil em `auditorias/contexto.md`. Perfil C ou D, ou o usuário dizendo que vai mandar ao cliente: faça a conferência de entrega **antes** de copiar, porque parte dela muda o original (README, `.env.example`, troca de chave) e a cópia tem que sair já com isso. Projeto pessoal ou publicação de repositório: pule esta seção.

**Histórico.** Leia os registros em `auditorias/`. Se o `12-pre-lancamento.md` não existe ou o veredito não foi 🟢, ou se a `14-revisao-geral` não rodou depois da última mudança, **avise no topo do relatório** que a entrega está saindo sem essa conferência. Reúna tudo que ficou **pendente** ou como **decidi não corrigir**: vai para o guia de entrega.

**Checklist** — só o que o projeto tem:

1. **Serviços e titularidade.** Cada serviço externo (domínio, DNS, hospedagem, repositório, banco, e-mail, formulário, pagamento, analytics, Search Console, CMS, API paga, fonte ou template com licença), numa tabela `Serviço | Para que serve | Pago/Gratuito | Renovação | Titular (🔍) | Ação`. O recomendado é tudo **no nome e no e-mail do cliente** — domínio no CPF ou CNPJ dele —, com você como colaborador. O código não mostra o titular: marque 🔍 e peça para o usuário conferir.
2. **Chaves e contas suas no projeto:** chave de API, conta de teste, e-mail pessoal, serviço no seu cartão. Liste para trocar pelas do cliente, e depois da troca teste de novo o que elas afetam (formulário, login, pagamento).
3. **README e `.env.example`.** O README diz o que é o projeto, como rodar, como publicar e quais variáveis existem (nomes e para que servem, **sem valores**). O `.env.example` tem todas as variáveis que o código usa. Faltando, proponha — no original.
4. **Versão fechada:** tag, `CHANGELOG.md` atualizado e metadados com o dono do produto certo. App instalável: o arquivo final sai da skill `build`, e o keystore ou certificado fica com o cliente ou com quem vai manter, fora do repositório — sem ele, ninguém publica atualização.
5. **Credenciais:** por gerenciador de senhas ou link de uso único, nunca por WhatsApp ou e-mail em texto. O cliente troca as senhas iniciais e liga a verificação em duas etapas.
6. **Licenças** de template, fonte, ícone e imagem permitem uso comercial por este cliente. Se o `08-lgpd` já cobriu, traga as pendências de lá.
7. **Conforme o projeto:** backup automático e quem sabe restaurar, e plano gratuito que pausa por inatividade (tem banco) · usuário administrador do cliente criado e suas contas de teste removidas (tem login) · gateway no CNPJ do cliente, recebendo na conta dele (tem pagamento) · Search Console e Google Business Profile na conta do cliente (está no ar) · guia de edição para leigo, ou aviso de que alteração depende de desenvolvedor (cliente edita conteúdo) · monitoramento de disponibilidade e de expiração do domínio no e-mail do cliente (sugestão, prioridade baixa).

**Guia de entrega.** Com isso, escreva `ENTREGA.md` para ir **na cópia**, em linguagem simples e sem citar o kit, auditorias ou IA: o que o cliente tem, onde acessa cada coisa, o que vence e quando, quem procurar em caso de problema, e as **recomendações não aplicadas**, agrupadas por área, com o risco de cada uma em uma frase. Como `auditorias/` não vai na cópia, é este arquivo que leva essas recomendações por escrito. Use `[preencher]` onde faltar informação.

## O que sai da cópia

**Antes de tudo: segredo e coisa gerada.** A cópia é do disco, não do Git — então o que o `.gitignore` protegia **seria copiado** se você não tirar. Este grupo é o mais importante da skill:

| Nunca vai para a cópia | Por que |
|---|---|
| `.env`, `.env.local`, `.env.production` e qualquer `.env` que não seja o `.example` | **são as suas credenciais.** Copiar isso é vazar chave de produção na entrega. O `.example` vai no lugar |
| `*.keystore`, `*.jks`, `*.p12`, `*.pfx`, `*.pem`, `*.mobileprovision`, certificado | chave de assinatura é sua, não do projeto |
| `node_modules/`, `.venv/`, `__pycache__/`, `vendor/` | reinstalável, e enorme |
| `dist/`, `build/`, `out/`, `.next/`, `.nuxt/`, `.svelte-kit/`, `.output/`, `builds/` | build antiga confunde: quem recebe não sabe se é atual. Gera de novo a partir do código |
| `*.exe`, `*.msi`, `*.apk`, `*.aab`, `*.ipa`, `*.dmg`, `*.appx` | executável não é código-fonte. Se o que você entrega **é** o binário, ele sai da skill `build`, não daqui |
| `.cache/`, `.turbo/`, `*.log`, `.DS_Store`, `Thumbs.db`, `.vscode/`, `.idea/` | cache, log e configuração do seu editor |

Se algum desses arquivos for **realmente** parte do que o cliente precisa (um `.pem` que é do produto, um dado gerado que não se regenera), ele fica — mas diga qual e por quê, em voz alta, antes de copiar.

Depois desse grupo, leia `.claude/lz/manifesto.json`: ele lista o que o kit instalou naquele projeto, então você sabe exatamente o que é do kit em vez de adivinhar.

**Do kit lz**

| Sai | Observação |
|---|---|
| `.claude/` inteira | skills, regras do fluxo, manifesto |
| `CLAUDE.md` | **só o bloco do kit**, entre `<!-- lz:inicio -->` e `<!-- lz:fim -->`. Se sobrar conteúdo próprio, o arquivo fica com ele; se ficar vazio, o arquivo sai |
| `auditorias/` inteira | inclui o relatório de segurança, que é justamente o que não deve ir para fora |
| `PLANO.md`, `CONVENCOES.md` | documentação de processo, não do produto |
| `.githooks/` | a trava de commits é sua, não do projeto entregue |
| bloco do kit no `.gitignore` | só entre `# lz-kit (início)` e `# lz-kit (fim)`; o que você escreveu fora fica |

**De outras ferramentas de IA** — procure e remova o que existir: `.cursor/`, `.cursorrules`, `.windsurfrules`, `AGENTS.md`, `GEMINI.md`, `.gemini/`, `.github/copilot-instructions.md`, `.aider*`, `.clinerules`, `.roo/`, `.continue/`, `.codeium/`, `.specstory/`, `.junie/`, `.trae/`, `.kilocode/`, `.zed/`.

**Testes e verificação** — na cópia de entrega eles saem: `tests/`, `test/`, `__tests__/`, `e2e/`, `.maestro/`, `scripts/smoke*`, `scripts/screenshot*`, `scripts/test-*`, `playwright.config.*`, `vitest.config.*`, `jest.config.*`, `knip.json`, `capturas/`, `test-results/`, `playwright-report/`.

**Documento solto de trabalho** — `ANALISE.md`, `RESUMO.md`, `NOTAS.md`, `IMPLEMENTATION_PLAN.md`, `TODO.md` e parecidos. **Estes você nunca remove por padrão de nome:** mostre a lista e pergunte, porque um deles pode ser documentação de verdade do projeto.

## O que nunca sai

- Todo o código, estilo, imagem, fonte e dado que o projeto usa
- `README.md`, `LICENSE`, `CHANGELOG.md` — são documentação do produto, não do processo
- `.env.example` — é o que diz ao próximo quais variáveis existem. O `.env` de verdade **nunca** vai
- Lockfile, `package.json`, e qualquer arquivo de configuração de que a build dependa
- Migrations e seeds

Na dúvida sobre um arquivo, **ele fica**, e você o lista como "deixei, confirme se pode sair". Cópia com um arquivo a mais é um detalhe; cópia que não roda é retrabalho.

## Depois de remover: fazer a cópia voltar a fechar

**É aqui que uma limpeza malfeita se revela.** Remover arquivo é a parte fácil; o que quebra a entrega é a referência que ficou apontando para o que não existe mais. Passe por todos os itens abaixo — nenhum é opcional.

**`package.json`** — o campeão de problema:

| Procure | Por que quebra |
|---|---|
| `"test"`, `"smoke"`, `"e2e"`, `"knip"` nos scripts | apontam para arquivo ou ferramenta que saiu; rodar dá erro |
| `"prepare": "husky"` (ou `husky install`) | **o pior de todos:** com `.husky/` removido, o `npm install` do cliente **falha inteiro**. Tire o script |
| devDependencies só dos testes — `playwright`, `@playwright/test`, `vitest`, `jest`, `@testing-library/*`, `knip`, `supertest` | não quebram por estarem lá, mas engordam o install à toa. Liste quais vai tirar e confirme que nenhum arquivo que **ficou** as importa |
| `"main"`, `"bin"`, `"exports"`, `"files"` | apontando para caminho removido, o pacote não resolve |

**Arquivos de configuração que ficaram:**

- `vite.config.*`, `next.config.*`, `webpack.config.*` — se **importam** algo que saiu (plugin de teste, `setup.ts`), a build morre na hora
- `tsconfig.json` — `include`, `paths` ou `references` apontando para pasta removida faz o `tsc` falhar
- configuração do ESLint que estende arquivo removido
- `.github/workflows/` — passo que roda script apagado passa a falhar no CI de quem receber

**`README.md` que fala do kit.** Linha citando comando do kit (`/rodar`, `/build`), skill, Claude, `auditorias/` ou `CONVENCOES.md` é rastro de IA e aponta para o que não existe na cópia. Troque pelo comando real (`npm run dev`, `npm run build`) ou tire a linha.

**Import de arquivo removido é bloqueio, não limpeza.** Se algum arquivo que ficou importa um que saiu, **pare**: aquele arquivo não era só de teste. Mostre o import e reveja o que deve mesmo sair.

**Não copie `node_modules/`.** Além de ser enorme, é o que força o teste de verdade do passo seguinte: instalar do zero é o que revela o `prepare` quebrado e a configuração que não resolve.

## E então prove que roda

Não entregue cópia sem ter visto funcionar. Na pasta da cópia:

1. **Instale do zero**, do lockfile e sem `node_modules` copiado (`npm ci` ou o equivalente). Falha aqui é quase sempre script `prepare` sobrando
2. **Rode a build** inteira
3. **Suba o projeto e abra**: a página ou tela principal aparece, e o console não mostra erro novo
4. **Rode cada script que sobrou** no `package.json`. Script que existe e não funciona é pior que script que não existe

**Antes de entregar, uma última varredura por credencial:** procure na cópia por `.env`, por chave de assinatura e por padrões de segredo no código (`sk_`, `service_role`, `secret`, `Bearer `, senha em texto). Um segredo que escapou não é pendência para depois — é bloqueio, e a chave precisa ser rotacionada.

**Se a cópia não roda, a limpeza está errada** — e não é problema do usuário descobrir isso depois de entregar. Volte, veja o que faltou, e corrija. Compare com o original rodando, se precisar.

## O `.git/` sai também

A cópia é uma entrega: **não leva `.git/` nenhum.** Não copie a pasta, não rode `git init`, não crie commit inicial. O que o cliente recebe é uma pasta de arquivos que ele abre, zipa ou põe no repositório dele.

Isso resolve de uma vez o problema que apagar arquivo não resolve: **o histórico guarda tudo que já existiu.** Com o `.git/` na cópia, qualquer pessoa leria `auditorias/07-seguranca.md` — o mapa das vulnerabilidades do projeto — num commit antigo, mesmo com o arquivo apagado no último. Sem `.git/`, não há histórico para ler.

Duas consequências práticas:

- Não sobra `core.hooksPath` para limpar, porque não há configuração de Git na cópia.
- O `.gitignore` **fica, mas enxuto** — ver a seguir.

Nunca reescreva o histórico do projeto **original** por causa disso. O original é seu e continua com tudo.

## O `.gitignore` da cópia fica limpo

Ele fica, porque no dia em que quem recebeu criar o repositório dele vai precisar. Mas fica **só com o essencial**, e isso exige poda:

**Tire** toda linha que ignora algo que não existe mais na cópia — `.claude/`, `auditorias/`, `PLANO.md`, `CONVENCOES.md`, `test-results/`, `playwright-report/`, `capturas/`, `.maestro/` — e toda linha de ferramenta de IA: `.cursor/`, `.cursorrules`, `.windsurfrules`, `GEMINI.md`, `.aider*`, `.clinerules`.

Isso não é só arrumação: **um `.gitignore` falando de `.claude/` é, por si só, um rastro de IA na entrega.** Denuncia o que a skill acabou de remover.

**Fica** o que um programador chegando no projeto ignoraria de qualquer jeito: dependências, saídas de build, `.env` (com a exceção `!.env.example`), chaves de assinatura, cache, log, lixo de sistema e de editor.

Se o `.gitignore` do projeto tem seções que você não reconhece, **não apague no escuro**: liste e pergunte. Linha que ignora algo que existe na cópia tem motivo para estar lá.

## Formato do relatório

Antes de copiar, mostre e pare:

1. **Aviso**, se a entrega vai sem pré-lançamento aprovado ou sem revisão geral atualizada (só para cliente)
2. **Conferência de entrega** (só para cliente): tabela de serviços, pendências antes de entregar em ordem de prioridade, e o rascunho do `ENTREGA.md`
3. **Para onde vai a cópia** e a confirmação de que o original não será tocado
4. **Sai:** agrupado por origem (kit, outras ferramentas de IA, testes), com a contagem de arquivos
5. **Preciso que você confirme:** os documentos soltos e qualquer arquivo em que você ficou na dúvida
6. **Ajustes na cópia:** scripts, dependências de desenvolvimento, CI e README que vão mudar
7. **Segredos e gerados que não vão** — liste nominalmente, para o usuário conferir que nenhuma credencial vai na entrega
8. **`.gitignore`:** as linhas que saem e as que ficam
9. **Sem `.git/` e sem `node_modules/`** — confirme que ficou claro que a cópia é uma pasta solta, não um repositório

Depois de rodar:

10. **Antes e depois** em número de arquivos e tamanho em disco
11. **A cópia roda:** o que você executou e o que viu
12. **Como rodar a cópia**, em duas linhas, para colar no e-mail da entrega — incluindo o `npm ci`, já que ela vai sem dependências instaladas

## Registro

Grave `auditorias/projeto-limpo.md` **no projeto original** — na cópia essa pasta não existe: data, para onde foi a cópia, o que saiu, o que você decidiu manter, os ajustes feitos e o resultado do teste. Se foi para cliente, também: o que foi transferido, o que ainda está nas suas contas e por quê, e as recomendações não aplicadas que foram no `ENTREGA.md`.

Marque no `auditorias/roteiro.md` a data da entrega.

E diga ao usuário, em uma linha, que o kit volta na pasta original com `npx github:Lintzz/lz-kit` — nada do que saiu da cópia se perdeu do original.
