# ExplicaIA.md

> Este documento explica o **domínio de negócio** desta aplicação para outro modelo de IA que vá trabalhar neste repositório sem contexto prévio. Ele não fala sobre código, arquitetura de software, padrões de implementação ou banco de dados — fala sobre **o que o sistema faz, por que ele faz assim, o que cada regra significa pro negócio, e o que cada erro comunica**. Para detalhes técnicos (como as coisas são implementadas), o outro modelo deve olhar o `CLAUDE.md`; para as fórmulas matemáticas exatas, o `Formulas.md`.

---

## 1. O que é esse sistema, em uma frase

É uma ferramenta de **gestão de estoque orientada por demanda (DDMRP — Demand Driven Material Requirements Planning)**. Ela recebe dados de consumo/estoque/pedidos de um cliente, calcula automaticamente **buffers de estoque** (faixas de segurança: vermelho/amarelo/verde) para cada combinação produto+centro, e a partir disso diz **o que está saudável, o que está em risco de ruptura, o que está em excesso, e quanto deveria ser comprado/produzido/transferido agora**.

Não é um ERP. Não substitui o sistema do cliente — ele **recebe** dados do ERP/sistema do cliente (via arquivo), **calcula** em cima deles, e **devolve** recomendações e indicadores. O nome interno pro motor que faz isso é "o Robô" (Robot).

## 2. Filosofia central

### 2.1. "O Robô" é o coração do sistema, dividido em duas fases sempre na mesma ordem

1. **Carga (ingestão)**: importa dados externos do cliente (hoje só via arquivo CSV) — produtos, centros, itens de estoque, histórico de consumo, previsões (forecast), pedidos em aberto.
2. **Cálculo**: recalcula, para cada item (produto+centro), todos os indicadores DDMRP a partir dos dados carregados — consumo médio, variabilidade, zonas de buffer, demanda qualificada, etc.

A carga **sempre** roda antes do cálculo, nunca em paralelo, e se a carga falhar o cálculo nem começa — a lógica é: calcular em cima de dado desatualizado/errado é pior do que não calcular.

### 2.2. Recalcula tudo, todo dia, do zero — nunca incremental

Toda vez que o cálculo roda, cada indicador calculado é **zerado e recalculado do zero** para todos os itens ativos (ou para um item específico, se pedido assim). Não existe "atualização incremental" ou "só recalcula o que mudou". Isso é uma escolha deliberada: garante que o resultado nunca acumula erro de execuções anteriores, ao custo de recalcular coisas que talvez não tivessem mudado. Filosofia: **previsibilidade e correção acima de economia de processamento**.

### 2.3. Separação rígida entre "o que o usuário edita" e "o que o robô calcula"

Todo indicador que o robô calcula (consumo médio, desvio padrão, zonas de buffer, demanda qualificada, etc.) é **só leitura** para quem usa o sistema por fora — ninguém pode digitar um valor de zona de buffer diretamente, por exemplo. Só o robô escreve esses campos. Isso existe pra garantir que o número que o usuário vê sempre reflete a fórmula oficial, nunca um valor manual que ficou "preso" ali.

A exceção deliberada é o **tipo de buffer "Manual Fixo"** (ver seção 5) — aí, e só aí, o usuário assume o controle da zona manualmente, através de um mecanismo formal (Ajuste de Buffer), nunca digitando direto.

### 2.4. Nada é apagado de verdade

Toda exclusão no sistema é reversível por trás — o registro fica marcado como excluído mas continua existindo, com registro de quando e por quem. Isso vale pra qualquer entidade de negócio (produtos, centros, pedidos, ajustes, etc.). Filosofia: **rastreabilidade total**, nada desaparece silenciosamente.

### 2.5. Cada cliente é um mundo isolado

Cada implantação do sistema atende **um único cliente**. Não há multi-tenant dentro de uma mesma instância — configurações, arquivos de carga e regras de negócio específicas de um cliente não se misturam com as de outro.

### 2.6. O sistema prefere "adicionar a peça agora, ligar depois"

Vários campos/indicadores foram adicionados ao modelo de negócio *antes* de existir uma fórmula que os alimenta — ficam reservados, documentados como "ainda não calculado por nenhuma regra", esperando a regra ser definida e aprovada. Isso é intencional (evita retrabalho de modelagem depois), não um bug ou coisa esquecida — mas também significa que **nem todo campo que existe no sistema já tem uma lógica de negócio por trás hoje**.

---

## 3. Os conceitos de negócio principais

| Conceito | O que representa |
|---|---|
| **Produto** | Um item que pode ser comprado, vendido, produzido ou transferido. |
| **Centro** | Um local físico/operacional (depósito, fábrica, loja) onde produtos ficam armazenados ou são consumidos. |
| **Item de Centro (Produto+Centro)** | A unidade real de gestão do DDMRP — "este produto, neste centro". É aqui que moram o buffer de estoque, os parâmetros de cálculo (lead time, quantidade mínima de pedido, etc.) e todos os indicadores calculados. Um mesmo produto pode ter comportamento de buffer totalmente diferente em centros diferentes. |
| **Perfil de Buffer** | Um "molde" de configuração reutilizável (fator de lead time, fator de variabilidade, se o item é produção sob encomenda, etc.) que pode ser aplicado a vários itens de centro, evitando configurar cada item do zero. |
| **Histórico (History)** | O consumo realizado de um item de centro, um valor por dia. É o "passado" — dado que já aconteceu e foi confirmado. |
| **Previsão (Forecast)** | A expectativa de demanda futura de um item de centro, cadastrada por **período** (ex.: um valor total para um mês inteiro), não dia a dia — o sistema é quem distribui esse valor pelos dias do período. |
| **Calendário** | Define quais dias são dias úteis e quais não são, data a data (não é uma regra genérica de "toda segunda é útil" — cada data individual tem sua marcação). É usado para saber como distribuir a previsão pelos dias e para outras contagens de "dias corridos vs. dias úteis". |
| **Pedido (Order)** | Uma ordem de movimentação real — pode ser ordem de produção, transferência entre centros, pedido de compra ou pedido de venda. Tem uma quantidade pedida e uma quantidade já entregue; a diferença é o que ainda falta chegar/sair. Pedidos podem ser marcados como **fictícios** (simulações, não movimentações reais) — isso muda como eles entram (ou não) em quase todo cálculo, ver seção 6. |
| **Parceiro (Partner)** | Fornecedor ou cliente externo associado a um pedido ou a um item de centro. |
| **Tag / Motivo (Reason) / Grupo de Alocação** | Formas de classificar e organizar itens de centro para relatórios e regras — um "Motivo" pode ser protegido pelo sistema (ver seção 7). |
| **Ajuste de Buffer / Ajuste de Zona / Ajuste de Demanda** | Três mecanismos independentes de **override temporário** com data de início e fim — permitem, por um período programado, mudar manualmente o tipo/tamanho do buffer, ajustar uma zona específica, ou ajustar o consumo médio considerado no cálculo. Pensados para eventos conhecidos com data marcada (uma promoção, uma parada de fábrica, uma mudança de fornecedor) sem precisar reconfigurar o item permanentemente. |
| **Workspace** | Uma área de simulação **pessoal, por usuário** — cada usuário pode propor uma quantidade de pedido diferente da sugerida pelo sistema para um item e "aprovar" essa proposta, para ver como o buffer ficaria se aquele pedido fosse de fato colocado, sem afetar ninguém além de quem está simulando. |
| **Nota (Note)** | Uma anotação de texto livre associada a um item de centro, para contexto humano que não cabe em nenhum campo estruturado. |

---

## 4. Os indicadores DDMRP calculados pelo robô, explicados

Todos os detalhes matemáticos exatos (incluindo casos de borda) estão em `Formulas.md`. Aqui vai o **significado de negócio** de cada um.

### Consumo médio diário (Adu — Average Daily Usage)

Quanto, em média, esse item consome por dia. Pode ser calculado de três formas, dependendo de como o item está configurado:

- **Histórico**: olhando só pro passado (média do consumo real dos últimos N dias).
- **Futuro**: olhando só pra frente (média da previsão de venda/consumo dos próximos N dias).
- **Misto**: a média simples entre os dois — usado quando o item tem tanto histórico relevante quanto uma previsão futura confiável, e se quer equilibrar os dois.

Dias em que o consumo foi marcado como "descartado" (um outlier identificado por alguém, ex.: uma venda anormal que não deve influenciar a média) são pulados no cálculo do Histórico, e a janela de dias "empurra" mais pra trás até completar dias válidos suficientes — ou seja, **um dia descartado nunca reduz a quantidade de dias considerados, só substitui por um dia mais antigo**.

### Desvio padrão e Coeficiente de Variação (StandardDeviation / Cv)

Medem **o quão instável/imprevisível** é o consumo desse item. Um Cv alto significa que o consumo varia muito de dia pra dia (item "nervoso"), o que normalmente justifica um buffer de segurança maior.

### Intervalo Médio de Demanda (Adi — Average Demand Interval)

Mede **o quão intermitente** é a demanda — ou seja, o quanto o consumo é "espaçado" (dias sem nenhum consumo entre um evento de venda e outro). Um Adi alto indica um item que vende raramente, mas quando vende pode vender uma quantidade relevante — um perfil de demanda diferente de um item que vende um pouco todo dia.

### Zonas do Buffer (Vermelha / Amarela / Verde)

O coração visual do DDMRP. Um buffer de estoque é dividido em três faixas, empilhadas:

- **Zona Vermelha**: a reserva de segurança contra ruptura. Ficar dentro dela é sinal de alerta — o estoque está perto de faltar.
- **Zona Amarela**: a faixa "normal de consumo" — o estoque que se espera consumir até chegar o próximo pedido.
- **Zona Verde**: a faixa de "tamanho de pedido recomendado" — determina de quanto em quanto se pede, evitando pedir toda hora em quantidades pequenas.

O tamanho de cada zona depende do consumo médio, do lead time (tempo entre pedir e receber), da variabilidade do item e de quanto se pede por vez — e pode ser recalculado de formas diferentes dependendo do **tipo de buffer** do item:

- **Normal**: fórmula padrão DDMRP, recalculada todo dia a partir do consumo médio e parâmetros do item.
- **Manual Fixo**: o usuário (via um Ajuste de Buffer) define as zonas diretamente, e elas não são recalculadas pelo robô enquanto esse ajuste estiver vigente — usado quando o comportamento normal do DDMRP não faz sentido pra aquele item num período específico.
- **Min/Max**: uma variação mais simples, onde a zona vermelha é definida pelo maior consumo diário já visto numa janela de tempo (não uma fórmula estatística), e não tem zona amarela nem zona vermelha de segurança — usado tipicamente pra itens de comportamento mais simples/previsível.
- **Min/Max Dinâmico**: parecido com o Min/Max, mas olha pra **maior acúmulo de consumo dentro de uma janela móvel** (o maior "pico consecutivo" observado), em vez do maior dia isolado — mais sensível a picos sustentados de demanda.

Um **Fator de Ajuste de Demanda** ativo pode alterar (pra mais ou pra menos, em valor fixo ou percentual) o consumo médio usado nas fórmulas de zona **antes** delas rodarem — pensado pra ajustar manualmente a expectativa de demanda de um item por um período (ex.: "esse produto vai vender 20% mais no mês que vem por causa de uma campanha", sem mudar o Adu calculado de verdade). **Esse ajuste não afeta o tipo Min/Max Dinâmico** — decisão deliberada, não esquecimento.

Depois das zonas calculadas, um **Fator de Ajuste de Zona** ativo pode somar um delta (fixo ou percentual) em cima de uma zona específica (vermelha, amarela ou verde, cada ajuste mira uma zona só) — pensado pra correções pontuais de tamanho de zona sem mexer na fórmula de base.

**Todo cálculo de zona sempre arredonda pra cima e nunca deixa uma zona negativa** — filosofia: é sempre mais seguro superestimar um pouco a proteção do estoque do que subestimar.

### Demanda Qualificada (QualifiedDemand)

Quanto da demanda **futura já conhecida** (pedidos de venda/saída já cadastrados, ainda não entregues) deve efetivamente "descontar" do estoque disponível na conta de reposição. Nem toda demanda futura conta — só entra a demanda que **ultrapassa um limiar** (um "pico" de saída relevante, não ruído do dia a dia) dentro de um horizonte de tempo proporcional ao lead time do item. A ideia é: pedidos grandes o bastante e próximos o bastante no tempo devem ser considerados na hora de decidir se é preciso repor estoque agora, mesmo antes desse pedido realmente sair.

### Fluxo Líquido (Netflow)

O indicador central de "saúde atual" de um item: `estoque disponível + o que já está a caminho (entradas pendentes) - a demanda futura já qualificada`. É esse número, comparado contra as zonas do buffer, que diz se o item está em zona vermelha (risco), amarela (normal) ou verde (excesso/saudável) **considerando o que ainda vai acontecer**, não só o estoque físico parado na prateleira.

### Quantidade de Pedido / Quantidade de Pedido Otimizada (OrderQuantity / OptimizedOrderQuantity)

Quanto deveria ser pedido agora, pra trazer o Fluxo Líquido de volta ao topo do buffer — só sugere pedir algo quando o Fluxo Líquido está abaixo da zona amarela. A versão "otimizada" ajusta esse número pra baixo, pra um múltiplo da embalagem/lote do fornecedor, e zera a sugestão se o valor calculado for menor que a quantidade mínima de pedido aceita pelo fornecedor (não vale a pena gerar um pedido menor que o mínimo permitido).

### Buffer de Execução vs. Buffer Analítico (leituras alternativas)

Além da leitura "planejamento" (zonas cheias, Netflow), o sistema também calcula uma leitura de **"execução"**, com zonas do buffer reduzidas pela metade em alguns pontos — pensada pra decisões operacionais do dia a dia (mais sensível, reage mais rápido) em vez da decisão estratégica de quanto comprar. Existe ainda um terceiro conjunto de zonas ("Analítico") reservado pra uma leitura futura ainda não definida.

### Buffer de Tempo (TimeBuffer), Dias pra Receber / Dias de Atraso

Indicadores por **pedido individual** (não por item de centro): o quão perto (ou quão atrasado) um pedido específico está da sua data de entrega esperada, numa escala relativa ao próprio lead time do pedido — usado pra colorir/priorizar pedidos abertos por urgência de acompanhamento.

### Simulação de Workspace (SimulatedNetflow)

Quando um usuário aprova uma quantidade de pedido simulada na sua área pessoal de trabalho (Workspace), o sistema recalcula o Fluxo Líquido **como se** aquele pedido já tivesse sido colocado, só pra visualização daquele usuário — sem alterar nenhum dado real de ninguém. É uma ferramenta de "e se eu pedir X, como fica o buffer?" antes de decidir de verdade.

---

## 5. Regras de negócio importantes (comportamento, não estrutura de dados)

- **Um item de centro não pode existir sem produto e centro válidos** — e o mesmo vale pra qualquer relação (fornecedor, tag, motivo, grupo de alocação, perfil de buffer): antes de aceitar a informação, o sistema sempre confirma que a referência existe de verdade. Uma referência inválida nunca é silenciosamente aceita nem vira um erro genérico de sistema — vira uma mensagem de negócio clara (ver seção 6).
- **Depois de criado, um vínculo estrutural não muda** — o produto e o centro de um item, por exemplo, são fixados na criação. Pra "corrigir" um vínculo errado, a via correta é excluir e recriar o registro, nunca editar por cima. Isso vale de forma consistente pra praticamente todo módulo que amarra duas entidades entre si (itens de centro, ajustes de zona/buffer/demanda, forecast, histórico, etc.) — o raciocínio é que mudar a identidade de um registro no meio do caminho corrompe o histórico de tudo que já foi calculado em cima dele.
- **Período de vigência também é fixo depois de criado**, nos três tipos de ajuste (Buffer/Zona/Demanda) — mesma lógica: mudar a data de um ajuste já aplicado bagunça o que já foi calculado com base nele.
- **Não pode haver dois ajustes ativos com períodos sobrepostos para o mesmo item** (e, no caso do Ajuste de Zona, para a mesma zona específica) — o sistema bloqueia a criação de um novo ajuste se o período dele colidir com um já ativo pro mesmo alvo. Isso existe porque dois ajustes conflitantes ativos ao mesmo tempo tornariam o resultado do cálculo ambíguo/instável.
- **Só o valor consumido de Histórico e Previsão pode ser editado depois de criado** — tudo o resto (produto, centro, período) é fixo. A única exceção é o **status de revisão do Histórico** (não revisado / revisado / descartado), que é justamente feito pra mudar depois: alguém revisa o consumo já registrado e pode marcar um dia como outlier a ser ignorado nos cálculos.
- **Um motivo (Reason) marcado como "do sistema" não pode ser editado nem excluído por ninguém através da aplicação** — só é criado assim por uma ação direta fora da interface normal, e depois fica protegido. É um mecanismo de "registro oficial, intocável pelo usuário comum".
- **Uma anotação (Note) só pode ser editada por quem a criou** — qualquer outro usuário pode lê-la, mas não alterá-la (excluir, por enquanto, não tem essa restrição).
- **Pedidos fictícios nunca são tratados como movimentação real**: eles nunca são atualizados/excluídos automaticamente por uma nova carga de dados (mesmo que coincidam por acaso com o identificador de um pedido real), e a maioria dos relatórios e cálculos os ignora por padrão — a única exceção deliberada é o cálculo de Demanda Qualificada, que os inclui, e o dia de "hoje" na mesma fórmula, que trata pedido fictício de forma diferente de pedido real (ver `Formulas.md`).
- **Criação de novos usuários exige estar autenticado** — não existe cadastro público de conta; um usuário só pode ser criado por outro usuário já logado.
- **Toda operação de escrita registra quem fez e quando**, automaticamente, sem precisar ser informado por quem chama a ação — isso alimenta rastreabilidade e, no caso das Notas, também define a regra de "só quem criou pode editar".
- **A carga de dados nunca apaga uma tabela inteira sozinha por acidente**: se um arquivo de carga vier vazio (por erro de configuração, por exemplo), o sistema não executa nenhuma exclusão em massa — mas um arquivo que venha **incompleto** (faltando linhas que deveriam estar lá) ainda pode, sim, acabar excluindo registros que "não foram enviados dessa vez", dependendo de como aquela fonte de dados está configurada. Isso é um risco conhecido e deliberadamente restrito só às fontes de dados onde faz sentido (ex.: não se aplica a Pedidos).
- **Toda tela de listagem despreza registros excluídos** — uma vez marcado como excluído, um registro nunca reaparece em consultas normais.

---

## 6. Exceções lançadas manualmente e o que cada uma significa

O sistema nunca deixa um erro de validação de negócio virar um erro técnico genérico (código 500) — toda vez que uma regra de negócio é violada, uma mensagem de erro clara e específica é devolvida. Isso é filosofia central: **quem está integrando ou usando o sistema deve sempre entender *por que* algo foi rejeitado**, nunca receber um erro opaco.

As mensagens abaixo são agrupadas por significado de negócio (o texto exato pode variar ligeiramente por módulo, mas o motivo é sempre o mesmo tipo de situação):

### "X not found." (ex.: "Product not found.", "Center not found.", "Partner not found.", "Tag not found.", "Reason not found.", "Allocation group not found.", "Buffer profile not found.", "Role not found.", "Origin center not found.", "Destiny center not found.", "Father product not found.", "Father center not found.", "Center product not found.")

**O que significa**: alguém tentou criar ou atualizar um registro referenciando outra entidade (produto, centro, parceiro, etc.) **por um identificador que não existe** (ou que existe mas já foi excluído). É sempre lançado **antes** de qualquer gravação — o sistema nunca deixa uma referência quebrada entrar no banco de dados só pra falhar depois de forma mais confusa. Ação esperada de quem recebe esse erro: conferir o identificador enviado, provavelmente ele está errado, desatualizado, ou aponta pra um registro já excluído.

### "Not found" (genérico, sem nome de entidade)

**O que significa**: uma tentativa de buscar, atualizar ou excluir um registro específico (por exemplo, "atualize o item de id 42") não encontrou nada com esse identificador — o próprio recurso que a operação está tentando alterar não existe (diferente do erro acima, que é sobre uma *referência dentro* do payload).

### "Email already in use."

**O que significa**: alguém tentou criar um usuário com um e-mail que já pertence a outro usuário. A unicidade de e-mail é garantida pela regra de negócio, não é um bloqueio automático de banco de dados — então essa checagem sempre acontece antes de salvar.

### "Invalid credentials"

**O que significa**: uma tentativa de login falhou — e-mail não encontrado ou senha incorreta. **De propósito, a mensagem não diz qual dos dois foi o problema** — é uma decisão de segurança: revelar se o e-mail existe ou não facilita ataques de descoberta de contas.

### "Invalid refresh token."

**O que significa**: uma tentativa de renovar a sessão (trocar um token de acesso vencido por um novo, sem precisar logar de novo) usou um token de renovação que não existe, já expirou, ou já foi usado antes (tokens de renovação só podem ser usados uma vez — usar de novo um token já trocado é tratado como token inválido, mesma mensagem). Quem recebe esse erro deve pedir pro usuário logar de novo do zero.

### "There is already an active [buffer/zone/demand] adjustment factor for this product/center[/zone] in the given period."

**O que significa**: alguém tentou criar um Ajuste de Buffer, de Zona ou de Demanda cujo período de vigência **colide** com outro ajuste do mesmo tipo, já ativo, pro mesmo item (e, no caso de Zona, pra mesma zona específica). Explica a regra da seção 5 — dois ajustes conflitantes ativos ao mesmo tempo deixariam o cálculo ambíguo, então o segundo é recusado na criação. Ação esperada: desativar/encerrar o ajuste existente primeiro, ou ajustar as datas pra não sobrepor.

### "EndDate must be greater than or equal to StartDate." / "dateEnd must not be earlier than dateStart."

**O que significa**: um período informado (de uma previsão, ou de um filtro de relatório) tem a data final antes da data inicial — um intervalo de tempo que não faz sentido existir.

### "RedZoneBase, RedZoneSafe, YellowZone and GreenZone can only be edited when BufferType is ManualFixed."

**O que significa**: alguém tentou informar manualmente o tamanho de uma zona de buffer num item que **não** está configurado como "Manual Fixo". Só faz sentido informar zonas na mão quando o item explicitamente saiu do modo de cálculo automático — do contrário, o robô vai sobrescrever esse valor na próxima execução mesmo assim, então o sistema já barra a tentativa na hora, pra não dar a falsa impressão de que funcionou.

### "System reasons cannot be edited." / "System reasons cannot be deleted."

**O que significa**: um Motivo marcado como "do sistema" (ver seção 5) foi alvo de uma tentativa de edição ou exclusão pela aplicação — barrado porque esses registros são intocáveis por essa via, só podem mudar por uma ação administrativa direta fora do fluxo normal.

### "Only the user who created this note can edit it."

**O que significa**: um usuário tentou editar uma anotação (Note) que não foi criada por ele. Reforça a regra de propriedade de anotações da seção 5.

### "Unsupported calculation step '...'." / "Unsupported ingestion view '...'." / "Unsupported ingestion source type '...' for view '...'."

**O que significa**: a configuração do robô (o que ele deve carregar e calcular) referencia um nome de etapa de cálculo ou uma fonte de dados de carga que **não existe** no sistema — normalmente sinal de um erro de digitação ou de uma configuração desatualizada/incompleta no arquivo de configuração do cliente, não um problema com os dados em si.

### "No ingestion source configured for view '...'."

**O que significa**: foi pedido pra rodar a carga de uma fonte de dados específica, mas essa fonte simplesmente não está cadastrada na configuração do cliente.

### "View '...' declares '...' as key but has no fieldMapping targeting it." / "View '...' maps key field '...' to a constant (no 'source') — a join key can't be a fixed value shared by every row."

**O que significa**: erro de configuração da carga de dados — a "chave" declarada pra identificar/casar registros dessa fonte (ex.: código do produto) não está sendo de fato preenchida a partir do arquivo do cliente, ou está fixada num valor constante igual pra toda linha (o que faria todas as linhas do arquivo colidirem no mesmo registro). Detectado e barrado **antes** de tocar em qualquer dado, pra evitar uma carga silenciosamente errada.

### "CenterProduct not found." (ao rodar cálculo ou relatório escopado a um item específico)

**O que significa**: foi pedido pra recalcular ou consultar um único item de centro por identificador, mas esse identificador não corresponde a nenhum item existente.

Qualquer outra mensagem de erro de negócio segue o mesmo padrão: **texto direto em linguagem natural, dizendo exatamente qual validação falhou**, sempre verificado antes de qualquer gravação no sistema. Um erro que **não** segue esse padrão (uma mensagem genérica tipo "Internal server error") significa que algo deu errado de forma **inesperada** — não uma regra de negócio sendo aplicada, e sim uma falha real que precisa de investigação técnica.

---

## 7. Resumo da filosofia, em poucas frases

- **O robô nunca inventa dado** — ele só transforma o que foi carregado, através de fórmulas documentadas e estáveis.
- **O usuário nunca escreve por cima de um número calculado** — ele pode, no máximo, ajustar as entradas do cálculo (via os três tipos de ajuste, os toggles de configuração do item, ou o modo Manual Fixo), nunca o resultado.
- **Toda rejeição de negócio é explicada em linguagem natural**, nunca um erro técnico opaco.
- **Nada é definitivamente apagado** — tudo é reversível e rastreável.
- **O sistema prefere recalcular tudo de novo, todo dia, a manter estado incremental** — simplicidade e previsibilidade acima de performance, nesse ponto específico.
- **Zonas de buffer nunca ficam negativas e sempre arredondam pra cima** — o sistema erra sempre a favor de mais proteção de estoque, nunca menos.
- **Cada regra nova (fórmula, campo, validação) é documentada antes ou junto de ser implementada** — o objetivo declarado é que a documentação nunca fique desatualizada em relação ao comportamento real do sistema.
