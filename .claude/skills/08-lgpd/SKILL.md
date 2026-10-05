---
name: 08-lgpd
description: "Audita privacidade e requisitos legais brasileiros: mapa de dados coletados, politica de privacidade, consentimento de cookies antes de analytics e pixel, direitos do titular, regras de e-commerce e cobranca recorrente. Use quando o usuario pedir revisao de LGPD, politica de privacidade, banner de cookies ou conformidade legal do site."
---

# Auditoria de LGPD e Requisitos Legais

Leia primeiro `.claude/lz/FLUXO.md`: ele define a ordem das etapas, o protocolo de perguntas, o arquivo `auditorias/contexto.md`, o vocabulario de status e o formato do registro. As regras de la valem aqui.

Você está revisando este site quanto a privacidade (LGPD — Lei 13.709/2018) e requisitos legais comuns para sites no Brasil. Seu trabalho nesta etapa é **levantar e relatar**. Não altere nem crie arquivos até eu aprovar.

**Importante:** esta é uma revisão técnica de boas práticas, não um parecer jurídico. Deixe isso claro no relatório. Textos como política de privacidade e termos gerados aqui são **rascunhos** que o cliente deve revisar, idealmente com um advogado, especialmente em e-commerce ou quando há dados sensíveis.

## Fase 0: Mapeie quais dados o site coleta e para onde vão

Explore o código, formulários, scripts de terceiros, cookies, backend e integrações. Monte um **Perfil**:

- **Tipo:** landing page / institucional / portfólio / blog / app com login / e-commerce / doações
- **Quem é o responsável pelo site:** empresa (CNPJ) ou pessoa física? (se não der para saber, marque 🔍)
- **Mapa de dados**, em tabela: `Dado coletado | Onde é coletado | Para que serve | Para onde vai (serviço/terceiro) | Por quanto tempo fica`
  - Formulários (nome, e-mail, telefone, mensagem...)
  - Cadastro e login
  - Dados de compra, entrega ou pagamento
  - Cookies e rastreamento: Google Analytics, Tag Manager, Meta Pixel, Hotjar/Clarity, chat, embeds de YouTube e mapas
  - Newsletter
  - Uploads
- **Dados sensíveis?** saúde, religião, origem racial, biometria, dados de crianças e adolescentes (ex.: clínica, escola, academia, igreja)

Se o site **não coleta nenhum dado pessoal e não usa cookies de rastreamento** (ex.: landing page só com link para WhatsApp e sem analytics), diga isso claramente: a maior parte deste checklist vira N/A.

## Fase 0.5: Contexto e perguntas

Leia `auditorias/contexto.md` e siga o protocolo de perguntas do FLUXO: no máximo 5 perguntas **no total**, todas de uma vez, com opções e recomendação marcada, e nada que já esteja no contexto. As perguntas comuns (site publicado e domínio definitivo, cliente ou projeto próprio, conteúdo final ou rascunho, o que está planejado e ainda não foi feito) só entram se o contexto não responder. Das específicas desta etapa, abaixo, fique com as que mudam o resultado:

1. Quem é o responsável pelo site: empresa com CNPJ ou pessoa física?
2. Para onde vão os dados do formulário e quem tem acesso a eles?
3. Vai ter Google Analytics, Meta Pixel ou outro rastreamento?
4. As imagens e fotos usadas são de banco licenciado, do próprio cliente, ou de origem desconhecida?

## Fase 1: Decida o que se aplica

Só audite os grupos cujo gatilho existe. O resto vai para a lista N/A com motivo.

## Fase 2: Checklist

### Sempre

1. **Identificação do responsável:** o site mostra quem é a empresa ou profissional e um meio de contato. Para empresas, recomendável exibir razão social e CNPJ no rodapé (obrigatório em e-commerce, ver abaixo).
2. **Direitos de uso de conteúdo:** imagens, fontes, ícones, vídeos e textos têm origem e licença que permitem uso comercial. Aponte imagens de origem incerta (baixadas do Google, marcas d'água, bancos pagos sem licença) e fotos de pessoas reais sem indicação de autorização.
3. **Marcas de terceiros:** logos de clientes/parceiros/"atendemos convênios" usados com autorização (**🔍 Confirmar com o cliente**).

### Se coleta qualquer dado pessoal (formulário, cadastro, newsletter, compra)

4. **Política de privacidade** existe, está acessível (rodapé e perto dos formulários) e cobre, em linguagem clara:
   - quem é o controlador e como contatá-lo (e o encarregado/DPO, se houver)
   - quais dados são coletados e para quê
   - base legal de cada uso (ex.: execução de contrato, consentimento, legítimo interesse)
   - com quem são compartilhados (liste os serviços reais detectados: hospedagem, e-mail, gateway, analytics)
   - por quanto tempo são guardados
   - direitos do titular (acesso, correção, exclusão, portabilidade, revogação do consentimento) e como exercê-los
   
   Aponte o que falta e, se não existir, prepare um **rascunho** baseado no mapa de dados real da Fase 0. Não invente dados que o site não coleta.
5. **Minimização:** os formulários pedem só o necessário para a finalidade. Aponte campos desnecessários (ex.: CPF num simples formulário de contato).
6. **Transparência no ponto de coleta:** perto do botão de envio, uma frase curta com link para a política ("Ao enviar, você concorda com nossa Política de Privacidade" ou similar).
7. **Consentimento para marketing:** se os dados serão usados para newsletter, promoções ou remarketing, há checkbox **separado e desmarcado por padrão**; sem consentimento embutido em "aceito os termos".
8. **Segurança do dado em trânsito e armazenado:** HTTPS; dados não expostos em URLs, logs ou planilhas públicas. (A auditoria de segurança cobre isso em detalhe; aqui, apenas confirme o básico.)

### Se usa cookies ou rastreamento não essenciais (analytics, pixel, mapas de calor, embeds que rastreiam)

9. **Aviso de cookies** presente, informando categorias e finalidades.
10. **Consentimento de verdade:** scripts não essenciais só carregam **depois** do aceite (verifique no código se GA/Pixel são carregados direto no `<head>` antes de qualquer consentimento); opção de recusar tão fácil quanto aceitar; possibilidade de mudar a escolha depois. Referência: Guia Orientativo de Cookies da ANPD.
11. **Política de cookies** (pode ser seção da política de privacidade) listando os cookies/serviços usados.
12. Alternativas mais leves quando fizer sentido: embeds do YouTube em modo `youtube-nocookie.com` com fachada, analytics sem cookies.

### Se tem cadastro / login

13. Usuário consegue ver e corrigir seus dados e solicitar exclusão da conta (pelo site ou por canal informado).
14. **Termos de uso** definindo regras da plataforma, responsabilidades e cancelamento.

### Se é e-commerce (Código de Defesa do Consumidor e Decreto 7.962/2013)

15. **Identificação completa:** razão social, CNPJ (ou CPF), endereço físico e canais de contato, em local visível.
16. **Informações da oferta:** preço total com impostos e frete discriminados antes do pagamento; condições de pagamento; prazos de entrega; restrições da oferta.
17. **Direito de arrependimento:** informação clara sobre desistência em até 7 dias da entrega para compras on-line (art. 49 do CDC) e como exercê-lo.
18. **Políticas de troca, devolução e reembolso** publicadas e acessíveis antes da compra.
19. **Resumo do pedido** antes da confirmação e confirmação imediata do recebimento do pedido (e-mail ou tela).
20. **Termos e condições de compra** publicados.

### Se tem doação ou assinatura recorrente

21. Deixa claro antes do pagamento que a cobrança é recorrente, o valor, a periodicidade e **como cancelar**; o cancelamento é simples e funciona.

### Se trata dados sensíveis ou de crianças e adolescentes

22. Sinalize como **alto risco**: exige base legal específica (consentimento específico e destacado, ou outra hipótese legal aplicável) e, para crianças, consentimento de pelo menos um dos pais ou responsável. Recomende revisão por advogado antes do lançamento.

## Como verificar

- Cite **evidência concreta**: arquivo e linha (ex.: onde o Pixel é carregado, onde está o formulário).
- O que não dá para saber pelo código (CNPJ, contratos, autorizações de imagem, quem tem acesso aos dados), marque **🔍 Confirmar com o cliente**.
- Não afirme que o site "está em conformidade com a LGPD"; diga quais boas práticas estão ou não atendidas.

## Formato do relatório

1. **Aviso:** revisão técnica, não substitui orientação jurídica
2. **Perfil e mapa de dados** (tabela da Fase 0)
3. **Nível de risco geral:** Baixo / Médio / Alto, com uma frase de justificativa
4. **Tabela:** `# | Item | Status | Prioridade | Evidência | O que fazer`
   - Status: ✅ OK / ⚠️ Parcial / ❌ Falha / 🔍 Confirmar com o cliente
5. **Perguntas para o cliente** (informações necessárias para fechar as pendências)
6. **Rascunhos**, se aplicável e faltantes: política de privacidade, texto do aviso de cookies, frase para formulários, termos/políticas de troca. Todos marcados como rascunho, com `[preencher]` onde faltar informação.
7. **Itens N/A** com motivo

Depois do relatório, pare e pergunte o que devo criar ou alterar.

## Registro

Depois que eu aprovar e as correções forem aplicadas, salve um resumo em `auditorias/08-lgpd.md` (na raiz do projeto, fora da pasta pública) com: data, o mapa de dados, itens corrigidos, pendências, itens que decidi não corrigir (com o motivo) e as perguntas ao cliente ainda sem resposta. Esse arquivo é lido pelo pré-lançamento, pela revisão geral e, na entrega ao cliente, pelo `projeto-limpo`. Se o repositório for público, siga a ressalva do FLUXO sobre `auditorias/`: avise e ofereça ignorar a pasta, sem decidir sozinho.
