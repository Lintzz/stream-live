# Guia de decisão: hospedagem e domínio

Consulte este guia na hora de recomendar onde o projeto vai rodar. Ele dá o **critério de decisão**, não uma lista de marcas. Preços e limites de plano gratuito mudam com frequência: confirme na página oficial antes de citar número, e informe a data da consulta.

## Ordem das perguntas

Decida nesta ordem. Cada resposta elimina opções.

1. **O site precisa de servidor rodando o tempo todo?**
   Site estático, SSG ou serverless não precisa. Nesse caso, hospedagem de borda (plataformas de deploy contínuo a partir do repositório) resolve, sai de graça ou muito barato, e escala sozinha. A maioria dos projetos de cliente para aqui.

2. **Precisa de processo contínuo?**
   Fila, worker, cron pesado, WebSocket persistente, bot que fica ligado, ou algo que precisa de mais de alguns segundos por requisição. Aí entra servidor dedicado ou VPS, ou uma plataforma de containers.

3. **O tráfego é imprevisível ou muito grande?**
   Escala automática favorece as plataformas de borda. VPS é previsível no custo, mas tem teto: se estourar, é você que sobe a máquina.

4. **Há requisito de onde o dado mora?**
   Dado que precisa ficar em região específica limita as opções e muda a latência.

5. **Quem vai manter?**
   VPS significa alguém cuidando de sistema operacional, atualização de segurança, certificado, backup e monitoramento. Se o cliente não tem quem faça e você não vai dar manutenção, VPS é uma dívida, não uma economia.

## Regra prática

- **Landing page, site institucional, portfólio, blog** — plataforma de deploy a partir do repositório, plano gratuito. É o caso mais comum. Não recomende nada mais pesado sem motivo.
- **Aplicação com login e banco** — mesma plataforma, com funções serverless e um serviço de banco gerenciado. Ainda não precisa de VPS.
- **E-commerce próprio** — mesma base, atenção a limites de função e a custo de imagem.
- **Processamento pesado, mídia, fila, serviço sempre ligado** — VPS ou containers.
- **Cliente com exigência corporativa de nuvem específica** — siga a exigência, e diga que o custo de complexidade sobe.

Nuvem grande (AWS, GCP, Azure) para site de cliente pequeno costuma ser a escolha errada: mais peças, mais configuração, conta imprevisível e mais superfície para errar. Recomende só se houver exigência real.

## Limites que costumam doer

Avise sobre eles **antes** de escolher, não depois:

- Limite de banda e de build no plano gratuito
- Tempo máximo de execução de função serverless
- Tamanho máximo de upload por requisição
- Limite de transformação e otimização de imagens
- Projeto de banco gratuito que **pausa por inatividade** — péssimo para site de cliente que recebe poucas visitas
- Uso comercial permitido ou não no plano gratuito
- Quantidade de colaboradores no plano gratuito

## Domínio

Trate como etapa separada, porque quase sempre atrasa.

- **O normal é o projeto começar sem domínio.** Publique na URL temporária da plataforma para o cliente ver e registre o estágio como `preview`. Nada de cobrar canonical, sitemap e SSL de domínio nessa fase.
- **O domínio deve ser comprado pelo cliente**, no CPF ou CNPJ dele, com o e-mail dele. Você entra como colaborador. Domínio comprado no seu cartão vira problema na renovação e refém na hora de transferir.
- `.com.br` exige CPF ou CNPJ brasileiro e tem regras próprias de titularidade; `.com` é mais simples de transferir. Diga isso ao escolher.
- **Renovação:** deixe claro quem paga e em que data. Domínio expirado derruba o site inteiro e o e-mail junto.
- **DNS:** decida se fica no registrador ou num serviço de DNS separado. Se o cliente já usa e-mail no domínio, cuidado redobrado: mexer em DNS errado derruba o e-mail da empresa.
- Quando o domínio chegar, a skill `13-dominio` fecha tudo que ficou adiado.

## E-mail no domínio

Ponto que costuma ser esquecido no planejamento e vira urgência depois:

- E-mail profissional (`contato@dominio`) é serviço à parte, quase sempre pago por caixa, e não vem com a hospedagem.
- Se o site envia e-mail (formulário, confirmação, recuperação de senha), isso exige serviço de envio e configuração de SPF, DKIM e DMARC no DNS. Sem isso, a mensagem cai em spam.
- São duas coisas diferentes: caixa de entrada para a pessoa ler, e envio automático pelo sistema. Diga qual das duas o projeto precisa — às vezes é só uma.

## O que registrar no plano

Para a opção recomendada e para a alternativa mais simples:

- Onde hospeda, e por quê, ligado a uma necessidade real confirmada
- Custo mensal estimado, com data da consulta
- Qual limite do plano gratuito provavelmente vai ser o primeiro a apertar
- O que muda se o projeto crescer, e quanto de trabalho é migrar depois
- Quem cria a conta, e em nome de quem
