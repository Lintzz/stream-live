---
name: rodar
description: "Roda o projeto para ver na tela, sem gerar build quando nao precisa - site com npm run dev, app Expo ou React Native no emulador Android, Electron, Tauri, .NET, Flutter. Detecta o tipo do projeto, confere dependencias (instaladas e, uma vez por dia, vulnerabilidades e versoes atrasadas), .env e emulador, sobe e testa se abriu de verdade. Use quando o usuario pedir para rodar, testar, iniciar, startar, abrir no emulador ou ver o projeto funcionando."
---

# Rodar o projeto em desenvolvimento

Leia primeiro `.claude/lz/FLUXO.md` e `.claude/lz/DETECCAO.md`. A detecção de tipo, comandos e emulador está toda lá.

Esta skill é para **ver funcionando rápido**, sem gerar build de distribuição. Não é uma auditoria: não precisa de relatório nem de aprovação para rodar. Execute e informe o resultado.

## 1. Descubra como roda

Siga a ordem de detecção do `DETECCAO.md`. Se o `auditorias/contexto.md` já tem a seção "Como rodar e gerar build", use o que está lá, a menos que o `package.json` ou os arquivos de configuração tenham mudado desde então.

Se houver mais de uma parte rodável (web e mobile no mesmo repositório, front e back separados), pergunte qual — ou todas, em processos separados.

Para Expo, decida entre **abrir sem compilar** e **compilar** conforme a seção Expo do `DETECCAO.md`, e diga qual escolheu. O padrão é o caminho sem compilar sempre que ele funcionar.

## 2. Confira o que costuma travar

Antes de iniciar, verifique rapidamente — e só o que se aplica:

- **Dependências instaladas.** Se faltar `node_modules` (ou equivalente), instale com o gerenciador do lockfile em modo que **não altera o lockfile** (`npm ci`, `pnpm install --frozen-lockfile`, `yarn install --frozen-lockfile`). Se o lockfile estiver desatualizado e a instalação falhar, pare e avise em vez de atualizar por conta própria.
  - Se a instalação reportar **vulnerabilidade**, não deixe passar nem trate como ruído: carregue `.claude/skills/dependencias/SKILL.md` e faça ao menos as Fases 0 e 1 dela — classificar o aviso e ver se é produção ou desenvolvimento. Não rode `audit fix` aqui: o objetivo desta skill é ver o projeto rodando; a correção fica para a `dependencias`, com aprovação.
- **Dependências em dia — uma vez por dia**, em projeto novo ou antigo. Veja a linha "Dependências conferidas em" do `auditorias/contexto.md`. Se não é de hoje, rode a visão de produção do `audit` e a lista de desatualizadas do gerenciador (tabela da Fase 1 da skill `dependencias`), sem atrasar o projeto de subir. O resultado é **uma linha**: `Dependências: 0 críticas/altas em produção · 3 majors atrasadas (react, vite, eslint)`. O que já está em "Aceitas" no `auditorias/dependencias.md` não conta.
  - **Crítica ou Alta em produção:** carregue a skill `dependencias`, faça a triagem (Fases 0 a 2) e proponha a correção. Não aplique aqui — o projeto continua rodando, e a correção espera aprovação.
  - **Só majors atrasadas, ou só vulnerabilidade de desenvolvimento:** fica na linha, sem mais nada. Atualizar é decisão sua, quando quiser.
  - **Sem internet ou sem o comando:** pule e diga em meia linha.
- **Variáveis de ambiente.** Se existe `.env.example` e falta `.env` (ou falta alguma variável), liste quais faltam. **Não crie `.env` com valores inventados.** Se o projeto sobe sem elas, rode e avise o que não vai funcionar.
- **Porta ocupada.** Se a porta padrão já está em uso, veja se é o próprio projeto rodando de antes; se for, reaproveite em vez de subir outro.
- **Emulador** (projetos Android): siga a seção Emulador do `DETECCAO.md` — confirmar dispositivo, iniciar o AVD se preciso e esperar o boot terminar antes de mandar o app.

## 3. Rode

- Servidor de desenvolvimento é um processo **contínuo**: rode em segundo plano, para a sessão não ficar presa esperando ele terminar.
- Acompanhe a saída até o ponto em que o projeto declara que está pronto (URL local impressa, "compiled successfully", bundle do Metro concluído, janela do Electron aberta).
- Se aparecer erro nessa fase, leia o erro, diga a causa provável e a correção — **não altere código para "fazer subir" sem me perguntar**, a não ser que a correção seja de ambiente (instalar dependência faltante, iniciar emulador).

## 4. Teste se subiu de verdade

"O comando não deu erro" não é o mesmo que "está funcionando". Confirme:

- **Web:** faça uma requisição para a URL local e confira resposta 200 e HTML com conteúdo. Se houver ferramenta de navegador disponível, abra a página e verifique o console sem erros.
- **Mobile:** o app foi instalado ou aberto no emulador e o bundle carregou (sem tela vermelha de erro). Se possível, confira com `adb logcat` filtrado pelo app nos primeiros segundos.
- **Desktop (Electron, Tauri, .NET):** o processo continua vivo depois de alguns segundos e não há erro na saída.

## 5. Informe

Em poucas linhas:

- Tipo detectado e comando usado (e, no Expo, se compilou ou não e por quê)
- A linha das dependências, se a checagem do dia rodou agora
- Onde abrir: URL local, emulador, janela
- O que ficou de fora: variável faltando, funcionalidade que não vai funcionar assim
- Como parar

## Registro

Grave ou atualize a seção **"Como rodar e gerar build"** do `auditorias/contexto.md` no formato do `DETECCAO.md`: tipo, gerenciador, comando de teste rápido, comando compilando, emulador padrão, e a data e o resultado da checagem de dependências. Da próxima vez, essa skill vai direto ao comando.

Não crie outro registro: rodar não é etapa de auditoria.
