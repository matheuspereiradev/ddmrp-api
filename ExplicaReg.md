# ExplicaReg — Guia de Regras de Negócio do Sistema

Este documento explica, em linguagem de negócio, o que o sistema faz, quais são as suas rotas (endpoints), o que cada uma delas serve, quais regras de negócio a aplicação aplica, quais mensagens de erro (exceções) podem aparecer e o que elas significam, como funcionam os relatórios e como são calculados os principais indicadores do sistema. Não é um documento técnico de código — o objetivo é responder dúvidas de quem usa o sistema no dia a dia (planejamento de estoque/reposição) sobre "por que aconteceu isso" ou "o que esse campo/relatório significa".

---

## 1. Visão geral

O sistema é uma ferramenta de **planejamento e reposição de estoque baseada em DDMRP** (Demand Driven Material Requirements Planning — uma metodologia de gestão de estoque por "buffers" dinâmicos, em vez de estoque mínimo fixo tradicional). Ele existe para responder, para cada combinação de **produto + centro** (uma filial, depósito ou unidade onde o produto é estocado), três perguntas:

1. **Quanto de buffer (estoque de proteção) esse item deveria ter?** — dividido em três faixas (zona vermelha, amarela e verde).
2. **Qual é a situação atual do item frente a esse buffer?** — está em ruptura, em alerta, saudável ou com excesso de estoque?
3. **Quanto e quando pedir mais?** — a sugestão de quantidade de pedido.

Essas respostas são recalculadas automaticamente, todos os dias, por um processo chamado internamente de **"Robô"**, que primeiro carrega os dados mais recentes vindos de sistemas externos (ingestão) e depois recalcula todos os indicadores (cálculo DDMRP). Veja a seção [19. O Robô](#19-o-robô--ingestão-de-dados-e-cálculo-ddmrp).

### 1.1 Glossário rápido

- **Buffer**: o "colchão" de estoque de proteção de um item, dividido em três zonas (de baixo para cima): **Vermelha** (zona crítica/de segurança), **Amarela** (cobre o consumo esperado durante o tempo de reposição) e **Verde** (define o tamanho do lote de pedido). O topo da zona verde é o **ponto de pedido**: quando a posição de estoque projetada cai nele ou abaixo, o sistema sugere um novo pedido.
- **Netflow (fluxo líquido)**: a posição de estoque "projetada" de um item — estoque físico, mais o que já está a caminho (entradas pendentes), menos a demanda já comprometida (demanda qualificada). É a métrica principal usada para decidir se o item precisa de reposição.
- **Adu (consumo médio diário)**: quantas unidades desse item são consumidas, em média, por dia — calculado a partir do histórico de consumo e/ou da previsão de demanda futura.
- **Adi (intervalo médio de demanda)**: mede o quão intermitente é a demanda de um item — quanto maior, mais espaçadas (irregulares) são as ocorrências de consumo.
- **Lead Time**: tempo de reposição, em dias, entre pedir e receber o item.
- **MOQ**: quantidade mínima de pedido.
- **Cor do Buffer**: uma classificação visual (Vermelho, Amarelo, Verde, Azul, Preto, Sem Cor) que resume, de forma simples, a situação de um item frente ao seu buffer — ver [seção 20.6](#206-cor-do-buffer-bufferColor).
- **BAF, ZAF, DAF**: os três tipos de "ajuste manual temporário" que podem ser aplicados a um item — respectivamente, ajuste de **tipo/tamanho do buffer inteiro**, ajuste de **uma zona específica** e ajuste da **demanda média**. Ver [seção 13](#13-ajustes-de-buffer-baf-zaf-e-daf).
- **Pedido fictício**: um pedido criado manualmente pela interface (não vindo do sistema do cliente) — serve para simulações ("e se eu fizesse esse pedido?") e nunca é confundido com pedidos reais.
- **Workspace**: um espaço de simulação pessoal, por usuário, para testar uma quantidade de pedido diferente da sugerida pelo sistema, sem alterar nada de verdade. Ver [seção 18](#18-workspace--simulação-pessoal).
- **Robô**: o processo automático que carrega dados novos (ingestão) e recalcula todos os indicadores de buffer (cálculo).

### 1.2 Como ler as mensagens de erro

Toda resposta da API segue o mesmo formato, com um indicador de sucesso, os dados (quando aplicável) e uma lista de erros. Os erros de negócio (não erros técnicos do sistema) sempre caem em uma destas categorias:

- **400 (requisição inválida)** — a ação não pode ser feita porque alguma informação enviada está incorreta, ausente, ou viola uma regra de negócio (ex.: "Product not found.", período de vigência sobreposto, campo obrigatório faltando). A mensagem sempre explica exatamente o motivo.
- **404 (não encontrado)** — o registro que se está tentando editar, excluir ou consultar não existe (ou já foi excluído). Mensagem padrão: **"Not found"**.
- **401/403 (não autorizado/proibido)** — a ação exige estar autenticado, ou o usuário autenticado não tem permissão para fazer aquilo especificamente (ex.: editar uma nota de outra pessoa, editar um motivo protegido pelo sistema).

Nas seções abaixo, cada módulo lista as mensagens exatas que podem aparecer e o que fazer quando elas acontecem.

---

## 2. Autenticação e Usuários

O módulo de usuários controla quem acessa o sistema e como a sessão é mantida.

**O que cada rota faz:**
- **Criar usuário**: só pode ser feito por alguém que já está autenticado — não existe cadastro público (auto-registro). Isso é proposital: o acesso ao sistema é sempre concedido por alguém que já está dentro dele (ex.: um administrador cadastrando a equipe), nunca por autoatendimento externo.
- **Listar usuários**: retorna a lista paginada de usuários cadastrados.
- **Login**: recebe e-mail e senha e devolve um par de tokens — um **token de acesso** (usado nas próximas requisições, válido por um tempo curto) e um **token de renovação** (usado só para obter um novo token de acesso quando o atual expirar).
- **Renovar sessão**: troca um token de renovação ainda válido por um par novo de tokens. O token de renovação antigo é imediatamente invalidado (não pode ser usado de novo) — a cada renovação, um novo token substitui o anterior.
- **Encerrar sessão (logout)**: invalida um token de renovação específico. Só o próprio dono do token pode encerrá-lo.
- **Atualizar usuário**: altera nome, e-mail e/ou perfil de acesso (`Role`) de um usuário existente. Não existe endpoint de troca de senha através desta rota.
- **Excluir usuário**: remove (de forma lógica) o usuário.

**Regras de negócio:**
- O e-mail deve ser único no sistema — não é possível cadastrar dois usuários com o mesmo e-mail.
- Todo usuário precisa ter um perfil de acesso (`Role`) válido e existente.
- A senha é sempre armazenada de forma criptografada (nunca em texto puro).

**Mensagens de erro e quando acontecem:**
- **"Email already in use."** (400) — ao tentar criar um usuário com um e-mail que já pertence a outro usuário.
- **"Role not found."** (400) — ao criar ou atualizar um usuário informando um perfil de acesso que não existe.
- **"Invalid credentials"** (400) — no login, quando o e-mail não existe ou a senha está incorreta. A mensagem é sempre a mesma nos dois casos, por segurança (não revela qual dos dois campos está errado).
- **"Invalid refresh token."** — ao tentar renovar a sessão com um token de renovação inexistente, expirado ou já usado/revogado (401); ou ao tentar encerrar (revogar) um token que não existe, que já não está mais ativo, ou que pertence a outro usuário (400 nesse caso — tentar encerrar a sessão de outra pessoa também cai nessa mesma mensagem genérica, sem revelar o motivo específico).
- **"Not found"** (404) — ao tentar atualizar ou excluir um usuário que não existe.

---

## 3. Perfis de Acesso (Role)

Representa o **perfil/papel de acesso** atribuído a um usuário (ex.: Administrador, Operador, Planejador). Todo usuário do sistema tem exatamente um perfil.

- **Criar / Listar / Atualizar (renomear) / Excluir** perfis — operações simples, sem regras de negócio adicionais além de nome obrigatório.
- Não há checagem de nome duplicado — é possível cadastrar dois perfis com o mesmo nome.
- **"Not found"** (404) ao tentar atualizar/excluir um perfil inexistente.

---

## 4. Centros (Center)

Representa um **centro de distribuição, filial, depósito ou unidade física** onde o estoque é mantido e de/para onde os pedidos são movimentados. É referenciado por praticamente todos os outros módulos (produto em estoque, pedidos, ajustes, etc.).

- Campos: código (identificador de negócio, usado inclusive como chave para reconhecer o centro nos arquivos de carga de dados externos), descrição, cidade e zona (região/localização, opcionais).
- Todos os campos podem ser alterados a qualquer momento.
- **"Not found"** (404) ao tentar atualizar/excluir um centro inexistente.

---

## 5. Parceiros (Partner)

Representa um **parceiro comercial** — tipicamente um fornecedor ou transportador — que pode estar associado a um item de estoque (como fornecedor padrão) ou a um pedido específico.

- Campos: código (chave de negócio, também usada na carga de dados externos) e descrição.
- Todos os campos podem ser alterados.
- **"Not found"** (404) ao tentar atualizar/excluir um parceiro inexistente.

---

## 6. Etiquetas (Tag)

Uma **marcação/classificação livre** que pode ser associada a um item de estoque (produto+centro) para agrupamento ou identificação visual (ex.: campanhas, categorias especiais de atenção).

- Campos: nome e descrição opcional.
- **"Not found"** (404) ao tentar atualizar/excluir uma etiqueta inexistente.

---

## 7. Motivos (Reason)

Um **motivo/justificativa** que pode ser associado a um item de estoque — por exemplo, para documentar por que um item recebeu um tratamento manual ou uma exceção no planejamento.

- Campos: nome e descrição opcional.
- **Motivos "de sistema"**: um motivo pode ser marcado internamente como "de sistema" (protegido) — isso nunca acontece através da criação normal pela interface; um motivo criado por um usuário nunca nasce protegido. Um motivo marcado como "de sistema" **não pode ser editado nem excluído** pela interface.

**Mensagens de erro:**
- **"System reasons cannot be edited."** (403) — ao tentar editar um motivo protegido pelo sistema.
- **"System reasons cannot be deleted."** (403) — ao tentar excluir um motivo protegido pelo sistema.
- **"Not found"** (404) — ao tentar atualizar/excluir um motivo inexistente.

---

## 8. Perfis de Buffer (BufferProfile)

Um **perfil de configuração de buffer DDMRP** — um conjunto padronizado de parâmetros que pode ser aplicado a diversos itens (produto+centro) para definir como o buffer deles se comporta. Não está ligado a nenhum produto ou centro específico — é uma configuração reutilizável (um "modelo" que vários itens podem seguir).

**Principais parâmetros do perfil:**
- **Tipo de suprimento**: se o item é Distribuído, Comprado, Fabricado ou Sob Encomenda (MTO — Make To Order).
- **Categoria de Lead Time** (Curto / Médio / Longo / MTO) e **Categoria de Variabilidade** (Baixa / Média / Alta / MTO): classificações usadas para escolher os fatores multiplicadores sugeridos.
- **Fator de Lead Time** e **Fator de Variabilidade**: multiplicadores usados no cálculo do tamanho das zonas vermelha e amarela — quanto maior o fator, maior o buffer de proteção sugerido.
- **Dias de cálculo de Adu (histórico e futuro)**: janela padrão de dias usada para calcular o consumo médio diário.
- **Frequência**: frequência de reposição do item.
- **Parametrização da zona verde**: três interruptores que definem quais dos três "candidatos" (quantidade mínima de pedido, Adu×LeadTime×Fator, Adu×Frequência) competem para definir o tamanho da zona verde — o maior entre os habilitados vence.
- **Parâmetros de detecção de pico de demanda** (horizonte e limiar): definem quando uma demanda pendente é considerada um "pico" relevante o suficiente para entrar na demanda qualificada do item.
- **Indicador "Ativo"**: um perfil pode ser ativado/desativado através de uma rota própria, sem precisar reenviar todos os outros campos.
- **Indicador "Sob Encomenda" (MTO)**: sinaliza que itens desse perfil são produzidos/atendidos sob encomenda.

**Mensagens de erro:**
- **"Not found"** (404) — ao atualizar, excluir ou ativar/desativar um perfil inexistente.

---

## 9. Buffer Mestre (MasterBuffer)

Estabelece uma **relação hierárquica entre dois itens** (produto+centro) — um item "filho" e um item "pai" de referência, com uma ordem de sequência. Usado para modelar cadeias de hierarquia/replicação entre itens relacionados (ex.: um centro distribuidor que serve de referência para centros satélites).

- Na criação, são informados: produto e centro do item filho, produto e centro do item pai, e a sequência.
- **Depois de criado, só a sequência pode ser alterada** — os quatro vínculos (produto/centro filho, produto/centro pai) ficam fixos para sempre; se algum estiver errado, é necessário excluir o vínculo e criar um novo.

**Mensagens de erro (todas 400, na criação):**
- **"Product not found."** — o produto do item filho não existe.
- **"Center not found."** — o centro do item filho não existe.
- **"Father product not found."** — o produto do item pai não existe.
- **"Father center not found."** — o centro do item pai não existe.
- **"Not found"** (404) — ao atualizar/excluir um vínculo inexistente.

---

## 10. Grupos de Alocação e Alocação Priorizada (AllocationGroup)

### 10.1 Cadastro do grupo

Um **grupo de alocação** é apenas um agrupamento nomeado — cada item de estoque pode, opcionalmente, pertencer a um grupo (essa associação é feita no cadastro do item, não aqui). O grupo em si só tem um nome; ele existe para servir de "conjunto" sobre o qual a alocação priorizada (abaixo) é executada.

- Criar / listar / renomear / excluir grupos.
- **"Not found"** (404) ao atualizar/excluir um grupo inexistente.

### 10.2 Alocação Priorizada — o que é e para que serve

**O problema que resolve:** quando a quantidade disponível para atender vários itens de um grupo é **limitada** (por exemplo: capacidade de um caminhão, orçamento de compra, cota de fornecimento, peso ou volume máximo de um embarque), o sistema precisa decidir **como distribuir essa quantidade limitada entre os itens do grupo**, priorizando quem está em situação mais crítica antes de quem já está bem abastecido — em vez de simplesmente cortar tudo proporcionalmente.

**Consultar o total já aprovado por grupo**: mostra, para cada grupo de alocação, quanto já foi aprovado (pela área de planejamento, no espaço de simulação pessoal de cada usuário — ver [Workspace](#18-workspace--simulação-pessoal)) dos itens desse grupo, convertido para cinco unidades de medida diferentes (unidade, peso, volume, valor monetário e paletes). Se um item do grupo não tiver o atributo físico necessário para alguma dessas conversões (ex.: sem peso cadastrado), aquele total fica indisponível para aquela unidade. Essa consulta apenas relata a situação atual — não redistribui nada.

**Executar a redistribuição:** o usuário informa qual grupo, qual o limite total disponível (`Limit`), em qual unidade de medida esse limite deve ser interpretado (`AdjustmentType`) e até onde a quantidade de um item pode ser reduzida quando é necessário cortar (`StopCondition`). O mecanismo funciona assim:

1. São reunidos todos os itens do grupo que já têm uma quantidade aprovada no espaço de trabalho do usuário atual. Cada item carrega sua quantidade aprovada, sua posição atual em relação à meta do buffer (o quanto mais baixo esse percentual, mais crítico/necessitado o item está), sua quantidade mínima de pedido, seu múltiplo de embalagem e, quando disponíveis, seu peso, volume, valor unitário e fator de palete.
2. Itens sem múltiplo de embalagem válido, ou sem o atributo necessário para a unidade de medida escolhida (por exemplo, rodar em "Peso" para um produto sem peso cadastrado), são automaticamente deixados de fora da redistribuição — permanecem com a quantidade que já tinham.
3. Soma-se a quantidade já aprovada de todos os itens elegíveis, na unidade de medida escolhida, e compara-se com o limite informado:
   - **Se o total aprovado for maior que o limite** (demanda maior que o suprimento disponível): o sistema **reduz** a quantidade dos itens, um múltiplo de embalagem por vez, sempre tirando primeiro do item em **melhor** situação (mais próximo ou acima da sua meta de buffer), até o total caber no limite. Um item para de ser reduzido quando reduzir mais o levaria abaixo do "piso" definido pela condição de parada; se ainda estiver acima da sua quantidade mínima de pedido nesse ponto, é ajustado exatamente para essa quantidade mínima.
   - **Se o total aprovado for menor que o limite** (sobra de suprimento frente à demanda): o sistema **aumenta** a quantidade dos itens, um múltiplo de embalagem por vez, sempre priorizando primeiro o item em **pior** situação (mais distante da sua meta de buffer, mais necessitado), até atingir o limite ou não ser mais possível adicionar mais um pacote sem ultrapassá-lo.
   - Se o total já for exatamente igual ao limite, nada é alterado.
4. A nova quantidade aprovada de cada item é gravada de volta no espaço de trabalho do usuário (só para itens que já tinham um registro lá — itens sem workspace prévio são ignorados na gravação, mas ainda aparecem no resultado devolvido).
5. O resultado retornado mostra a quantidade final aprovada de cada item após a redistribuição, permitindo comparar o "antes e depois".

**Opções de "até onde reduzir" (`StopCondition`):**
- **Zero** — o item pode ser reduzido até zero.
- **Moq** — o item nunca é reduzido abaixo da sua quantidade mínima de pedido.
- **OnePackQuantity** — o item nunca é reduzido abaixo de um múltiplo de embalagem.

**Opções de unidade de medida do limite (`AdjustmentType`):**
- **Unit** (padrão) — o limite é uma quantidade de unidades do produto.
- **Weight** — o limite é um peso total, usando o peso unitário de cada produto.
- **Volume** — o limite é um volume total, usando o volume unitário de cada produto.
- **Value** — o limite é um valor monetário total, usando o valor unitário de cada produto.
- **Pallet** — o limite é uma quantidade de paletes, convertendo pelo fator de palete de cada produto.

**Mensagens de erro:**
- **"Allocation group not found."** (404) — ao tentar executar a alocação priorizada para um grupo que não existe.
- Se não houver nenhum item aprovado no grupo, o sistema simplesmente devolve uma lista vazia (não é tratado como erro).

---

## 11. Produtos (Product)

Representa o **cadastro mestre de produtos/materiais** — o item físico em si (referência/SKU, descrição, unidade de medida, peso, volume, categoria, marca, valor, etc.). É a base sobre a qual os módulos de Centro-Produto, Previsão, Histórico e Pedidos se apoiam.

- **Listar produtos**: pode ser filtrada por centro — nesse caso, mostra apenas os produtos que efetivamente **têm um vínculo ativo com aquele centro** (ou seja, "produtos trabalhados/estocados naquele centro"). Sem esse filtro, lista todos os produtos cadastrados no sistema, independente de qualquer centro.
- Não há checagem de referência/código duplicado.

**Mensagens de erro:**
- **"Center not found."** (404) — ao listar produtos filtrando por um centro que não existe.
- **"Not found"** (404) — ao atualizar/excluir um produto inexistente.

---

## 12. Centro-Produto — o Buffer DDMRP (CenterProduct)

Este é o **módulo central do sistema**: representa o buffer de um produto específico em um centro específico. Concentra os parâmetros de reposição (lead time, MOQ, tamanho de lote), os campos calculados automaticamente pelo Robô (consumo médio, zonas do buffer, demanda qualificada) e as configurações de detecção de picos de demanda.

### 12.1 Vínculos (relações)

- **Obrigatórios**: o produto e o centro do item.
- **Opcionais**: centro de origem (quando o abastecimento vem de outro centro em vez de um fornecedor externo), fornecedor/parceiro, etiqueta, motivo, grupo de alocação, e um perfil de buffer de referência.
- **O produto e o centro nunca podem ser alterados depois de criado o item** — para corrigi-los, é preciso excluir e recriar o registro.

### 12.2 Campos operacionais (editáveis livremente)

- **Quantidade de embalagem (lote de compra)**, **MOQ** (quantidade mínima de pedido), **Lead Time** (tempo de reposição em dias) e **Frequência** de reposição.
- Classificações livres (classe, classificação, segmento).
- **Estoque físico** e **estoque reservado** (não disponível) — o **estoque disponível** é calculado automaticamente como estoque físico menos o reservado.
- **Dias de cálculo do consumo médio** (histórico e futuro) — permite sobrepor, item a item, a janela padrão usada para calcular o Adu (ver [seção 20.1](#201-consumo-médio-diário-adu)).
- **Uso de fator sugerido de Lead Time / Variabilidade**: se ligado (padrão), o item usa o fator sugerido pelo perfil de buffer associado; se desligado, usa um fator personalizado definido manualmente no próprio item.
- **Perfil de buffer travado**: sinaliza que o perfil de buffer deste item não deve ser recalculado/reatribuído automaticamente.
- **Uso do ajuste de demanda na zona verde**: por padrão, um ajuste de demanda (DAF) só afeta as zonas vermelha e amarela; esse interruptor permite que ele também afete a zona verde.
- **Parametrização da zona verde**: três interruptores (usar MOQ / usar Adu×Frequência / usar Adu×Lead Time×Fator) que definem quais candidatos competem para dimensionar a zona verde do item — o maior entre os habilitados é usado (mesma lógica do perfil de buffer, mas configurável por item).
- **Tipo de Buffer** (ver abaixo) — muda completamente a forma como as zonas desse item são calculadas.
- Parâmetros de detecção de pico de demanda (horizonte e limiar) — mesma ideia do perfil de buffer, mas configurável por item.

### 12.3 Campos exclusivos do Robô (somente leitura pela interface)

Estes campos **nunca podem ser digitados pelo usuário** — só são preenchidos pelo cálculo automático diário: consumo médio diário (Adu), intervalo médio de demanda (Adi), desvio padrão e coeficiente de variação do consumo, os tamanhos das três zonas do buffer (vermelha, amarela, verde — com uma exceção, ver 12.4), o delta aplicado por ajustes de zona, e a demanda qualificada. Também existe um conjunto de indicadores **derivados automaticamente** desses valores (nunca digitados, nunca gravados por nenhum processo — são sempre recalculados na hora da consulta): os "topos" acumulados de cada zona (até onde vai o vermelho, até onde vai o amarelo, até onde vai o verde — este último é o **ponto de pedido**), as zonas de "execução" (usadas para acompanhar pedidos em aberto) e as zonas "analíticas" (uma leitura mais granular, distinguindo a parte "segura" da parte de "excesso" de cada zona).

### 12.4 Regra especial — edição manual das zonas do buffer

As quatro medidas de zona (base do vermelho, segurança do vermelho, amarelo, verde) podem, excepcionalmente, ser editadas manualmente — mas **só quando, no mesmo pedido de atualização, o item também está sendo definido como "Buffer Fixo Manual"** (`BufferType = ManualFixed`). Se qualquer uma dessas quatro medidas for enviada enquanto o tipo de buffer continuar sendo outro (Normal, MinMax, MinMax Dinâmico), a atualização inteira é rejeitada.

**Mensagem de erro:** **"RedZoneBase, RedZoneSafe, YellowZone and GreenZone can only be edited when BufferType is ManualFixed."** (400).

### 12.5 Etiquetagem rápida (PATCH dedicados)

Existem três rotas específicas para alterar **apenas** o grupo de alocação, **apenas** a etiqueta, ou **apenas** o motivo de um item, sem precisar reenviar todos os outros campos — e aceitando `null` para simplesmente remover a associação atual.

### 12.6 Mensagens de erro (validação de vínculos, todas 400)

- **"Product not found."** / **"Center not found."** — na criação, quando o produto ou centro informado não existe.
- **"Origin center not found."** — quando o centro de origem informado não existe.
- **"Provider not found."** — quando o fornecedor/parceiro informado não existe.
- **"Tag not found."** — quando a etiqueta informada não existe.
- **"Reason not found."** — quando o motivo informado não existe.
- **"Allocation group not found."** — quando o grupo de alocação informado não existe.
- **"Buffer profile not found."** — quando o perfil de buffer informado não existe.
- **"Not found"** (404) — ao atualizar, excluir ou usar um dos PATCHs de etiquetagem em um item inexistente.

### 12.7 Tipo de Buffer (enum `BufferType`)

- **Normal** — as zonas são recalculadas automaticamente todo dia pela fórmula padrão DDMRP (ver [20.2](#202-zonas-do-buffer)).
- **ManualFixed (Fixo Manual)** — as zonas são valores fixos, definidos manualmente (por este cadastro, quando `BufferType = ManualFixed`, ou por um ajuste de buffer — BAF) e não são recalculadas automaticamente.
- **MinMax** — a zona vermelha é definida como o maior consumo diário observado no histórico recente; não há zona de segurança (amarela é zero).
- **DynamicMinMax (MinMax Dinâmico)** — variante mais reativa do MinMax, baseada em uma janela móvel de consumo acumulado, que se ajusta a picos recentes de demanda.

### 12.8 Detecção de pico de demanda (Spike)

- **Tipo de horizonte** (`Dlt` — baseado no Lead Time, ou `Days` — número fixo de dias): define até quantos dias à frente o sistema olha para identificar demanda "qualificada" (pedidos pendentes considerados relevantes o suficiente para consumir o buffer).
- **Tipo de limiar** (`Adu` — baseado num múltiplo do consumo médio diário, ou `PlanningRedZone` — baseado numa porcentagem da zona vermelha): define a partir de qual volume diário de demanda pendente um dia é considerado um "pico" relevante.

---

## 13. Ajustes de Buffer (BAF, ZAF e DAF)

Estes três módulos permitem fazer **alterações temporárias e programadas**, por período de vigência, no comportamento do buffer de um item — sem precisar mexer manualmente todo dia. Todos os três compartilham a mesma estrutura básica: são vinculados a um produto+centro, têm uma data de início e fim de vigência (que **nunca podem ser alteradas depois de criadas** — só recriando o registro), podem ser ativados/desativados a qualquer momento por uma rota dedicada (sem precisar reenviar todo o cadastro), e **não pode existir mais de um ajuste ativo do mesmo tipo, para o mesmo item, com período sobreposto** (ver detalhe de escopo abaixo em cada um).

### 13.1 BAF — Ajuste de Buffer (`BufferAdjustmentFactor`)

Troca temporariamente **o tipo de buffer inteiro** de um item (por exemplo, para "Fixo Manual" durante uma promoção) e, quando o tipo escolhido é "Fixo Manual", também define diretamente os tamanhos das três zonas nesse período.

**Como funciona a reversão automática (o "voltar ao normal"):**
- Ao **criar** um BAF, o sistema tira automaticamente uma "foto" (snapshot) do estado atual do buffer do item (tipo de buffer e tamanho das quatro zonas) — essa foto fica guardada dentro do próprio BAF, congelada, e nunca é alterada depois. Se o item ainda não existir no momento da criação do BAF, a foto fica vazia (não é erro).
- Quando o **período de vigência termina** (`EffectiveTo` no passado), o Robô automaticamente devolve o buffer do item para o estado registrado na foto — mas só os campos que tinham um valor salvo; um campo que não tinha valor na foto fica intocado.
- A reversão também acontece **imediatamente, fora do robô**, se o usuário **excluir** ou **desativar** manualmente um BAF que estava ativo e dentro do período de vigência.
- Um mesmo BAF nunca reverte duas vezes — uma vez revertido, ele fica marcado como "já revertido". Se o BAF for **reativado**, essa marca é zerada, permitindo que ele reverta de novo numa futura desativação/expiração.
- Importante: a reversão só tem efeito **duradouro** quando o tipo revertido é "Fixo Manual" — se o tipo revertido for Normal/MinMax/MinMax Dinâmico, o próprio Robô recalcula essas zonas do zero no dia seguinte de qualquer forma.

**Mensagens de erro (400, na criação):**
- **"Product not found."** / **"Center not found."**
- **"There is already an active buffer adjustment factor for this product/center in the given period."** — já existe outro BAF ativo, para o mesmo item, com período sobreposto (aqui a checagem não distingue zona — é por item inteiro).
- **"Not found"** (404) ao atualizar/excluir/ativar-desativar um BAF inexistente.

### 13.2 ZAF — Ajuste de Zona (`ZoneAdjustmentFactor`)

Aplica um ajuste temporário a **uma única zona específica** (vermelha, amarela ou verde) do buffer de um item, somando um valor fixo ou um percentual à zona calculada normalmente — por exemplo, para reforçar só a zona verde durante uma campanha, sem mexer nas outras duas.

- Como o ajuste é por zona, **é permitido ter até três ZAFs ativos simultaneamente no mesmo item** (um por zona) — a checagem de sobreposição de período considera a zona: dois ajustes na mesma zona não podem se sobrepor, mas um ajuste na zona vermelha e outro na zona verde podem coexistir no mesmo período.
- O tipo de ajuste pode ser em **valor fixo** ou em **percentual** sobre a zona.

**Mensagens de erro (400, na criação):**
- **"Product not found."** / **"Center not found."**
- **"There is already an active zone adjustment factor for this product/center/zone in the given period."** — já existe outro ZAF ativo, para o mesmo item **e mesma zona**, com período sobreposto.
- **"Not found"** (404) ao atualizar/excluir/ativar-desativar um ZAF inexistente.

### 13.3 DAF — Ajuste de Demanda (`DemandAdjustmentFactor`)

Aplica um ajuste temporário ao **consumo médio diário (Adu)** usado no cálculo do buffer de um item — por exemplo, para antecipar manualmente um aumento ou queda esperada de demanda (sazonalidade, campanha) antes que ela apareça no histórico real de consumo. O ajuste pode ser em valor fixo ou percentual, e afeta por padrão as zonas vermelha e amarela (a zona verde só é afetada se o item tiver essa opção especificamente ligada — ver [12.2](#122-campos-operacionais-editáveis-livremente)).

- Diferente do BAF, **o DAF não tem nenhuma lógica de "foto"/reversão automática** — é apenas um multiplicador/soma aplicado enquanto está ativo e dentro do período; ao excluir/desativar, nada é revertido automaticamente em outro lugar (o próprio recálculo diário do Robô simplesmente deixa de aplicá-lo).

**Mensagens de erro (400, na criação):**
- **"Product not found."** / **"Center not found."**
- **"There is already an active demand adjustment factor for this product/center in the given period."**
- **"Not found"** (404) ao atualizar/excluir/ativar-desativar um DAF inexistente.

---

## 14. Pedidos (Order)

Representa **qualquer movimento de estoque programado ainda em aberto** — pedido de compra, ordem de produção, transferência entre centros ou pedido de venda. É a fonte de dados que alimenta a demanda qualificada e os indicadores de execução do buffer.

### 14.1 Tipos de pedido (`OrderType`)

- **ProductionOrder (Ordem de Produção)** — a quantidade produzida chega no centro de **destino**.
- **Transfer (Transferência)** — movimento entre dois centros (origem e destino).
- **PurchaseOrder (Pedido de Compra)** — compra de um fornecedor.
- **SaleOrder (Pedido de Venda)** — venda a um cliente.

### 14.2 Entrada e saída

Cada pedido tem dois indicadores independentes — se ele representa uma **entrada** de estoque e/ou uma **saída** — além de um indicador de **fictício** (ver abaixo).

### 14.3 Pedidos criados pela interface são sempre "fictícios"

Ao criar um pedido manualmente pela interface, ele é **sempre marcado automaticamente como fictício** — não existe opção de criar um pedido "real" por essa via. Pedidos reais (vindos do sistema do cliente) só entram através do processo automático de carga de dados (ingestão). Na prática: qualquer pedido cadastrado manualmente é tratado como uma simulação de "e se" e nunca é confundido, mesclado ou apagado pela sincronização automática de pedidos reais.

### 14.4 Campos calculados automaticamente

- **Quantidade pendente**: quantidade total do pedido menos quantidade já entregue (nunca fica negativa, mesmo que o pedido tenha sido entregue a mais).
- **Lead Time do pedido**: dias entre a data de criação e a data de entrega prevista (vazio se não houver data de entrega).
- **Dias para receber** / **Dias de atraso**: quantos dias faltam para a entrega prevista, ou quantos dias já se passaram desde que ela venceu sem ter sido concluída.

### 14.5 O que pode ser filtrado na listagem

Centro de destino, centro de origem, produto, se é fictício ou não, se é entrada e/ou se é saída — todos combináveis.

### 14.6 O que pode ser alterado depois de criado

Só os campos operacionais (fornecedor/parceiro, quantidade, quantidade entregue, unidade de medida, posição, data de criação, data de entrega, observações). O número do pedido, o produto, os centros de origem/destino, o tipo e os indicadores de entrada/saída/fictício **ficam fixos para sempre** — para corrigi-los, é preciso excluir e recriar o pedido.

**Mensagens de erro (400, na criação, e também na atualização para o fornecedor):**
- **"Product not found."**
- **"Partner not found."** — na criação, ou na atualização se um novo fornecedor/parceiro for informado (é o único vínculo reeditável).
- **"Destiny center not found."**
- **"Origin center not found."**
- **"Not found"** (404) ao atualizar/excluir um pedido inexistente.

---

## 15. Previsão de Demanda (Forecast)

Representa a **previsão de demanda futura** de um produto em um centro, lançada **por período** (tipicamente um mês, não dia a dia) — usada para projetar o consumo futuro esperado no cálculo do consumo médio diário e do buffer.

### 15.1 Como a previsão é "aberta" dia a dia

Como a previsão é cadastrada por período (um valor total para um intervalo de datas), o sistema sabe "abri-la" automaticamente em uma linha por dia dentro daquele intervalo:
- Cada **dia útil** do intervalo recebe uma fração igual do valor total (valor total dividido pela quantidade de dias úteis do intervalo).
- Cada **dia não útil** também aparece na listagem diária, mas com valor **zero** (não é omitido — apenas não recebe parte do valor).
- A definição de quais datas são dias úteis vem de um calendário próprio do sistema, onde cada data é marcada individualmente como útil ou não (não é derivado automaticamente do dia da semana).

### 15.2 As duas formas de consultar

- **Consulta diária (explodida)**: retorna uma linha por dia, com o valor já fracionado como descrito acima — útil para gráficos de tendência dia a dia.
- **Consulta agrupada**: retorna o registro tal como foi cadastrado (um valor por período/mês, sem fracionamento) — útil para quem quer ver o valor mensal cheio.

### 15.3 O que fica fixo depois de criada

Produto, centro, data de início e data de fim do período **nunca podem ser alterados** depois de criada a previsão — só o valor total pode ser corrigido via atualização. Para corrigir produto/centro/período, é necessário excluir e recriar.

**Mensagens de erro (400, na criação):**
- **"Product not found."** / **"Center not found."**
- **"EndDate must be greater than or equal to StartDate."** — quando a data de fim informada é anterior à data de início.
- **"Not found"** (404) ao atualizar/excluir uma previsão inexistente.

---

## 16. Histórico de Consumo (History)

Representa o **histórico diário de consumo real** de um produto em um centro — a série que alimenta o cálculo do consumo médio (Adu), desvio padrão e coeficiente de variação. Cada registro também guarda, automaticamente, uma **"foto" diária** dos principais indicadores de buffer do item naquele dia (estoque, zonas, consumo médio, demanda qualificada etc.), permitindo consultar a evolução do buffer ao longo do tempo (ver [relatório de histórico de estoque](#212-relatório-histórico-de-estoque-inventoryhistory)).

### 16.1 Fluxo de revisão do consumo (`DiscardStatus`)

Todo registro de consumo nasce com o status **"Não Revisado"**. Um analista pode revisar os lançamentos e marcar cada dia como:
- **NotReviewed (Não Revisado)** — estado inicial, ainda não olhado.
- **Reviewed (Revisado)** — foi conferido e é considerado válido.
- **Discarded (Descartado)** — considerado um valor atípico/erro de lançamento; esse dia é **ignorado** no cálculo do consumo médio (Adu) e do desvio padrão/coeficiente de variação (mas ainda conta normalmente no cálculo do Adi — ver [20.1](#201-consumo-médio-diário-adu) e [20.1-b](#20-1b-intervalo-médio-de-demanda-adi)).

Esse status só pode ser alterado através de uma atualização manual (ou de uma rota dedicada só para ele) — nunca é definido no momento da criação do registro, e o processo automático de carga de dados também nunca mexe nele ao reatualizar o consumo de um dia já existente (evitar apagar uma revisão já feita por um analista).

### 16.2 O que fica fixo

Produto, centro e data **nunca podem ser alterados** depois de criado o registro — só a quantidade consumida e o status de revisão podem mudar.

**Mensagens de erro:**
- **"Product not found."** / **"Center not found."** (400, na criação).
- **"Not found"** (404) ao atualizar/excluir um registro de histórico inexistente, ou ao usar a rota dedicada de status de revisão em um registro inexistente.

---

## 17. Notas (Note)

Anotações/comentários livres associados a um item específico (produto+centro) — um espaço para o time de planejamento registrar observações sobre aquele item.

- Toda nota é vinculada a um item existente e registra automaticamente quem a criou.
- **Só quem criou a nota pode editá-la.** Não há essa mesma restrição para exclusão — qualquer usuário autenticado pode excluir a nota de outra pessoa.
- O item vinculado não pode ser trocado depois de criada a nota — só o conteúdo do texto pode ser editado.

**Mensagens de erro:**
- **"Center product not found."** — ao criar uma nota para um item inexistente, ou ao listar notas filtrando por um item que não existe (404 nesse segundo caso).
- **"Only the user who created this note can edit it."** (403) — ao tentar editar uma nota criada por outro usuário.
- **"Not found"** (404) ao atualizar/excluir uma nota inexistente.

---

## 18. Workspace — Simulação Pessoal

O Workspace é um **espaço de simulação pessoal, por usuário**, onde o planejador pode testar uma quantidade de pedido diferente da sugerida pelo sistema para um item (produto+centro), e ver imediatamente qual seria o efeito no status do buffer — sem alterar nenhum dado real. Não é um cadastro tradicional: existem só duas ações, sem uma tela de "criar" separada — a mesma ação de "salvar" cria ou atualiza o registro conforme ele já exista ou não para aquele usuário+item.

**Como funciona:**
- O usuário informa o item (produto+centro), a quantidade que quer simular, e se essa quantidade deve ser considerada "aprovada" na simulação.
- Se **aprovada**, o sistema soma essa quantidade ao fluxo líquido (Netflow) normal do item, formando um "Netflow simulado", e recalcula, só para essa simulação, qual seria o percentual de ocupação do buffer e qual cor de buffer resultaria — permitindo ao usuário "testar mentalmente" o efeito de um pedido diferente do sugerido antes de efetivamente criá-lo.
- Se **não aprovada**, o registro fica salvo mas sem nenhum efeito na simulação (o Netflow simulado é igual ao normal).
- Cada usuário só enxerga e só pode alterar os próprios registros — a identidade do usuário logado sempre faz parte da chave, nunca é escolhida pelo cliente.
- Existe uma ação para **limpar todos** os registros de workspace do usuário autenticado de uma vez (não recebe um item específico — apaga tudo daquele usuário).

**Regras de negócio:**
- Se o item (produto+centro) ainda não tiver buffer calculado pelo Robô, a simulação simplesmente retorna um resultado neutro (percentual zero, sem cor) — não é tratado como erro.
- Limpar o workspace quando não existe nenhum registro também não é erro — simplesmente não faz nada.

**Mensagens de erro:**
- **"Center not found."** / **"Product not found."** (400) — ao salvar uma simulação para um centro/produto que não existe.

Essa mesma lógica de "quantidade aprovada no workspace do usuário" também é usada como entrada da [Alocação Priorizada](#10-2-alocação-priorizada--o-que-é-e-para-que-serve) e aparece no [relatório de gestão de buffer](#211-relatório-de-gestão-de-buffer-inventorybuffermanagement) como as colunas de quantidade otimizada/aprovada.

---

## 19. O Robô — Ingestão de Dados e Cálculo DDMRP

O "Robô" é o processo que mantém todos os indicadores de buffer atualizados. Ele tem duas metades, que também podem ser disparadas separadamente:

### 19.1 Ingestão (carga de dados)

Carrega dados novos vindos de fontes externas (arquivos do sistema do cliente) para dentro do sistema — centros, produtos, itens produto+centro, previsões, histórico de consumo, pedidos, entre outros. Pode ser disparada **para todas as fontes configuradas de uma vez**, ou **para uma única fonte específica** (por exemplo, "carregar só o histórico de consumo", sem tocar nas outras fontes).

- O resultado indica, por fonte processada: quantas linhas foram lidas, quantas foram inseridas, quantas atualizadas, quantas removidas (quando aplicável) e quantas tiveram erro — com a lista detalhada de erros, identificados pelas próprias informações de negócio da linha (não por número de linha do arquivo).
- **Um erro em uma linha não interrompe o processamento do arquivo inteiro** — apenas aquela linha é reportada como erro; as demais continuam sendo processadas normalmente.
- Praticamente todo problema de configuração ou de arquivo de origem (arquivo de configuração ausente, fonte de dados não reconhecida, coluna de referência mal configurada) é tratado como **erro de requisição (400)** — nunca como falha interna do sistema — porque são sempre causados por configuração/arquivo, não por um defeito do sistema em si.

### 19.2 Cálculo (recálculo dos indicadores DDMRP)

Recalcula, em sequência, **todos os indicadores de buffer** (consumo médio, intervalo médio de demanda, zonas do buffer, ajustes, demanda qualificada, entre outros — ver [seção 20](#20-fórmulas-e-indicadores-calculados)) de todos os itens ativos, ou — se informado um item específico — **apenas daquele item** (um "recálculo pontual", útil depois de editar algo manualmente em um item e querer ver o efeito imediatamente, sem esperar o próximo ciclo completo).

- As etapas do cálculo são sempre executadas **na ordem certa** (cada etapa pode depender do que a anterior calculou) e **sempre todas** — não existe opção de rodar só uma etapa isolada.
- **Se uma etapa falhar, o processo para naquele ponto** — as etapas seguintes não são executadas, porque continuar calcularia em cima de dados que ainda não foram atualizados corretamente.

**Mensagem de erro:**
- **"CenterProduct not found."** (404) — ao pedir um recálculo pontual informando um item que não existe.

### 19.3 Disparo completo (o Robô "de verdade")

Existe uma terceira ação que combina as duas anteriores na ordem certa e garantida: primeiro roda a **ingestão completa** (todas as fontes), e **só depois** — e só se a ingestão não tiver falhado — roda o **cálculo completo** (todos os itens). Essa é a forma "oficial" de rodar o Robô do início ao fim. Se a ingestão falhar, o cálculo **nunca chega a ser executado**.

---

## 20. Fórmulas e Indicadores Calculados

Esta seção explica, em termos de negócio, como cada indicador automático é calculado pelo Robô. Nenhum destes valores pode ser digitado manualmente — todos são resultado do cálculo diário (com a única exceção do "Buffer Fixo Manual", ver [12.4](#124-regra-especial--edição-manual-das-zonas-do-buffer)).

### 20.1 Consumo médio diário (Adu)

O "Adu" é quantas unidades de um item são, em média, consumidas por dia. Cada item pode ser configurado para usar uma das três fórmulas, dependendo de quantos "dias de histórico" e quantos "dias futuros" estão configurados nele:

- **Histórico** (só dias de histórico configurados): soma o consumo real dos últimos N dias "válidos" (não descartados na revisão) e divide sempre por N — mesmo que nem todos os N dias tenham dado disponível (item muito novo, por exemplo). Um dia marcado como "descartado" na revisão não é contado — a janela simplesmente "empurra" mais um dia para trás no histórico até completar N dias válidos. Se não existir nenhum dado válido, o resultado é zero (nunca fica vazio).
- **Futuro** (só dias futuros configurados): usa a previsão de demanda dos próximos N dias corridos (a partir de amanhã), já fracionada dia a dia como explicado na seção de [Previsão de Demanda](#151-como-a-previsão-é-aberta-dia-a-dia) — dias não úteis contam como zero, mas o divisor continua sendo sempre N dias corridos (não só os úteis).
- **Misto** (ambos configurados): a média simples entre o valor do Histórico e o valor do Futuro.
- Se nenhum dos dois estiver configurado no item, o consumo médio fica em branco (nada a calcular).

### 20.1-b Intervalo médio de demanda (Adi)

Mede o quão intermitente/espaçada é a demanda de um item: quanto maior o Adi, mais irregulares são as ocorrências de consumo. É calculado dividindo a quantidade de dias com registro de histórico, dentro de uma janela de dias fixa (a mesma para todos os itens, configurável no processo de cálculo, por padrão 360 dias), pela quantidade desses dias que teve consumo maior que zero. **Diferente do consumo médio, aqui os dias marcados como "descartados" continuam contando normalmente** — não há esse filtro. Se não houver nenhum dia com consumo no período, o resultado é zero.

**Desvio padrão e coeficiente de variação do consumo**: calculados junto com o Adu (usando a mesma janela de dias válidos do "Histórico"), medem o quanto o consumo diário varia em torno da média — quanto maior, mais irregular é a demanda do item.

### 20.2 Zonas do buffer

O tamanho das três zonas do buffer (vermelha, amarela, verde) depende do **tipo de buffer** configurado no item (ver [12.7](#127-tipo-de-buffer-enum-buffertype)):

- **Normal**:
  - A **zona amarela** é o consumo médio diário (já ajustado por eventual ajuste de demanda ativo, ver [13.3](#133-daf--ajuste-de-demanda-demandadjustmentfactor)) multiplicado pelo lead time do item — ou seja, cobre o consumo esperado durante o tempo de reposição.
  - A **zona verde** é o maior valor entre até três "candidatos" habilitados no item/perfil: a quantidade mínima de pedido, o consumo médio × lead time × fator de lead time, e a frequência de reposição × consumo médio (ajustado ou não pelo ajuste de demanda, conforme configuração). Um candidato desabilitado no item simplesmente não participa da comparação.
  - A **zona vermelha** é dividida em duas partes: a parte de "segurança" (consumo médio ajustado × lead time × fator de lead time) e a parte "base" (a mesma coisa, multiplicada também pelo fator de variabilidade).
  - Nenhuma zona pode ficar negativa — o menor valor possível é sempre zero.
- **Fixo Manual**: as zonas não são recalculadas automaticamente — usam os valores definidos manualmente (pelo cadastro do item ou por um BAF).
- **MinMax**: a zona amarela é sempre zero (sem faixa intermediária) e não há parte de "segurança" na zona vermelha; a zona vermelha (base) é simplesmente o **maior consumo diário já observado** dentro de uma janela de dias recente (configurável, padrão 180 dias) — um único dia de pico define o tamanho da zona vermelha. A zona verde usa a mesma fórmula do tipo Normal.
- **MinMax Dinâmico**: mais reativo a picos recentes — o sistema analisa uma janela móvel de dias (do tamanho do maior valor entre o lead time e a frequência do item) e encontra qual foi o **maior consumo acumulado** observado nessa janela dentro do histórico recente do item. A zona amarela é o consumo médio × lead time; a zona verde é a quantidade mínima de pedido; a zona vermelha é esse "maior acumulado" menos o consumo esperado durante o lead time (nunca menor que zero). **Este tipo não sofre efeito de nenhum ajuste de demanda (DAF)** — usa sempre o consumo médio "puro".

### 20.3 Indicadores derivados das zonas (sempre calculados na hora, nunca gravados)

A partir das quatro medidas físicas de zona (base do vermelho, segurança do vermelho, amarelo, verde), o sistema sempre deriva na hora:

- **Topo de cada zona**: até onde vai o vermelho (soma das duas partes do vermelho), até onde vai o amarelo (vermelho + amarelo) e até onde vai o verde (vermelho + amarelo + verde — este é o **ponto de pedido**, o limite que, se a posição projetada de estoque cair nele ou abaixo, indica que é hora de repor).
- **Zonas de "execução"**: uma segunda leitura, baseada em metade da zona vermelha e no valor puro da zona amarela — usada para avaliar a situação de pedidos já em andamento frente ao estoque físico (não à posição projetada).
- **Zonas "analíticas"**: uma terceira leitura, mais granular, que separa a parte "segura" e a parte de "excesso" de cada zona — ainda não alimenta nenhuma regra automática, reservada para uma leitura de relatório futura.

### 20.4 Ajuste de zona (ZAF) aplicado

Quando um ajuste de zona (ver [13.2](#132-zaf--ajuste-de-zona-zoneadjustmentfactor)) está ativo e vigente para uma zona específica de um item, o sistema calcula o "delta" (valor fixo ou percentual sobre a zona) e **soma** esse delta na zona correspondente já calculada — o ajuste de vermelho é somado especificamente na parte "base" do vermelho, nunca na parte de "segurança".

### 20.5 Fluxo líquido (Netflow)

O **Netflow** é a posição de estoque "projetada" de um item — o número que realmente diz se ele precisa de reposição: **estoque + entradas pendentes − demanda já qualificada**. É a principal métrica usada para classificar a cor do buffer (abaixo).

**Demanda qualificada**: soma, entre os pedidos de saída ainda pendentes, apenas os dias em que a quantidade pendente daquele dia ultrapassa um limiar configurado no item (baseado num múltiplo do consumo médio, ou numa porcentagem da zona vermelha), dentro de um horizonte de dias à frente também configurável. Ou seja: nem toda demanda futura pendente entra no cálculo — só a que é grande o suficiente para ser considerada relevante ("pico" qualificado). O dia de hoje é tratado como um caso especial que sempre acumula qualquer atraso e a própria demanda do dia.

**Quantidade sugerida de pedido**: quando o fluxo líquido está abaixo do topo da zona amarela, o sistema sugere pedir a diferença entre o topo da zona verde (o ponto de pedido) e o fluxo líquido atual; caso contrário, não sugere pedido algum (zero). Essa quantidade sugerida ainda é ajustada para baixo, ao múltiplo de embalagem mais próximo, e zerada se ficar abaixo da quantidade mínima de pedido.

### 20.6 Cor do buffer (`BufferColor`)

Classifica, de forma simples e visual, a situação de uma quantidade (tipicamente o Netflow, mas a mesma classificação é usada também para o estoque físico em outras leituras) frente às três zonas do buffer:

- **Sem Cor** — o buffer desse item ainda não foi calculado (nenhuma zona definida).
- **Preto** — ruptura: a quantidade está zerada ou negativa.
- **Vermelho** — dentro da zona vermelha (situação crítica, reposição urgente).
- **Amarelo** — dentro da zona amarela (situação de atenção).
- **Verde** — dentro da zona verde (situação saudável).
- **Azul** — acima do topo da zona verde (excesso de estoque).

Existe ainda uma leitura mais granular, a **cor analítica**, que distingue a parte "segura" da parte de "excesso" de cada uma das zonas vermelha e amarela (ex.: "Vermelho Seguro" vs. "Excesso de Vermelho"), além das mesmas classificações de ruptura/excesso/sem cor.

### 20.7 Dias de cobertura de estoque

Quantos dias o estoque físico atual do item duraria, no ritmo do consumo médio diário — calculado como estoque disponível dividido pelo consumo médio (zero quando o consumo médio ainda não foi calculado, para não gerar um resultado infinito/indefinido).

### 20.8 Indicadores de prazo de pedidos (`TimeBuffer`, dias para receber, dias de atraso)

Para cada pedido de entrada com data de entrega prevista, o sistema calcula o quanto do "tempo de reposição" já se passou, como um percentual — e classifica isso numa cor (mesmas cinco faixas: sem cor, verde, amarelo, vermelho, preto — preto quando o percentual passa de 100%, ou seja, o pedido já deveria ter chegado há mais tempo do que o próprio lead time original previa). Também são calculados, para todo pedido: quantos dias faltam para a entrega prevista, e quantos dias de atraso já se acumularam (ambos zero quando não se aplicam — sem data de entrega, ou pedido ainda não vencido).

### 20.9 Buffer de execução por pedido (`ExecutionBuffer`)

Usado no relatório de pedidos em aberto ([21.3](#213-relatório-de-pedidos-em-aberto-openordersinbounds)): para cada pedido de entrada, soma o estoque físico do item a todos os pedidos de entrada **anteriores** a ele (mesmo produto e centro de destino, criados antes e com entrega prevista até a mesma data), e divide pelo topo da zona amarela de execução — dando uma ideia de "quanto de buffer de execução já estaria coberto até este pedido específico chegar".

---

## 21. Relatórios

Os relatórios não alteram nenhum dado — são apenas consultas que combinam e resumem as informações dos módulos acima.

### 21.1 Relatório de gestão de buffer (`inventoryBufferManagement`)

O relatório principal do sistema: **uma linha por item** (produto+centro), reunindo tudo o que se sabe sobre ele — dados cadastrais, todas as zonas e seus topos (normal, execução, analítica), consumo médio, intervalo médio de demanda, netflow, cor do buffer (nas três leituras), quantidade sugerida de pedido, entradas e saídas pendentes (separando pedidos reais de fictícios), dias de cobertura, e a quantidade "simulada" do workspace pessoal do usuário (ver [seção 18](#18-workspace--simulação-pessoal)) quando existir.

Esse relatório suporta filtros e ordenação bem flexíveis: é possível **filtrar por qualquer um dos campos exibidos** (por exemplo, "só itens com cor de buffer vermelha", "só itens com estoque abaixo de determinado valor", "só de um determinado produto"), **ordenar por qualquer campo**, e **paginar** os resultados (até 500 registros por página) com a opção de saber o total de registros que atendem ao filtro. Também é possível restringir o relatório a uma lista específica de centros antes de qualquer outro filtro — essa restrição de centros é sempre aplicada primeiro, então filtrar por um centro que não esteja nessa lista sempre resulta em nada.

### 21.1-b Resumo por cor (`inventoryBufferManagement/colorSummary`)

Uma versão resumida do relatório acima: em vez de uma linha por item, devolve **quantos itens estão em cada cor do buffer**, agora sob três perspectivas ao mesmo tempo (pela cor do netflow, pela cor de execução, e pela cor analítica) — pensado para alimentar um painel/dashboard com gráfico de rosca ou barras mostrando "quantos itens estão em cada situação agora". Aceita os mesmos filtros de campo e a mesma restrição por lista de centros do relatório principal, mas não faz sentido ordenar nem paginar uma contagem agrupada, então essas opções não se aplicam aqui.

### 21.2 Relatório de projeção de estoque (`projectedStockAlert`)

Diferente do relatório principal (que mostra o estado atual), este **simula, dia a dia, um período futuro** para um único item, mostrando qual seria o estoque projetado se nada mudar além do que já está agendado (previsão de demanda, pedidos já pendentes). A cada dia, o estoque de abertura é o estoque de fechamento do dia anterior; o consumo do dia é o maior valor entre o consumo médio, os pedidos de saída programados e a previsão de demanda daquele dia (cada um desses três componentes pode ser individualmente ligado/desligado na consulta); o estoque de fechamento soma as entradas do dia e subtrai esse consumo. Também é possível configurar se pedidos atrasados (entrada ou saída) devem ser "empurrados" para o dia de hoje na simulação, e se pedidos fictícios devem ou não entrar nas somas (este é o único relatório que considera pedidos fictícios por padrão).

### 21.3 Relatório de pedidos em aberto (`openOrders/inbounds`)

Lista **pedidos de entrada ainda não totalmente entregues** (sempre excluindo pedidos fictícios — este relatório nunca os considera), podendo ser filtrado por centro de destino e/ou produto. Para cada pedido, mostra a quantidade ainda pendente, o prazo (dias para receber/dias de atraso), o indicador de "buffer de execução" (quanto do buffer de execução já estaria coberto contando esse pedido e todos os anteriores a ele) e a cor correspondente a essa situação.

### 21.4 Relatório histórico de estoque (`inventoryHistory`)

Mostra, para um único item (produto+centro) e um intervalo de datas, a evolução diária dos principais indicadores registrados pelo histórico (estoque, demanda qualificada, entradas em aberto, consumo, zonas do buffer daquele dia) — útil para gráficos de tendência. Como o dia de hoje só ganha um registro de histórico depois que o Robô rodar naquele dia, o relatório **completa automaticamente uma linha extra para "hoje"**, montada em tempo real a partir do estado atual do item, para não deixar um buraco no gráfico no dia corrente.

### 21.5 Relatório de penetração de buffer (`bufferPenetration`)

Para todos os itens que tiveram histórico registrado num intervalo de datas (podendo ser filtrado por um ou mais centros e/ou um produto), conta, **por item**, quantos dias ele passou em cada cor do buffer dentro do período — respondendo perguntas como "esse item passou quantos dias em ruptura no último trimestre?". Pode ser calculado sob duas perspectivas (à escolha de quem consulta): pela cor do netflow (a leitura "clássica" de saúde do buffer) ou pela cor de execução (olhando o estoque físico contra as zonas de execução). Também soma, à parte, quantos dias o item passou "em vermelho ou em ruptura" combinados.

### 21.6 Relatório de itens por cor ao longo do tempo (`itemsByBufferColorHistory`)

O inverso do relatório anterior: em vez de contar dias por item, conta, **por dia**, quantos itens estavam em cada cor do buffer — pensado para um gráfico de tendência mostrando a evolução da carteira inteira (quantos itens em vermelho, amarelo, verde etc. em cada dia do período). Sempre devolve as duas perspectivas (netflow e execução) juntas, uma lista de dias para cada uma.

### 21.7 Relatório de histórico acumulado de buffer (`accumulatedBufferHistory`)

Soma, dia a dia, os valores de zona e de estoque de **todos os itens qualificados** (que já têm zona vermelha calculada) dos centros informados (aqui a lista de centros é obrigatória) — sem filtro de produto, sempre a carteira inteira somada. Mostra, por dia, o total acumulado de cada zona (netflow, execução e analítica), o estoque disponível total, o fluxo líquido total, o excesso de estoque total (com e sem considerar a zona amarela) e a faixa de oscilação mínima/máxima esperada do estoque agregado — uma visão "de cima", da saúde geral do buffer da operação ao longo do tempo, sem detalhar item a item.

---

## 22. Resumo — o que cada mensagem de erro geral significa

Estas mensagens não são específicas de um módulo — podem aparecer em qualquer um deles:

- **"Not found"** — o registro que você tentou editar, excluir, ativar/desativar ou consultar por id não existe (nunca existiu, ou já foi excluído).
- **"&lt;Entidade&gt; not found."** (ex.: "Product not found.", "Center not found.", "Partner not found.") — um dos vínculos informados (produto, centro, parceiro, etiqueta, motivo, grupo de alocação, perfil de buffer etc.) não corresponde a um registro existente. Sempre aparece ao criar ou, quando o campo é editável, ao atualizar um registro.
- **"There is already an active … in the given period."** — já existe outro ajuste do mesmo tipo, ativo, com período sobreposto para o mesmo item (e, no caso do ajuste de zona, também para a mesma zona). Resolva desativando/excluindo o ajuste anterior, ou ajustando o período do novo.
- Mensagens de permissão (403) — a ação é proibida para o usuário atual mesmo estando autenticado (ex.: editar uma nota de outra pessoa, editar/excluir um motivo protegido pelo sistema).
