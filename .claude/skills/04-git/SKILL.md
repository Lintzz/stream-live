---
name: 04-git
description: "Audita o repositorio Git: segredos vazados no historico, .env commitado por engano, .gitignore, repositorio publico, arquivos grandes, preview deployments e GitHub Actions. Use antes do primeiro push, ao suspeitar que uma chave ou .env foi enviado ao GitHub, ou quando o usuario pedir revisao do repositorio."
---

# Auditoria de Git e Repositório

Leia primeiro `.claude/lz/FLUXO.md`: ele define a ordem das etapas, o protocolo de perguntas, o arquivo `auditorias/contexto.md`, o vocabulario de status e o formato do registro. As regras de la valem aqui.

Você está revisando o **repositório** deste projeto, não o site em si: o que foi versionado, o que vazou para o histórico e como o código chega até a hospedagem. Seu trabalho é **verificar e relatar**. Não altere nenhum arquivo nem rode nenhum comando que reescreva o histórico até eu aprovar.

Esta é a primeira auditoria do projeto, e deve ser rodada **antes de publicar o repositório ou dar acesso a alguém**. Se encontrar segredo exposto, isso vira prioridade sobre todo o resto.

## Fase 0: Entenda o repositório

Rode e interprete:

```
git remote -v
git status --short
git log --oneline -20
git log --format='%an <%ae>' | sort -u
git ls-files | head -100
du -sh .git
```

Monte um **Perfil do repositório**:

- **Existe repositório Git?** Se não, diga isso: a maior parte deste checklist vira N/A e a recomendação é criar um antes de continuar.
- **Tem remoto?** Onde (GitHub, GitLab, Bitbucket)? Já houve `push`?
- **Público ou privado?** Se não der para saber pelo código, marque **🔍 Confirmar no painel**.
- **Quem aparece como autor dos commits** (nome e e-mail). Em repositório público, esse e-mail fica visível.
- **Deploy:** a hospedagem está conectada ao repositório (Vercel, Netlify, GitHub Pages, Actions) ou o upload é manual?
- **O repositório vai para o cliente?**

## Fase 0.5: Contexto e perguntas

Leia `auditorias/contexto.md` e siga o protocolo de perguntas do FLUXO: no máximo 5 perguntas **no total**, todas de uma vez, com opções e recomendação marcada, e nada que já esteja no contexto. As perguntas comuns (site publicado e domínio definitivo, cliente ou projeto próprio, conteúdo final ou rascunho, o que está planejado e ainda não foi feito) só entram se o contexto não responder. Das específicas desta etapa, abaixo, fique com as que mudam o resultado:

1. O repositório é público ou privado, e o cliente vai receber acesso a ele?
2. Em algum momento você acha que commitou `.env`, senha, chave ou arquivo de cliente?
3. O e-mail que aparece nos commits pode ficar visível publicamente?

## Fase 1: Decida o que se aplica

Só audite os grupos cujo gatilho existe. O resto vai para a lista N/A com motivo.

## Fase 2: Checklist

### Segredos (prioridade máxima)

1. **Segredo rastreado agora:** procure em `git ls-files` por `.env` e variantes, `.pem`, `.key`, `.p12`, `.sql`, `credentials`, `service-account.json`, backups e planilhas de cliente. Leia também arquivos de config versionados em busca de chave escrita direto no código.
2. **Segredo no histórico**, mesmo que o arquivo já tenha sido apagado depois:
   ```
   git log --all --full-history --name-only -- "*.env*" ".env" "*.pem" "*.key"
   git log -p --all -S "SUPABASE_SERVICE_ROLE" | head -50
   ```
   Se possível, rode `gitleaks detect --no-git=false` ou `trufflehog filesystem .`. Se não tiver a ferramenta, informe o comando para eu rodar.
3. **Se encontrar segredo exposto**, a ordem das ações é esta, e não pode ser invertida:
   1. **Rotacionar a chave** no painel do serviço (gerar nova, revogar a antiga). Isso é o que realmente resolve. Enquanto a chave antiga existir, ela continua válida para quem copiou.
   2. Remover o arquivo do rastreamento (`git rm --cached .env`) e garantir que está no `.gitignore`.
   3. Só então, se fizer sentido, limpar o histórico com `git filter-repo` ou BFG.
   4. Avisar que limpar o histórico **não garante remoção**: forks, clones, caches do GitHub e bots que varrem repositórios públicos podem já ter copiado. Por isso o passo 1 é o que conta.
   
   Classifique a exposição: repositório **público** com chave de produção = crítico e urgente; repositório **privado** = grave, mas o risco é menor (rotacione mesmo assim, principalmente se houve colaborador ou fork).

### `.gitignore` (sempre)

4. O instalador do kit mantém um bloco `# lz-kit (início)…(fim)` no `.gitignore` com dependências, saídas de build, segredos, artefatos de teste e cache. Os arquivos do kit **não** são ignorados: vão para o repositório de propósito. Confira que ele está lá e **não edite dentro dele** — o que for seu vai fora do bloco. Além disso, o arquivo cobre, no mínimo: `.env` e variantes (`.env.local`, `.env.production`), `node_modules/`, saída de build (`dist/`, `build/`, `.next/`, `out/`, `builds/`, `android/app/build/`), chaves de assinatura (`*.keystore`, `*.jks`, `*.pfx`), `.DS_Store`, logs, `coverage/`, arquivos temporários e de backup, uploads de usuário, chaves (`*.pem`, `*.key`).
5. **`.env.example` versionado** (só nomes de variáveis, sem valores) para o projeto ser reproduzível.
6. **Registros do kit:** `auditorias/` vai para o repositório, e o registro de segurança descreve vulnerabilidades. Em repositório **privado**, tudo certo. Em repositório **público**, siga a ressalva do FLUXO: avise e ofereça ignorar a pasta. Para mandar o projeto a alguém, o caminho é a `projeto-limpo`, que gera uma cópia sem o kit e sem histórico.
7. Nada que deveria ser ignorado está rastreado hoje. Cheque especialmente `node_modules/` e pasta de build versionados por engano.

### Conteúdo do repositório (sempre)

8. **Arquivos grandes:** liste o que passa de ~5 MB (vídeos, PSD, imagens originais, dumps). Repositório inchado é lento de clonar; sugira Git LFS ou guardar fora do repositório.
9. **Dados de terceiros:** contratos, planilhas, documentos, fotos e áudios do cliente que não precisam estar versionados.
10. **Caminhos e informações locais:** caminhos absolutos da sua máquina, comentários internos, anotações e `TODO` que não deveriam ser lidos pelo cliente.

### Se o repositório é público

11. Revise o que está exposto sob a ótica de quem não deveria ver: dados pessoais do cliente, e-mails, telefones, preços internos, estrutura de pastas com nomes internos.
12. **Recomendação padrão:** trabalho de cliente fica em repositório **privado**, salvo decisão em contrário.
13. **E-mail dos commits:** se o e-mail do autor for pessoal e não quiser expô-lo, indique configurar um e-mail de privacidade (`@users.noreply.github.com`) e avise que os commits antigos mantêm o e-mail original.
14. **Licença:** repositório público sem licença não autoriza reuso. Se for código de cliente, é mais um motivo para mantê-lo privado.

### Commits e branches (sempre)

15. Mensagens de commit legíveis e sem segredo escrito na própria mensagem.
16. Existe uma branch principal limpa, o deploy sai dela, e não há branches antigas com código quebrado ou dados de teste esquecidos.
17. Nenhuma alteração importante só na sua máquina (`git status` com arquivos não commitados que deveriam estar versionados).

### Se a hospedagem está conectada ao repositório (Vercel, Netlify, Cloudflare Pages)

18. **Variáveis de ambiente no painel da hospedagem**, nunca no repositório.
19. **Branch de produção correta** configurada.
20. **Preview deployments:** cada branch ou pull request pode gerar uma URL pública com versão não finalizada, indexável e às vezes apontando para dados de produção. Avalie se devem ser protegidos por senha ou desativados.

### Se usa GitHub Pages

21. Só conteúdo estático e público; nada de backend, chave ou dado de cliente; pasta/branch publicada é a correta; se houver domínio próprio, o arquivo `CNAME` está versionado.

### Se usa GitHub Actions ou outro CI

22. Segredos em **Secrets** do repositório, nunca no arquivo do workflow.
23. Workflow não imprime variáveis sensíveis no log.
24. Actions de terceiros fixadas por versão ou SHA, não por `@master`.
25. Permissões do `GITHUB_TOKEN` reduzidas ao necessário.

### Se o repositório vai para o cliente

26. Transferência de propriedade ou acesso definido; colaboradores antigos removidos; e uma última conferida no histórico para ver se há algo que não deveria seguir junto (chaves antigas, dados de outro cliente, código de outro projeto).

## Como investigar

- Rode os comandos e **mostre o resultado relevante**, não só a conclusão.
- **Nunca rode nada que reescreva o histórico** (`filter-repo`, `rebase`, `push --force`) por conta própria. Descreva o comando e me deixe decidir.
- Se não tiver acesso ao remoto, marque **🔍 Confirmar no painel** e diga onde olhar.

## Formato do relatório

1. **Perfil do repositório**
2. **Veredito de segredos:** 🔴 Segredo exposto (rotacionar agora) / 🟡 Suspeita a confirmar / 🟢 Nada encontrado
3. **Se houver segredo:** a lista do que vazou, onde, e o passo a passo de rotação na ordem certa
4. **Tabela:** `# | Item | Status | Severidade | Evidência | Correção sugerida`
   - Status: ✅ OK / ⚠️ Parcial / ❌ Falha / 🔍 Confirmar no painel
5. **Comandos que recomendo rodar**, com o que cada um faz
6. **Itens N/A** com motivo

Depois do relatório, pare e pergunte o que devo aplicar.

## Registro

Depois que eu aprovar e as correções forem aplicadas, salve um resumo em `auditorias/04-git.md` com: data, veredito de segredos, chaves que foram rotacionadas (só o nome, nunca o valor), o que foi ajustado no `.gitignore`, pendências e itens que decidi não corrigir, com o motivo. Esse arquivo é lido pelo pré-lançamento, pela revisão geral e, na entrega ao cliente, pelo `projeto-limpo`.
