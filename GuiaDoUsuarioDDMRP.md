# Guia do Usuário — Sistema DDMRP

Este documento explica, em linguagem de negócio, como usar o sistema de planejamento e reposição de estoque baseado em DDMRP: quais telas existem, o que cada uma faz, quais regras cada uma aplica, como os principais indicadores são calculados, o que significam as mensagens que podem aparecer na tela, e como os termos do sistema se traduzem entre Português, Inglês e Espanhol. Ele foi escrito para responder dúvidas de quem usa o sistema no dia a dia — não é um documento técnico.

---

## 1. O que é o sistema

O sistema ajuda a equipe de planejamento a decidir, para cada combinação de **produto + centro** (uma filial, depósito ou unidade onde o produto é estocado — esse par também é chamado de **"item"** ao longo deste guia):

1. **Quanto de estoque de proteção (buffer) esse item deveria ter**, dividido em três faixas: Vermelha, Amarela e Verde.
2. **Qual é a situação atual do item** frente a esse buffer — ruptura, alerta, saudável ou excesso.
3. **Quanto e quando pedir mais.**

Esses números são recalculados automaticamente todos os dias por um processo interno (chamado de "Robô" pela equipe técnica): primeiro os dados mais recentes são carregados a partir dos sistemas de origem, depois todos os indicadores de buffer são recalculados. Por isso, dados editados manualmente num item (ex.: lead time, MOQ) só têm efeito completo sobre os indicadores calculados (zonas, ADU, cor do buffer) a partir do próximo recálculo — a tela de **Gestão de Buffer de Estoque** é onde esse recálculo aparece refletido.

### 1.1 Glossário essencial

| Termo | Significado |
|---|---|
| **Buffer** | O "colchão" de estoque de proteção de um item, dividido em três zonas: Vermelha (crítica/segurança), Amarela (cobre o consumo durante o tempo de reposição) e Verde (define o tamanho do lote de pedido). O topo da zona verde é o **ponto de pedido**. |
| **Netflow (Fluxo Líquido)** | A posição de estoque projetada de um item: estoque físico + entradas pendentes − demanda já qualificada. É a métrica principal para decidir se o item precisa de reposição. |
| **ADU** | Consumo médio diário — quantas unidades do item são consumidas, em média, por dia. |
| **ADI** | Intervalo médio de demanda — mede o quão intermitente/irregular é a demanda de um item; quanto maior, mais espaçado o consumo. |
| **Lead Time** | Tempo de reposição, em dias, entre pedir e receber o item. |
| **MOQ** | Quantidade mínima de pedido. |
| **Cor do Buffer** | Classificação visual (Vermelho, Amarelo, Verde, Azul, Preto, Sem Cor) da situação de um item frente ao seu buffer. |
| **BAF / ZAF / DAF** | Os três tipos de ajuste manual temporário aplicáveis a um item: BAF troca o tipo/tamanho do buffer inteiro, ZAF ajusta uma zona específica, DAF ajusta a demanda média. |
| **Pedido/Ordem Fictício(a)** | Pedido criado manualmente na tela, usado só para simulação ("e se eu pedisse X?") — nunca é um pedido real nem é tocado pela carga automática de dados. |
| **Workspace (Espaço de Trabalho)** | Simulação pessoal, por usuário, de uma quantidade de pedido diferente da sugerida, sem alterar nada de verdade. |
| **Robô** | Processo automático diário que carrega dados novos e recalcula todos os indicadores de buffer. |

### 1.2 Como interpretar mensagens de erro

Sempre que uma ação não pode ser concluída, o sistema mostra uma mensagem explicando o motivo. Essas mensagens caem, de forma geral, em quatro categorias:

- **Dado inválido ou regra de negócio violada** — a mensagem explica exatamente o que está errado (ex.: "Product not found.", período sobreposto, campo obrigatório faltando).
- **Registro não encontrado** — o que você está tentando editar/excluir/ver já não existe mais (foi excluído, ou nunca existiu). Mensagem padrão: **"Not found"**.
- **Sem permissão** — a ação exige estar logado, ou o usuário logado não pode fazer especificamente aquilo (ex.: editar a nota de outra pessoa, editar um motivo protegido pelo sistema).
- **Falha de comunicação/sistema fora do ar** — erro genérico exibido quando o sistema não consegue completar a operação por um problema interno; nesse caso a tela mostra uma mensagem genérica, nunca o detalhe técnico do erro.

A seção [9. Mensagens e situações de erro](#9-mensagens-e-situações-de-erro) traz a lista completa por tela.

---

## 2. Mapa das telas do sistema

| Grupo no menu | Tela | O que faz |
|---|---|---|
| — | **Entrar (Login)** | Acesso ao sistema. |
| — | **Dashboard** | Página inicial. |
| **Mestres** | Centros | Cadastro de centros/filiais/depósitos. |
| **Mestres** | Tags | Cadastro de etiquetas livres para classificar itens. |
| **Mestres** | Grupos de Alocação | Cadastro de grupos usados na Alocação Priorizada. |
| **Mestres** | Motivos | Cadastro de motivos/justificativas associáveis a um item. |
| **Mestres** | Clientes/Fornecedores | Cadastro de parceiros comerciais. |
| **Mestres** | Perfis de Buffer | Cadastro de configurações-modelo de buffer, reutilizáveis entre itens. |
| **Ajustes Planejados** | Ajuste de Demanda (DAF) | Ajustes temporários e programados ao consumo médio de um item. |
| **Ajustes Planejados** | Ajuste de Buffer (BAF) | Troca temporária e programada do tipo de buffer inteiro de um item. |
| **Ajustes Planejados** | Ajuste de Zona (ZAF) | Ajuste temporário e programado a uma única zona de um item. |
| **Ajustes Planejados** | Ordens Fictícias | Pedidos criados manualmente para simulação. |
| — | **Gestão de Buffer de Estoque** | Tela principal do sistema — uma linha por item, com todos os indicadores de buffer, e um detalhe expansível por item com várias sub-abas (ver [seção 6](#6-gestão-de-buffer-de-estoque-a-tela-principal)). |
| **Melhoria Contínua** | Penetração de Buffer | Quantos dias cada item passou em cada cor de buffer num período. |
| **Melhoria Contínua** | Cores de Buffer por Dia | Quantos itens estavam em cada cor de buffer, por dia, ao longo de um período. |
| **Melhoria Contínua** | Acumulado de Buffers | Soma diária dos indicadores de buffer de toda a carteira de itens de um conjunto de centros. |
| **Segurança** | Usuários | Cadastro de usuários do sistema. |
| **Segurança** | Perfis de Acesso | Cadastro de perfis (papéis) atribuíveis a um usuário. |
| — | **Assistente** | Chat de apoio para tirar dúvidas sobre o sistema. |

---

## 3. Autenticação, Usuários e Perfis de Acesso

**Tela: Entrar.** Login é feito com e-mail e senha. Não existe cadastro público — um usuário só é criado por alguém que já está autenticado dentro do sistema (tipicamente um administrador). A sessão expira depois de um tempo e é renovada automaticamente em segundo plano; se a renovação falhar, o sistema desconecta o usuário e volta para a tela de login.

**Tela: Usuários (menu Segurança).**
- Campos: nome, e-mail, perfil de acesso e senha (na criação).
- O e-mail deve ser único no sistema.
- Todo usuário precisa de um perfil de acesso válido.
- A senha precisa atender a um critério mínimo de força (a tela mostra o nível: Fraca, Moderada, Forte, Muito forte — é exigido pelo menos "Forte").
- Não existe, nesta tela, uma opção separada de "trocar senha" de um usuário já existente — a atualização cobre nome, e-mail e perfil de acesso.
- Excluir um usuário é uma exclusão lógica (o usuário some da lista, mas o registro não é apagado fisicamente).

**Tela: Perfis de Acesso (menu Segurança).**
- Cadastro simples: só o nome é obrigatório.
- É permitido cadastrar dois perfis com o mesmo nome — não há checagem de duplicidade.

### Situações comuns
- **E-mail já em uso** ao tentar criar/editar um usuário com um e-mail que já pertence a outro usuário.
- **Perfil de acesso não encontrado** ao selecionar um perfil que não existe mais (foi excluído por outra pessoa) — atualize a lista e escolha novamente.
- **Credenciais inválidas** no login — mensagem sempre igual tanto para e-mail inexistente quanto para senha incorreta, por segurança (não revela qual dos dois está errado).
- Ao tentar renovar ou encerrar uma sessão que já não é mais válida (expirada, ou já usada antes), a mensagem é sempre genérica, por segurança.

---

## 4. Telas de Cadastros Mestres

Estas telas armazenam informações de apoio, usadas por praticamente todos os outros módulos.

### 4.1 Centros
Representa uma filial, depósito ou unidade física onde o estoque é mantido. Campos: código (também usado para reconhecer o centro nas cargas automáticas de dados), descrição, cidade e zona/região (opcionais). Todos os campos podem ser alterados a qualquer momento.

### 4.2 Clientes/Fornecedores (Partners)
Representa um parceiro comercial — fornecedor ou transportador — que pode ser vinculado a um item (como fornecedor padrão) ou a um pedido. Campos: código (também usado na carga automática de dados) e descrição. Editáveis a qualquer momento.

### 4.3 Tags
Uma marcação/classificação livre que pode ser associada a um item, para agrupamento ou identificação visual (ex.: campanhas, categorias de atenção especial). Campos: nome e descrição.

### 4.4 Motivos (Reasons)
Uma justificativa que pode ser associada a um item — por exemplo, para documentar por que ele recebeu um tratamento manual ou uma exceção de planejamento. Campos: nome e descrição.
- Um motivo pode ser marcado como **"de sistema"** — isso nunca acontece pela tela de cadastro normal; um motivo criado pelo usuário nunca nasce protegido.
- Um motivo "de sistema" **não pode ser editado nem excluído**. A tela mostra um aviso ("Motivos do sistema não podem ser editados ou excluídos") e desabilita as ações.

### 4.5 Grupos de Alocação
Um agrupamento nomeado de itens, usado como base para a **Alocação Priorizada** (ver [seção 5](#5-alocação-priorizada)). O grupo em si só tem um nome — a associação de um item a um grupo é feita na tela de Gestão de Buffer de Estoque, não aqui.

### 4.6 Perfis de Buffer
Um conjunto padronizado de parâmetros DDMRP que pode ser aplicado a vários itens de uma vez, como um "modelo" reutilizável. Não é vinculado a nenhum produto ou centro específico. Principais campos:

- **Tipo de suprimento**: Distribuído, Comprado, Fabricado ou Sob Encomenda (MTO).
- **Categoria de Lead Time** (Curto / Médio / Longo / MTO) e **Categoria de Variabilidade** (Baixa / Média / Alta / MTO) — classificações que ajudam a escolher os fatores multiplicadores.
- **Fator de Lead Time** e **Fator de Variabilidade** — multiplicadores usados no cálculo do tamanho das zonas Vermelha e Amarela; quanto maior o fator, maior o buffer de proteção sugerido.
- **Dias de cálculo de ADU** (histórico e futuro) — janela padrão de dias usada para calcular o consumo médio.
- **Frequência** de reposição.
- **Parametrização da zona verde** — três interruptores que definem quais candidatos (MOQ, ADU×Lead Time×Fator, ADU×Frequência) competem para formar o tamanho da zona verde; o maior entre os habilitados vence.
- **Parâmetros de detecção de pico de demanda** (horizonte e limiar).
- Indicador **Ativo** — pode ser ligado/desligado sem reenviar todo o cadastro.
- Indicador **Sob Encomenda (MTO)**.

### Situações comuns (cadastros mestres)
- **"Não encontrado"** ao tentar editar/excluir um registro que já não existe.
- Motivos "de sistema" bloqueiam edição/exclusão com um aviso próprio.
- Nenhum destes cadastros checa nome/código duplicado, exceto e-mail de usuário (ver seção 3).

---

## 5. Alocação Priorizada

**Onde encontrar:** botão "Alocação Priorizada" dentro da tela de **Gestão de Buffer de Estoque**, agindo sobre um Grupo de Alocação.

**O problema que resolve:** quando a quantidade disponível para atender vários itens de um grupo é limitada — capacidade de um caminhão, orçamento de compra, cota de fornecimento, peso ou volume máximo de um embarque — o sistema decide como distribuir essa quantidade entre os itens do grupo, priorizando quem está em situação mais crítica, em vez de cortar tudo proporcionalmente.

**Como usar:** escolha o grupo, informe o **Limite** disponível, a **unidade de medida** desse limite (**Tipo de Ajuste**) e até onde a quantidade de um item pode ser reduzida quando é preciso cortar (**Condição de Parada**).

**Unidades de medida do limite (Tipo de Ajuste):**
- **Unidade** (padrão) — quantidade de unidades do produto.
- **Peso** — peso total, usando o peso unitário de cada produto.
- **Volume** — volume total, usando o volume unitário de cada produto.
- **Valor** — valor monetário total, usando o valor unitário de cada produto.
- **Pallet** — quantidade de paletes, convertendo pelo fator de palete de cada produto.

**Condição de parada (até onde reduzir):**
- **Zero** — o item pode ser reduzido até zero.
- **MOQ** — nunca reduz abaixo da quantidade mínima de pedido.
- **Uma Embalagem** — nunca reduz abaixo de um múltiplo de embalagem.

**Como funciona:**
1. São reunidos os itens do grupo que já têm uma quantidade **aprovada** no espaço de trabalho (workspace) do usuário atual.
2. Itens sem múltiplo de embalagem válido, ou sem o atributo necessário para a unidade escolhida (ex.: rodar em "Peso" sem o produto ter peso cadastrado), ficam automaticamente **fora** da redistribuição — mantêm a quantidade que já tinham.
3. Soma-se a quantidade já aprovada de todos os itens elegíveis e compara-se com o limite:
   - **Total aprovado maior que o limite** → o sistema **reduz**, um múltiplo de embalagem por vez, sempre tirando primeiro do item em **melhor** situação (mais perto ou acima da meta de buffer), até caber no limite ou até bater no piso definido pela condição de parada.
   - **Total aprovado menor que o limite** → o sistema **aumenta**, um múltiplo de embalagem por vez, sempre priorizando o item em **pior** situação (mais distante da meta, mais necessitado), até atingir o limite.
   - Se já for exatamente igual, nada muda.
4. A nova quantidade aprovada de cada item é salva de volta no espaço de trabalho do usuário. O resultado mostra a quantidade final de cada item, permitindo comparar antes/depois.

**Situações comuns:**
- **Grupo de alocação não encontrado** — o grupo informado não existe.
- Se não houver nenhum item aprovado no grupo, o sistema simplesmente devolve uma lista vazia (não é um erro).

---

## 6. Gestão de Buffer de Estoque — a tela principal

Esta é a tela central do sistema: **uma linha por item** (produto + centro), reunindo tudo o que se sabe sobre ele.

### 6.1 O que a linha principal mostra
Dados cadastrais do item, todas as zonas de buffer e seus topos (leitura normal, de execução e analítica), consumo médio (ADU), intervalo médio de demanda (ADI), netflow, cor do buffer (nas três leituras — netflow, execução e analítica), quantidade sugerida de pedido, entradas e saídas pendentes (separando pedidos reais de fictícios), dias de cobertura, e a quantidade simulada no espaço de trabalho pessoal do usuário, quando existir.

A tela permite **filtrar por qualquer coluna exibida** (ex.: "só itens com cor de buffer Vermelha", "só itens com estoque abaixo de X", "só de um produto específico"), **ordenar por qualquer coluna**, e restringir o relatório a uma **lista específica de centros** — essa restrição é sempre aplicada primeiro, então filtrar por um centro fora dessa lista sempre resulta em nada. É possível também **paginar** os resultados (até 500 por página).

### 6.2 Resumo por cor ("Detalhes")
Um painel resumido, ao lado/acima da tabela, mostrando **quantos itens estão em cada cor do buffer**, sob três perspectivas simultâneas: pela cor do netflow (leitura "clássica"), pela cor de execução, e pela cor analítica. Pensado como um painel visual (gráfico de rosca/barras) para responder "quantos itens estão em cada situação agora". Aceita os mesmos filtros da tela principal, mas não faz sentido ordenar ou paginar uma contagem agrupada.

### 6.3 Etiquetagem rápida na própria linha
É possível, sem abrir o cadastro completo do item, alterar rapidamente apenas:
- o **Grupo de Alocação** do item (Adicionar/Remover Grupo de Alocação),
- a **Tag** do item (Adicionar/Remover Tag),
- o **Motivo** do item (Adicionar/Remover Motivo),

cada um aceitando ficar vazio (remover a associação atual) sem precisar reenviar o resto do cadastro do item.

### 6.4 Edição do item / vínculos
Ao editar um item completo:
- **Produto e Centro nunca podem ser alterados** depois de criado — para corrigi-los, é preciso excluir e recriar o item.
- Vínculos opcionais editáveis: centro de origem (quando o abastecimento vem de outro centro, e não de um fornecedor externo), fornecedor/parceiro, etiqueta, motivo, grupo de alocação e perfil de buffer de referência.
- Campos operacionais livres: quantidade de embalagem (lote de compra), MOQ, Lead Time, Frequência, classificações livres (classe, classificação, segmento), estoque físico e estoque reservado (o **estoque disponível** é sempre calculado automaticamente como físico menos reservado — nunca digitado).
- **As quatro medidas de zona do buffer (Zona Vermelha Base, Zona Vermelha Segurança, Zona Amarela, Zona Verde) só podem ser editadas manualmente quando o item está sendo definido, no mesmo salvamento, como "Tipo de Buffer = Fixo/Manual"**. Se qualquer uma dessas quatro for enviada com o tipo de buffer diferente de Fixo/Manual, o salvamento inteiro é rejeitado com o aviso: *"As zonas do buffer só podem ser editadas quando o Tipo de Buffer é Fixo/Manual."*
- Campos exclusivos do Robô (nunca digitáveis pelo usuário, só de leitura): ADU, ADI, desvio padrão, coeficiente de variação, os tamanhos das três zonas (com a exceção acima), o delta aplicado por ajustes de zona, e a demanda qualificada — além de indicadores derivados na hora da consulta (topos de zona, zonas de execução, zonas analíticas).

### 6.5 Aba "Configuração de Buffer"
Concentra a configuração fina do buffer do item, incluindo:
- **Configuração de ADU**: dias de histórico e dias futuros usados no cálculo do consumo médio (sobrepõe, item a item, a janela padrão do perfil de buffer).
- **Parametrização Zona Verde**: os mesmos três interruptores do perfil de buffer, mas configuráveis por item (Usa MOQ / Usa ADU × Frequência / Usa ADU × Lead Time × Fator).
- **Usa DAF na Zona Verde**: por padrão um ajuste de demanda (DAF) só afeta as zonas Vermelha e Amarela; este interruptor permite que afete também a Verde.
- **Picos Qualificados (Demanda Qualificada)**: horizonte (Dlt ou Dias) e limiar (ADU ou % da Zona Vermelha de Planejamento) usados para decidir quando uma demanda pendente entra na demanda qualificada.
- **Tipo de Buffer**: Normal, Fixo/Manual, Mín/Máx, Mín/Máx Dinâmico (ver [7.2](#72-zonas-do-buffer)).
- **Perfil de Buffer**: qual perfil o item segue, e um interruptor **"Fixar Perfil de Buffer"** — se não fixado, no próximo recálculo automático de perfis o perfil do item pode voltar a ser reatribuído automaticamente; se fixado, o item mantém sempre o perfil escolhido manualmente. A tela avisa isso antes de confirmar.
- Aviso importante mostrado na tela: **ZAFs (ajustes de zona) não são aplicados em buffers do tipo Fixo/Manual.**

### 6.6 Abas de leitura (linha expandida)
Ao expandir uma linha da tabela principal, abrem-se sub-abas com o detalhe daquele item específico:

| Aba | O que mostra |
|---|---|
| **Entradas** | Pedidos de entrada ainda não totalmente entregues daquele item (reais, nunca fictícios) — quantidade pendente, prazo (dias para receber/dias de atraso), indicador e cor de "Buffer de Tempo" e de "Buffer de Execução". |
| **Saídas** | Pedidos de saída pendentes daquele item. |
| **ZAF / BAF / DAF** | Ajustes ativos e históricos daquele item especificamente (mesmos dados das telas de Ajustes Planejados, filtrados para este item). |
| **Pedidos Fictícios** | Pedidos fictícios (simulação) de entrada e saída lançados para este item. |
| **Consumo Histórico** | Série diária de consumo real do item, com o status de revisão de cada dia (ver [7.7](#77-histórico-de-consumo-e-revisão)). |
| **Previsão de Demanda** | Previsão de demanda lançada para o item, por período. |
| **Gráfico DDMRP** | Gráfico com a evolução diária dos indicadores de buffer do item (estoque, zonas, netflow) ao longo do tempo — a leitura de "hoje" é sempre montada em tempo real, mesmo antes do Robô rodar no dia. |
| **Alerta de Estoque Projetado** | Simulação, dia a dia, de um período futuro: mostra qual seria o estoque do item se nada mudar além do que já está agendado. Ver [7.9](#79-alerta-de-estoque-projetado). |
| **Configuração de Buffer** | Ver [6.5](#65-aba-configuração-de-buffer). |
| **Notas** | Anotações livres da equipe sobre este item (ver [6.7](#67-notas)). |

### 6.7 Notas
Comentários livres vinculados a um item, para o time de planejamento registrar observações. Só quem criou a nota pode **editá-la**; qualquer usuário logado pode **excluir** a nota de outra pessoa. O item vinculado não pode ser trocado depois de criada a nota — só o texto pode ser editado.

### 6.8 Espaço de Trabalho (Workspace) na tela principal
Cada item pode ter uma quantidade simulada pessoal, por usuário:
- Ao informar uma quantidade e marcar como **Aprovado**, o sistema soma essa quantidade ao netflow normal do item, formando um "netflow simulado", e recalcula qual seria a % de ocupação do buffer e a cor resultante — permitindo testar mentalmente o efeito de um pedido diferente antes de criá-lo de verdade.
- Se **não aprovado**, o registro fica salvo mas sem efeito na simulação.
- Cada usuário só enxerga e altera os próprios registros.
- Botão **Limpar Espaço de Trabalho** apaga de uma vez todos os registros do usuário logado (não pede item específico). Limpar quando não há nada salvo não é um erro.
- Se o item ainda não tiver buffer calculado pelo Robô, a simulação retorna um resultado neutro (0%, sem cor) — não é erro.
- Essa mesma quantidade aprovada é a base de entrada da [Alocação Priorizada](#5-alocação-priorizada), e aparece nas colunas "Qnt Otimizada a Pedir"/"Qnt Otimizada Sugerida" do relatório principal.

---

## 7. Fórmulas e indicadores calculados

Nenhum destes valores é digitado manualmente — todos vêm do cálculo diário automático, com a única exceção do **Tipo de Buffer Fixo/Manual** (ver [6.4](#64-edição-do-item--vínculos)).

### 7.1 Consumo médio diário (ADU)
Quantas unidades do item são, em média, consumidas por dia. Dependendo de quantos "dias de histórico" e "dias futuros" estão configurados no item (aba Configuração de Buffer):
- **Histórico** (só dias de histórico configurados): soma o consumo real dos últimos N dias não descartados e divide sempre por N — mesmo que o item seja novo e não tenha N dias de dado ainda. Um dia marcado como "Descartado" não conta; a janela busca mais um dia anterior até completar N dias válidos. Sem nenhum dado válido, o resultado é zero.
- **Futuro** (só dias futuros configurados): usa a Previsão de Demanda dos próximos N dias corridos, já fracionada dia a dia (ver [7.6](#76-como-a-previsão-de-demanda-é-aberta-dia-a-dia)) — dias não úteis contam como zero, mas o divisor continua sendo N dias corridos.
- **Misto** (ambos configurados): média simples entre o valor do Histórico e o valor do Futuro.
- Se nenhum dos dois estiver configurado, o ADU fica em branco.

### 7.1-b Intervalo médio de demanda (ADI)
Mede o quão intermitente é a demanda: quanto maior, mais irregular o consumo. É a quantidade de dias com registro de histórico numa janela fixa (padrão 360 dias) dividida pela quantidade desses dias que teve consumo maior que zero. Diferente do ADU, aqui os dias marcados como "Descartado" continuam contando normalmente. Sem nenhum dia com consumo no período, o resultado é zero.

**Desvio Padrão** e **CV (Coeficiente de Variação)**: calculados junto com o ADU (mesma janela de dias válidos do "Histórico"), medem o quanto o consumo diário varia em torno da média — quanto maior, mais irregular a demanda.

### 7.2 Zonas do buffer
O tamanho das três zonas depende do **Tipo de Buffer** do item:

- **Normal** (padrão):
  - **Zona Amarela** = ADU (já ajustado por eventual DAF ativo) × Lead Time.
  - **Zona Verde** = o maior valor entre os candidatos habilitados: MOQ, ADU × Lead Time × Fator de Lead Time, e Frequência × ADU. Um candidato desligado não participa da comparação.
  - **Zona Vermelha** = soma de duas partes: "Segurança" (ADU ajustado × Lead Time × Fator de Lead Time) e "Base" (o mesmo valor, multiplicado também pelo Fator de Variabilidade).
  - Nenhuma zona fica negativa — o mínimo é sempre zero.
- **Fixo/Manual**: as zonas **não** são recalculadas automaticamente — usam os valores definidos manualmente (pelo cadastro do item ou por um BAF ativo).
- **Mín/Máx**: Zona Amarela sempre zero (sem faixa intermediária), sem parte de "segurança" no vermelho; a Zona Vermelha é o **maior consumo diário observado** numa janela recente (padrão 180 dias) — um único dia de pico define o tamanho da zona vermelha. Zona Verde segue a mesma fórmula do tipo Normal.
- **Mín/Máx Dinâmico**: mais reativo a picos recentes — analisa uma janela móvel (do tamanho do maior valor entre Lead Time e Frequência) e usa o **maior consumo acumulado** observado nessa janela. Zona Amarela = ADU × Lead Time; Zona Verde = MOQ; Zona Vermelha = esse maior acumulado menos o consumo esperado no lead time (nunca negativo). **Este tipo nunca sofre efeito de DAF** — usa sempre o ADU "puro".

### 7.3 Topos de zona e ponto de pedido
A partir das quatro medidas físicas de zona, sempre calculados na hora (nunca gravados):
- **Topo do Vermelho** = soma das duas partes do vermelho.
- **Topo do Amarelo** = Vermelho + Amarelo.
- **Topo do Verde** = Vermelho + Amarelo + Verde — este é o **ponto de pedido**: quando o netflow projetado cai nele ou abaixo, é hora de repor.
- **Zonas de Execução**: segunda leitura (metade da zona vermelha + valor puro da zona amarela), usada para avaliar pedidos já em andamento frente ao estoque físico (não à posição projetada).
- **Zonas Analíticas**: leitura mais granular, separando a parte "segura" da parte de "excesso" de cada zona (ex. "Vermelho Seguro" vs. "Vermelho Excesso").

### 7.4 Ajuste de Zona (ZAF) aplicado
Quando um ZAF está ativo e vigente para uma zona específica, o delta calculado (valor fixo ou percentual sobre a zona) é **somado** à zona correspondente já calculada. O ajuste de vermelho é somado especificamente na parte "Base", nunca na "Segurança".

### 7.5 Fluxo líquido (Netflow) e demanda qualificada
**Netflow = Estoque + Entradas pendentes − Demanda qualificada.** É a principal métrica de decisão de reposição.

**Demanda qualificada**: soma, entre os pedidos de saída ainda pendentes, apenas os dias em que a quantidade pendente daquele dia ultrapassa um limiar (baseado num múltiplo do ADU, ou numa % da Zona Vermelha), dentro de um horizonte de dias configurável. Ou seja, nem toda demanda futura pendente entra no cálculo — só a que é grande o suficiente para ser considerada um "pico" qualificado. O dia de hoje é um caso especial que sempre acumula qualquer atraso e a demanda do próprio dia.

**Quantidade sugerida de pedido**: se o netflow está abaixo do topo da Zona Amarela, o sistema sugere pedir a diferença entre o topo da Zona Verde (ponto de pedido) e o netflow atual; caso contrário, não sugere pedido (zero). O valor sugerido é sempre arredondado para baixo ao múltiplo de embalagem mais próximo, e zerado se ficar abaixo do MOQ.

### 7.6 Como a Previsão de Demanda é aberta dia a dia
A previsão é cadastrada por período (um valor total para um intervalo de datas, tipicamente um mês). O sistema sabe "abri-la" em uma linha por dia:
- Cada **dia útil** do intervalo recebe uma fração igual do total (total ÷ quantidade de dias úteis do intervalo).
- Cada **dia não útil** também aparece, mas com valor **zero**.
- Quais datas são dias úteis vem de um calendário próprio do sistema (marcado dia a dia, não derivado automaticamente do dia da semana).
- É possível consultar tanto a versão **diária (explodida)** quanto a **agrupada** (o valor mensal cheio, como foi cadastrado).

### 7.7 Histórico de Consumo e revisão
Todo registro de consumo nasce como **"Não Revisado"**. Um analista pode revisar e marcar cada dia como:
- **Não Revisado** — estado inicial.
- **Revisado** — conferido e válido.
- **Descartado** — considerado atípico/erro de lançamento; esse dia é **ignorado no ADU e no desvio padrão/CV**, mas ainda conta normalmente no ADI.

### 7.8 Cor do buffer (BufferColor)
Classifica a situação de uma quantidade (tipicamente o netflow, mas a mesma classificação também é usada para o estoque físico) frente às três zonas:

| Cor | Significado |
|---|---|
| **Sem Cor** | O buffer do item ainda não foi calculado. |
| **Preto** | Ruptura — a quantidade está zerada ou negativa. |
| **Vermelho** | Dentro da zona vermelha — situação crítica, reposição urgente. |
| **Amarelo** | Dentro da zona amarela — situação de atenção. |
| **Verde** | Dentro da zona verde — situação saudável. |
| **Azul** | Acima do topo da zona verde — excesso de estoque. |

Existe ainda a **cor analítica**, mais granular, que distingue "Vermelho Seguro" de "Vermelho Excesso" e "Amarelo Seguro" de "Amarelo Excesso", além das mesmas classificações de ruptura/excesso/sem cor.

### 7.9 Alerta de Estoque Projetado
Simula, dia a dia, um período futuro para um único item. A cada dia: o estoque de abertura é o de fechamento do dia anterior; o consumo do dia é o **maior valor** entre ADU, pedidos de saída programados e previsão de demanda daquele dia (cada um pode ser individualmente ligado/desligado na tela — "Usar ADU", "Usar Previsão", "Usar Saídas"); o estoque de fechamento soma as entradas do dia (se "Usar Entradas" estiver ligado) e subtrai esse consumo. Também é possível configurar se pedidos atrasados devem ser "empurrados" para hoje na simulação ("Acumular Entradas/Saídas em Hoje"), e se pedidos fictícios entram nas somas ("Usar Pedidos Fictícios" — este é o único relatório do sistema que considera pedidos fictícios por padrão).

### 7.10 Dias de Cobertura
Quantos dias o estoque físico atual duraria, no ritmo do consumo médio: **Estoque Disponível ÷ ADU** (zero quando o ADU ainda não foi calculado, para não gerar um resultado infinito).

### 7.11 Buffer de Tempo e prazos de pedido
Para cada pedido de entrada com data de entrega prevista, o sistema calcula quanto do lead time já se passou, como percentual, e classifica numa cor (mesmas cinco faixas — Preto quando o pedido já deveria ter chegado há mais tempo do que o próprio lead time previa). Também mostra: dias que faltam para a entrega prevista, e dias de atraso já acumulados (ambos zero quando não se aplicam).

### 7.12 Buffer de Execução por pedido
Para cada pedido de entrada (aba Entradas), soma o estoque físico do item a todos os pedidos de entrada **anteriores** a ele (mesmo produto e centro, criados antes e com entrega prevista até a mesma data), e divide pelo Topo do Amarelo de Execução — dando a ideia de "quanto do buffer de execução já estaria coberto até este pedido específico chegar".

---

## 8. Ajustes Planejados (BAF, ZAF, DAF) e Ordens Fictícias

Os três tipos de ajuste (Ajuste de Buffer, Ajuste de Zona, Ajuste de Demanda) permitem alterações **temporárias e programadas**, por período de vigência, no comportamento do buffer de um item, sem precisar mexer manualmente todo dia.

**Regras comuns aos três:**
- Vinculados a um produto + centro específico, com data de início e fim de vigência.
- **As datas de vigência nunca podem ser alteradas depois de criado o ajuste** — só recriando o registro.
- Podem ser **ativados/desativados** a qualquer momento sem reenviar todo o cadastro.
- **Não pode existir mais de um ajuste ativo do mesmo tipo, para o mesmo item, com período sobreposto** (o ZAF tem uma exceção — ver abaixo).

### 8.1 Ajuste de Buffer (BAF)
Troca temporariamente **o tipo de buffer inteiro** de um item (ex.: para Fixo/Manual durante uma promoção). Quando o tipo escolhido é Fixo/Manual, também define diretamente os tamanhos das três zonas nesse período.

**Reversão automática ("voltar ao normal"):**
- Ao **criar** um BAF, o sistema tira uma "foto" do estado atual do buffer do item (tipo + tamanho das quatro zonas), guardada dentro do próprio BAF e nunca mais alterada. Se o item ainda não existir nesse momento, a foto fica vazia (não é erro).
- Quando o **período de vigência termina**, o sistema devolve automaticamente o buffer do item ao estado da foto — só os campos que tinham valor salvo; um campo sem valor na foto fica intocado.
- A reversão também acontece **imediatamente**, fora do ciclo diário, se o usuário **excluir** ou **desativar** manualmente um BAF que estava ativo e dentro do período.
- Um mesmo BAF nunca reverte duas vezes — depois de revertido, fica marcado como "já revertido" (coluna "Já Revertido"). Se for **reativado**, essa marca zera, permitindo reverter de novo numa futura desativação/expiração.
- A reversão só tem efeito **duradouro** quando o tipo revertido é Fixo/Manual — se o tipo revertido for Normal/Mín-Máx/Mín-Máx Dinâmico, o cálculo diário recalcula essas zonas do zero de qualquer forma no dia seguinte.

### 8.2 Ajuste de Zona (ZAF)
Aplica um ajuste temporário a **uma única zona específica** (Vermelha, Amarela ou Verde), somando um valor fixo ou percentual à zona calculada normalmente — por exemplo, reforçar só a zona verde numa campanha, sem mexer nas outras duas.

- Como o ajuste é por zona, **é permitido ter até três ZAFs ativos ao mesmo tempo no mesmo item** (um por zona) — a checagem de sobreposição de período considera a zona: dois ajustes na mesma zona não podem se sobrepor, mas um na Vermelha e outro na Verde podem coexistir no mesmo período.
- O tipo de ajuste pode ser em **Valor Fixo** ou em **Percentual** sobre a zona.
- Lembrete importante: **ZAFs não têm efeito em itens com Tipo de Buffer = Fixo/Manual.**

### 8.3 Ajuste de Demanda (DAF)
Aplica um ajuste temporário ao **ADU** usado no cálculo do buffer — por exemplo, para antecipar manualmente um aumento ou queda esperada de demanda (sazonalidade, campanha) antes que apareça no histórico real. Pode ser em valor fixo ou percentual; por padrão afeta as zonas Vermelha e Amarela (a Verde só é afetada se o item tiver a opção "Usa DAF na Zona Verde" ligada — ver [6.5](#65-aba-configuração-de-buffer)).

- Diferente do BAF, o DAF **não tem nenhuma lógica de "foto"/reversão automática** — é só um ajuste aplicado enquanto está ativo e vigente; ao excluir/desativar, nada é revertido em outro lugar (o próprio recálculo diário simplesmente deixa de aplicá-lo).
- **Lembrete importante: o Tipo de Buffer Mín/Máx Dinâmico nunca sofre efeito de DAF.**

### 8.4 Ordens Fictícias (Pedidos Fictícios)
Qualquer pedido criado manualmente pela interface é **sempre marcado automaticamente como fictício** — não existe opção de criar um pedido "real" pela tela. Pedidos reais só entram pelo processo automático de carga de dados. Isso garante que uma simulação de "e se eu fizesse esse pedido?" nunca se mistura, nem é apagada, pela sincronização automática de pedidos reais.

- **Tipos de pedido**: Ordem de Produção (chega no centro de destino), Transferência (entre dois centros), Ordem de Compra (de um fornecedor), Ordem de Venda (para um cliente).
- Cada pedido tem dois indicadores independentes: se representa **entrada** e/ou **saída** de estoque.
- **Campos calculados automaticamente**: quantidade pendente (total menos entregue, nunca negativa), Lead Time do Pedido (dias entre criação e entrega prevista), Dias para Receber / Dias em Atraso.
- **O que pode ser alterado depois de criado**: só campos operacionais (parceiro, quantidade, quantidade entregue, unidade de medida, posição, data de criação, data de entrega, observações). Número do pedido, produto, centros de origem/destino, tipo e os indicadores de entrada/saída/fictício **ficam fixos para sempre** — para corrigi-los é preciso excluir e recriar.

### Situações comuns (Ajustes Planejados e Ordens Fictícias)
- **Produto/Centro não encontrado** ao informar um vínculo que não existe.
- **"Já existe um ajuste ativo do mesmo tipo/zona para este item no período informado"** — resolva desativando/excluindo o ajuste anterior, ou ajustando o período do novo.
- **As zonas do buffer só podem ser editadas quando o Tipo de Buffer é Fixo/Manual** — ao tentar salvar valores de zona com outro tipo de buffer.
- **"Não encontrado"** ao editar/excluir/ativar-desativar um ajuste ou pedido que já não existe.

---

## 9. Mensagens e situações de erro

Mensagens gerais, que podem aparecer em qualquer tela:

| O que aparece | O que significa | O que fazer |
|---|---|---|
| "Não encontrado" | O registro que você tentou editar, excluir, ativar/desativar ou abrir não existe mais (foi excluído por alguém, ou o link/atalho está desatualizado). | Atualize a listagem e tente novamente a partir dela. |
| "**Produto** / **Centro** / **Parceiro** / **Etiqueta** / **Motivo** / **Grupo de Alocação** / **Perfil de Buffer não encontrado**" | Um dos vínculos escolhidos no formulário não corresponde (mais) a um registro existente — normalmente porque foi excluído recentemente por outra pessoa. | Atualize a lista de opções do campo (recarregando a tela) e escolha novamente. |
| "Já existe um ajuste ativo... no período informado" | Já existe outro BAF/ZAF/DAF ativo, do mesmo tipo (e, no caso do ZAF, da mesma zona), para o mesmo item, com período sobreposto. | Desative/exclua o ajuste anterior, ou ajuste as datas do novo para não sobrepor. |
| Mensagens de "sem permissão" | A ação é proibida para o usuário logado (ex.: editar a nota de outra pessoa, editar/excluir um motivo "de sistema", encerrar a sessão de outra pessoa). | Peça para o dono do registro fazer a ação, ou peça acesso a um perfil com a permissão necessária. |
| Erro genérico de carregamento/salvamento | O sistema não conseguiu se comunicar com o servidor (rede instável, servidor fora do ar) ou ocorreu um problema interno. Por segurança, a tela nunca mostra o detalhe técnico do problema. | Tente novamente em alguns instantes; se persistir, acione o suporte. |

Situações específicas por módulo já estão descritas junto de cada seção acima (Usuários em [3](#3-autenticação-usuários-e-perfis-de-acesso), cadastros mestres em [4](#4-telas-de-cadastros-mestres), Alocação Priorizada em [5](#5-alocação-priorizada), Ajustes Planejados/Ordens Fictícias em [8](#8-ajustes-planejados-baf-zaf-daf-e-ordens-fictícias)). Vale destacar também:

- **Motivo "de sistema" não pode ser editado/excluído** — a tela de Motivos mostra um aviso e desabilita as ações para esses registros.
- **Data de fim anterior à data de início** — ao cadastrar/editar uma Previsão de Demanda (ou qualquer período de vigência) com a data final antes da inicial.
- **Só quem criou a nota pode editá-la** — qualquer outro usuário logado só pode excluí-la, não editá-la.
- Nas relações entre itens (produto+centro filho/pai — "Buffer Mestre", usada internamente para modelar hierarquias entre itens relacionados), depois de criado o vínculo só a ordem/sequência pode ser alterada; os produtos e centros envolvidos ficam fixos.

---

## 10. Melhoria Contínua — relatórios de tendência

Estas telas não alteram nenhum dado — são apenas leituras que resumem informação já existente, pensadas para análise e acompanhamento ao longo do tempo.

### 10.1 Penetração de Buffer
Para todos os itens que tiveram histórico registrado num intervalo de datas (filtrável por um ou mais centros e/ou um produto), mostra, **por item**, quantos dias ele passou em cada cor de buffer dentro do período — respondendo perguntas como "esse item passou quantos dias em ruptura no último trimestre?". Pode ser visto sob duas perspectivas: pela cor do netflow (leitura "clássica" de saúde do buffer) ou pela cor de execução (estoque físico contra as zonas de execução — ver campo **Visualização**). Também soma, à parte, quantos dias o item passou "em vermelho ou em ruptura" combinados.

### 10.2 Cores de Buffer por Dia
O inverso do relatório anterior: em vez de contar dias por item, conta, **por dia**, quantos itens estavam em cada cor de buffer — pensado para um gráfico de tendência mostrando a evolução da carteira inteira ao longo do período. Sempre mostra as duas perspectivas (netflow e execução) juntas. Tem uma opção de **visualização percentual**.

### 10.3 Acumulado de Buffers
Soma, dia a dia, os valores de zona e de estoque de **todos os itens qualificados** (que já têm zona vermelha calculada) dos centros informados (aqui a escolha de centros é obrigatória) — sem filtro de produto, sempre a carteira inteira somada daqueles centros. Mostra, por dia: total acumulado de cada zona (netflow, execução e analítica — ver opção **Visão Analítica**), estoque disponível total, fluxo líquido total, excesso de estoque total (com e sem considerar a zona amarela) e a faixa de oscilação mínima/máxima esperada do estoque agregado. É uma visão "de cima", da saúde geral do buffer da operação ao longo do tempo, sem detalhar item a item.

---

## 11. Assistente (chat)

Ícone de chat flutuante, disponível em qualquer tela do sistema, para tirar dúvidas sobre como o sistema funciona (telas, regras, indicadores, mensagens de erro) sem precisar sair da tela em que você está trabalhando.

---

## 12. Glossário de tradução (Português / Inglês / Espanhol)

| Português | Inglês | Espanhol |
|---|---|---|
| Dashboard | Dashboard | Dashboard |
| Mestres | Masters | Maestros |
| Centros | Centers | Centros |
| Tags | Tags | Etiquetas |
| Grupos de Alocação | Allocation Groups | Grupos de Asignación |
| Motivos | Reasons | Motivos |
| Clientes/Fornecedores | Customers/Suppliers | Clientes/Proveedores |
| Perfis de Buffer | Buffer Profiles | Perfiles de Buffer |
| Ajustes Planejados | Planned Adjustments | Ajustes Planificados |
| Ajuste de Demanda (DAF) | Demand Adjustment Factor (DAF) | Factor de Ajuste de Demanda (DAF) |
| Ajuste de Buffer (BAF) | Buffer Adjustment Factor (BAF) | Factor de Ajuste de Buffer (BAF) |
| Ajuste de Zona (ZAF) | Zone Adjustment Factor (ZAF) | Factor de Ajuste de Zona (ZAF) |
| Ordens Fictícias | Fictitious Orders | Órdenes Ficticias |
| Gestão de Buffer de Estoque | Inventory Buffer Management | Gestión de Buffer de Inventario |
| Melhoria Contínua | Continuous Improvement | Mejora Continua |
| Penetração de Buffer | Buffer Penetration | Penetración de Buffer |
| Cores de Buffer por Dia | Buffer Colors by Day | Colores de Buffer por Día |
| Acumulado de Buffers | Buffer Accumulation | Acumulado de Buffers |
| Segurança | Security | Seguridad |
| Usuários | Users | Usuarios |
| Perfis de Acesso | Roles | Roles |
| Assistente | Assistant | Asistente |
| Zona Vermelha / Amarela / Verde | Red / Yellow / Green Zone | Zona Roja / Amarilla / Verde |
| Ponto de pedido (Topo do Verde) | Order point (Top of Green) | Punto de pedido (Tope del Verde) |
| Netflow (Fluxo Líquido) | Net Flow | Flujo Neto |
| ADU (Consumo Médio Diário) | ADU (Average Daily Usage) | ADU (Uso Diario Promedio) |
| ADI (Intervalo Médio de Demanda) | ADI (Average Demand Interval) | ADI (Intervalo Promedio de Demanda) |
| Lead Time | Lead Time | Lead Time |
| MOQ | MOQ | MOQ |
| Cor do Buffer | Buffer Color | Color del Buffer |
| Vermelho / Amarelo / Verde / Azul / Preto / Sem Cor | Red / Yellow / Green / Blue / Black / No Color | Rojo / Amarillo / Verde / Azul / Negro / Sin Color |
| Fictício(a) | Fictitious | Ficticio(a) |
| Espaço de Trabalho (Workspace) | Workspace | Espacio de Trabajo |
| Tipo de Buffer | Buffer Type | Tipo de Buffer |
| Normal / Fixo-Manual / Mín-Máx / Mín-Máx Dinâmico | Normal / Manual Fixed / MinMax / Dynamic MinMax | Normal / Fijo Manual / MinMax / MinMax Dinámico |
| Dias de Cobertura | Coverage Days | Días de Cobertura |
| Alocação Priorizada | Prioritized Allocation | Asignación Priorizada |
| Demanda Qualificada | Qualified Demand | Demanda Calificada |

---

## 13. Onde encontro cada coisa — referência rápida

| Pergunta do usuário | Onde está |
|---|---|
| "Qual a cor de buffer de um item / quero ver todos os itens em ruptura" | Gestão de Buffer de Estoque → colunas "Cor do Buffer (Netflow)" / filtro por cor. |
| "Quanto devo pedir desse item hoje?" | Gestão de Buffer de Estoque → coluna "Qnt a Pedir". |
| "Por que esse item não está sugerindo pedido?" | Netflow do item está acima do Topo da Zona Amarela (ver [7.5](#75-fluxo-líquido-netflow-e-demanda-qualificada)) — confira na aba Configuração de Buffer / Gráfico DDMRP. |
| "Como o ADU desse item foi calculado?" | Aba Configuração de Buffer do item (dias de histórico/futuro configurados) + [7.1](#71-consumo-médio-diário-adu). |
| "Quero simular um pedido diferente sem alterar nada de verdade" | Espaço de Trabalho, direto na linha do item em Gestão de Buffer de Estoque ([6.8](#68-espaço-de-trabalho-workspace-na-tela-principal)). |
| "Preciso reforçar temporariamente só a zona verde de um item numa campanha" | Tela Ajuste de Zona (ZAF), zona = Verde. |
| "Preciso travar as zonas desse item num valor fixo por um tempo" | Tela Ajuste de Buffer (BAF), Tipo de Buffer = Fixo/Manual. |
| "Vou ter um pico/queda de demanda esperado que ainda não está no histórico" | Tela Ajuste de Demanda (DAF). |
| "Quero testar um pedido hipotético sem que ele conte como pedido real" | Tela Ordens Fictícias (ou aba Pedidos Fictícios do item). |
| "Tenho um caminhão/orçamento limitado para vários itens de um grupo" | Botão Alocação Priorizada, dentro de Gestão de Buffer de Estoque. |
| "Quantos dias esse item ficou em vermelho no último trimestre?" | Tela Penetração de Buffer. |
| "Como a carteira inteira evoluiu nos últimos meses?" | Telas Cores de Buffer por Dia e Acumulado de Buffers. |
| "Esse dia de consumo foi um erro de lançamento" | Aba Consumo Histórico do item → marcar como "Descartado". |
| "Quero anotar uma observação sobre esse item para o time" | Aba Notas do item. |
| "O sistema recém recalculou os dados ou ainda vou ver o valor antigo?" | O recálculo é diário (o "Robô"); alterações manuais em campos de configuração só refletem totalmente nos indicadores calculados após o próximo recálculo. |
