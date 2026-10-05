---
name: 05-conversao
description: "Audita conversao e UX do site: hero section, calls-to-action, prova social, formularios e botao de WhatsApp existente. Use quando o usuario pedir para revisar se o site converte, melhorar o hero, avaliar os CTAs ou revisar a experiencia do visitante."
---

# Auditoria de Conversão e UX

Leia primeiro `.claude/lz/FLUXO.md`: ele define a ordem das etapas, o protocolo de perguntas, o arquivo `auditorias/contexto.md`, o vocabulario de status e o formato do registro. As regras de la valem aqui.

Você é um especialista em conversão (CRO) e UX revisando este site. Seu trabalho nesta etapa é **analisar e propor**, não reescrever. Não altere nenhum arquivo nem texto do site até eu aprovar.

## Fase 0: Entenda o projeto e o objetivo

Monte um **Perfil** curto:

- **Tipo:** landing page / institucional / portfólio / e-commerce / sistema interno
- **Negócio e público:** o que é oferecido e para quem
- **Conversão principal:** qual é a ação que o site existe para gerar? (chamar no WhatsApp, preencher formulário, agendar, comprar, doar, ligar...)
- **Conversões secundárias**, se houver
- **Canais de contato existentes:** WhatsApp, telefone, e-mail, formulário, redes sociais

Se o projeto for um **sistema interno ou painel logado** (não uma página de marketing), diga isso e aplique apenas os itens de usabilidade e acessibilidade; o resto vai para N/A.

## Fase 0.5: Contexto e perguntas

Leia `auditorias/contexto.md` e siga o protocolo de perguntas do FLUXO: no máximo 5 perguntas **no total**, todas de uma vez, com opções e recomendação marcada, e nada que já esteja no contexto. As perguntas comuns (site publicado e domínio definitivo, cliente ou projeto próprio, conteúdo final ou rascunho, o que está planejado e ainda não foi feito) só entram se o contexto não responder. Das específicas desta etapa, abaixo, fique com as que mudam o resultado:

1. Qual é a ação principal que o site deve gerar (WhatsApp, formulário, agendamento, compra, doação, ligação)?
2. O cliente tem depoimentos, avaliações, número de clientes ou cases que eu possa usar?
3. Posso reescrever os textos do site ou eles são fechados pelo cliente?

## Fase 1: Decida o que se aplica

Só audite os grupos cujo gatilho existe. O resto vai para a lista N/A com motivo.

## Fase 2: Checklist

### Hero Section (primeira tela)

1. **Teste dos 5 segundos:** olhando só a primeira tela no celular, dá para entender **o que é, para quem é, por que escolher e qual o próximo passo**?
2. **Título** focado no benefício ou resultado para o cliente, não genérico ("Bem-vindo ao nosso site", "Soluções de qualidade").
3. **Subtítulo** que complementa com o como/diferencial.
4. **CTA principal visível** sem rolar, inclusive no celular.
5. **Imagem ou visual** relevante ao que é oferecido (não banco de imagem genérico, se houver alternativa).
6. **Prova rápida** perto do topo, se existir: nota de avaliações, número de clientes, logos, anos de experiência.

Se o hero estiver fraco, **não reescreva direto**: proponha **3 variações** de título + subtítulo + texto do CTA, cada uma com um ângulo diferente (ex.: resultado, dor, autoridade), usando só informações reais do site.

### Calls-to-Action

7. **Um CTA principal** claro por página, com verbo de ação e específico ("Agendar avaliação grátis", não "Enviar" ou "Saiba mais").
8. **Repetição estratégica:** CTA no hero, após seções de benefício/prova social e no final da página.
9. **Destaque visual:** contraste com o fundo, cor reservada ao CTA, área de toque de pelo menos 44×44px no celular.
10. **Sem competição:** CTAs secundários visualmente menos fortes que o principal.

### Botão flutuante de WhatsApp (somente se o site JÁ TEM esse botão)

Se o site não tem botão flutuante de WhatsApp, marque este grupo inteiro como N/A e não sugira implementar.

11. Link no formato `https://wa.me/55DDDNUMERO?text=...` (só números, com código do país), com **mensagem pré-preenchida** e codificada na URL, idealmente dizendo de onde a pessoa veio ("Olá! Vim pelo site e gostaria de...").
12. Abre em nova aba com `rel="noopener noreferrer"`; tem `aria-label`.
13. No celular, não cobre conteúdo, outros botões nem o banner de cookies; tamanho adequado ao toque; fica acima de outros elementos (`z-index`) sem atrapalhar.
14. Se houver analytics, o clique é registrado como evento.

### Prova social e confiança

15. Depoimentos reais (com nome, foto ou contexto), avaliações, cases, antes/depois, certificações. Aponte onde encaixariam se houver material; **não invente depoimentos**.
16. Informações de contato e, se for negócio local, endereço e horário fáceis de achar.
17. Se o site coleta dados pessoais ou usa rastreamento (pixel, analytics): **política de privacidade** e aviso de cookies (LGPD).

### Formulários (se tem)

18. Só os campos realmente necessários para a conversão (cada campo a mais derruba o envio); botão com texto de ação; o que acontece depois do envio está claro (resposta em quanto tempo, por qual canal). Os estados do formulário — erro inline, carregamento, sucesso, teclado móvel e preenchimento automático — ficam na skill `06-interface`.

### Usabilidade e acessibilidade básicas (sempre)

19. Layout funciona bem no celular (a maioria do tráfego); texto legível sem zoom; contraste suficiente; navegação simples; foco visível ao navegar por teclado.
20. Ordem das seções conta uma história: problema → solução → como funciona → prova → oferta/CTA → dúvidas (FAQ) → CTA final.

### Medição (se há algum analytics)

21. Conversões principais rastreadas como eventos (clique no WhatsApp, envio de formulário, clique em telefone). Se não houver analytics, apenas recomende, sem prioridade alta.

## Como investigar

- Cite **evidência concreta**: componente/arquivo e o texto atual.
- Avalie pensando **no celular primeiro**.
- Use apenas informações reais do site nas sugestões de texto. Se faltar informação (diferencial, números, depoimentos), liste as **perguntas que devo fazer ao cliente**.

## Formato do relatório

1. **Perfil** (com a conversão principal identificada)
2. **Resumo:** o que mais impede o visitante de converter hoje, em 3–5 linhas
3. **Tabela:** `# | Item | Status | Prioridade | Evidência | Sugestão`
   - Status: ✅ OK / ⚠️ Parcial / ❌ Falha / 🔍 Verificar manualmente
4. **Propostas de hero** (se aplicável): 3 variações
5. **Perguntas para o cliente** (informações que melhorariam o site)
6. **Itens N/A** com motivo

Depois do relatório, pare e pergunte o que devo aplicar.

## Registro

Depois que eu aprovar e as correções forem aplicadas, salve um resumo em `auditorias/05-conversao.md` (na raiz do projeto, fora da pasta pública) com: data, itens corrigidos, pendências, itens que decidi não corrigir (com o motivo) e as perguntas ao cliente ainda sem resposta. Esse arquivo é lido pelo pré-lançamento, pela revisão geral e, na entrega ao cliente, pelo `projeto-limpo`. Se o repositório for público, siga a ressalva do FLUXO sobre `auditorias/`: avise e ofereça ignorar a pasta, sem decidir sozinho.
