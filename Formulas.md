# Formulas — Robot (cálculo DDMRP)

Lista centralizada das fórmulas usadas pelos steps de cálculo do Robot (`POST /api/calculation/run` / `POST /api/robot/run`). Cada step é uma classe C# em `Service.Infra.Data/Calculation/Steps/*.cs` (implementa `ICalculationStep`, tem acesso ao `ApplicationDbContext`/EF Core) — não são stored procedures no banco, então não precisam de migration pra "instalar"; mudar a fórmula é só editar o código. Toda fórmula nova deve ser documentada aqui antes (ou junto) de o step correspondente ser implementado.

**`POST /api/calculation/run` aceita um body opcional `{ "idCenterProduct": int }`** (2026-09-16) — quando enviado, todo step roda escopado a esse único `CenterProduct.Id` (reset + recálculo só daquela linha) em vez de todo `CenterProduct` ativo; sem body (ou `idCenterProduct` omitido/null), roda como sempre, pra todos. `CalculationService.RunAsync` valida que o id existe (`NotFoundException` se não) antes de rodar qualquer step. Cada `ICalculationStep.ExecuteAsync` recebe `idCenterProduct` como parâmetro e filtra sua própria SQL por `(@idCenterProduct IS NULL OR cp.Id = @idCenterProduct)` — nenhuma fórmula abaixo muda, só o escopo de linhas afetadas. `POST /api/robot/run` não aceita esse parâmetro (sempre roda ingestão completa + todos os steps sem escopo).

## Adu (`CenterProduct.Adu`)

Step: `Service.Infra.Data/Calculation/Steps/CalculateAduStandardDesvAndCvStep.cs` (nome no `calculation.config.json`: `"CalculateAduStandardDesvAndCv"`)

**Campos envolvidos**: `CenterProduct.Adu` (resultado), `CenterProduct.HistoryAduDays`, `CenterProduct.FutureAduDays`, `History.Consumption`/`Date`/`DiscardStatus`, `Forecast.Value`/`StartDate`/`EndDate`, `Calendar.Date`/`IsWorkingDay`.

### Qual fórmula usar (por `CenterProduct`)

| `HistoryAduDays` | `FutureAduDays` | Fórmula |
|---|---|---|
| > 0 | = 0 (ou nulo) | **Histórico** |
| = 0 (ou nulo) | > 0 | **Futuro** |
| > 0 | > 0 | **Misto** |
| = 0 (ou nulo) | = 0 (ou nulo) | `Adu` fica `NULL` (nada a calcular) |

### Histórico

```
Histórico = (Soma de History.Consumption dos últimos HistoryAduDays dias não descartados) / HistoryAduDays
```

- "Últimos N dias não descartados" caminha para trás a partir de ontem (hoje não entra), pulando qualquer dia cujo `History.DiscardStatus = Discarded` — **cada dia descartado estende a janela em mais um dia pra trás**, até completar `HistoryAduDays` dias válidos.
- O divisor é sempre `HistoryAduDays` (fixo), não a quantidade de linhas realmente somadas.
- Se não existirem `HistoryAduDays` dias válidos disponíveis no histórico (produto muito novo, por exemplo), soma o que houver disponível mesmo assim e divide por `HistoryAduDays` (janela incompleta, mesmo divisor fixo). Se não existir **nenhum** dia válido, o resultado é `0` (não `NULL`).

**Exemplo** (hoje = 12/09/2026, `HistoryAduDays` = 3):

| Data | Status |
|---|---|
| 11/09/2026 | Reviewed |
| 10/09/2026 | Discarded |
| 09/09/2026 | Reviewed |
| 08/09/2026 | Reviewed |

10/09 é pulado (descartado) e a janela estende até 08/09 pra completar 3 dias válidos → considera **11/09, 09/09 e 08/09**.

### Futuro

`Forecast` é cadastrado por intervalo (mensal, `StartDate`/`EndDate` + `Value` total), não um valor por dia — então primeiro ele é "aberto" dia a dia (ver [[project_forecast_daily_breakdown]] / `Service.Domain/Entities/Calendar.cs`): cada dia útil (conforme `Calendar.IsWorkingDay`, marcado diretamente por data) dentro do próprio `[StartDate, EndDate]` de um `Forecast` recebe uma fatia igual de `Value` — `Value / (quantidade de dias úteis nesse intervalo)`. Dias não úteis não recebem nada (não é um valor menor, é zero).

```
Futuro = (Soma do valor diário aberto de cada dia útil dos próximos FutureAduDays dias corridos) / FutureAduDays
```

- "Próximos N dias" = a partir de amanhã (hoje não entra), `FutureAduDays` dias **corridos** (incluindo dias não úteis, que apenas contribuem `0`) pra frente — o divisor continua sendo o total de dias corridos, não a quantidade de dias úteis na janela.
- Não existe descarte no Forecast (`DiscardStatus` é só de `History`) — todo dia corrido no intervalo entra, mesmo sem previsão cadastrada ou sendo dia não útil (conta como 0).
- Divisor sempre `FutureAduDays` (fixo).
- Implementado em `CalculateAduStandardDesvAndCvStep`'s `ForecastBusinessDays`/`FutureAdu` CTEs, lendo `dbo.Calendar.IsWorkingDay` diretamente — `Calendar` é uma tabela de referência com datas pré-geradas (5 anos, 2026-2030, seed manual via script SQL, não `HasData`).

### Misto

```
Misto = (Histórico + Futuro) / 2
```

- Sempre calculável quando `HistoryAduDays > 0` e `FutureAduDays > 0`: cada componente já vem `0` (não `NULL`) quando não há dados suficientes (ver regra do Histórico acima), então a média nunca fica bloqueada por falta de um dos dois.

### StandardDeviation e Cv (`CenterProduct.StandardDeviation`/`Cv`)

Calculados no **mesmo step** (`CalculateAduStandardDesvAndCvStep`), reaproveitando a mesma janela de dias válidos usada pelo Histórico (últimos `HistoryAduDays` dias não descartados, mesma regra de pular `Discarded` e estender a janela pra trás) — não participam do Histórico/Futuro/Misto, dependem só de `HistoryAduDays` (mesmo que `FutureAduDays` também esteja preenchido).

```
StandardDeviation = STDEVP(ISNULL(History.Consumption, 0)) dos dias do Histórico
Cv = StandardDeviation / AVG(ISNULL(History.Consumption, 0)) dos mesmos dias
```

- `STDEVP` é desvio padrão populacional (não amostral).
- Se `AVG(ISNULL(History.Consumption, 0)) = 0` (sem consumo nos dias considerados), `Cv = 0` em vez de dividir por zero.
- Mesma condição de aplicação do Histórico: só calculado quando `HistoryAduDays > 0`; caso contrário, `0`.

### Execução

- `CalculateAduStandardDesvAndCvStep` roda em lote (uma `UPDATE` set-based cobrindo todo `CenterProduct` ativo, executada via `ExecuteSqlRawAsync` dentro da classe C#), não em loop por linha — full recompute a cada execução, sem cálculo incremental (consistente com a regra geral do Robot).
- **Antes de calcular**, roda um `UPDATE` zerando `Adu`/`StandardDeviation`/`Cv` de todo `CenterProduct` ativo — convenção aplicada a toda coluna calculada do Robot (ver também `CalculateAdiStep`), pra garantir que nenhuma coluna fique com um valor de uma execução anterior caso a lógica de cálculo mude e deixe de cobrir algum caso.
- "Hoje" é `CAST(GETDATE() AS DATE)` — não é parametrizado.

## Adi (`CenterProduct.Adi`)

Step: `Service.Infra.Data/Calculation/Steps/CalculateAdiStep.cs` (nome no `calculation.config.json`: `"CalculateAdi"`)

**Campos envolvidos**: `CenterProduct.Adi` (resultado), `History.Consumption`/`Date` (todas as linhas contam, independente de `DiscardStatus`).

**Average Demand Interval** — mede o quão intermitente é a demanda: quanto maior o `Adi`, mais espaçadas as ocorrências de consumo.

```
Adi = (quantidade de linhas de History no período) / (quantidade dessas linhas com Consumption > 0)
```

- **Parâmetro `ThresholdDays`** (recebido pelo step via `calculation.config.json`, ex.: `{ "name": "ThresholdDays", "value": "360" }`, padrão `360` se omitido) — é **global pra execução inteira**, não um campo por `CenterProduct` como `HistoryAduDays`/`FutureAduDays`. Define o período: os últimos `ThresholdDays` dias corridos, terminando ontem (hoje não entra, mesma convenção do Adu).
- O numerador é a contagem real de linhas de `History` existentes no período — **não** o valor de `ThresholdDays` (se só existirem 200 linhas nos últimos 360 dias, numerador é 200, não 360).
- **Todas** as linhas contam, mesmo as com `DiscardStatus = Discarded` (diferente do Adu — aqui não há filtro de descarte).
- Se não existir nenhuma linha com `Consumption > 0` no período (incluindo o caso de não existir nenhuma linha de `History` no período), `Adi = 0` (não `NULL`).
- Não depende de `HistoryAduDays`/`FutureAduDays` — roda pra todo `CenterProduct` ativo que tenha (ou não) histórico no período.

### Execução

- `CalculateAdiStep` roda em lote (uma `UPDATE` set-based via `ExecuteSqlInterpolatedAsync`, que parametriza `ThresholdDays` com segurança em vez de concatenar a string), full recompute a cada execução.
- **Antes de calcular**, roda um `UPDATE` zerando `Adi` de todo `CenterProduct` ativo (mesma convenção do `CalculateAduStandardDesvAndCvStep`).
- "Hoje" é `CAST(GETDATE() AS DATE)`; a janela é `[hoje - ThresholdDays, hoje)`.

## BAF (`Service.Domain.Entities.BufferAdjustmentFactor`) → `CenterProduct.BufferType`/zonas

Step: `Service.Infra.Data/Calculation/Steps/ApplyBafStep.cs` (nome no `calculation.config.json`: `"ApplyBAF"`)

**Roda antes dos steps de zona** (`CalculateNormalBufferZones`/`CalculateMinMaxBufferZones`/`CalculateDynamicMinMaxBufferZones`) — precisa decidir o `BufferType` do dia antes deles, já que cada um filtra `CenterProduct` pelo `BufferType`. Não depende de `Adu`.

Três passos sequenciais, todos escopados por `(IdProduct, IdCenter)` casando `CenterProduct` com `BufferAdjustmentFactor`:

### Passo 1 — Reverter BAFs finalizados

Pra todo `BufferAdjustmentFactor` com `EffectiveTo < hoje` (já acabou) e `AlreadyReverted = false` (ainda não revertido):

```
CenterProduct.BufferType    = BAF.BufferTypeOld
CenterProduct.RedZoneSafe   = BAF.BufferDdmrpRedSafeOld
CenterProduct.RedZoneBase   = BAF.BufferDdmrpRedBaseOld
CenterProduct.YellowZone    = BAF.BufferDdmrpYellowOld
CenterProduct.GreenZone     = BAF.BufferDdmrpGreenOld
```

Só sobrescreve o campo se o `*Old` correspondente não for `NULL` (`ISNULL(BAF.*Old, CenterProduct.*)` — se o BAF nasceu sem `CenterProduct` pra tirar o snapshot, aquele campo específico fica como está, não é zerado). Depois de reverter, marca `BAF.AlreadyReverted = true` — pra nunca reverter de novo.

**Nota**: se o `BufferType` revertido for `Normal`/`MinMax`/`DynamicMinMax`, as zonas revertidas aqui são recalculadas do zero logo em seguida pelos steps de zona (full recompute sempre) — então reverter a zona só tem efeito prático duradouro quando o `BufferType` revertido é `ManualFixed` (que não tem step de recálculo).

### Passo 2 — `BufferType` segue o BAF ativo e vigente

Pra todo `BufferAdjustmentFactor` **ativo e vigente** (`IsActive = true` E `EffectiveFrom <= hoje` E `EffectiveTo >= hoje`, ambos os extremos incluídos):

```
CenterProduct.BufferType = BAF.BufferType
```

### Passo 3 — Zonas manuais direto do BAF

Pra todo `BufferAdjustmentFactor` ativo e vigente (mesmo critério do passo 2) **com `BufferType = ManualFixed`**:

```
CenterProduct.YellowZone   = BAF.BufferDdmrpYellow
CenterProduct.GreenZone    = BAF.BufferDdmrpGreen
CenterProduct.RedZoneSafe  = BAF.BufferDdmrpRed / 2
CenterProduct.RedZoneBase  = BAF.BufferDdmrpRed / 2
```

`BufferDdmrpRed` (um valor só) é dividido igualmente entre `RedZoneSafe`/`RedZoneBase`.

### Execução

- `ApplyBafStep` roda em lote (4 `UPDATE`s set-based via `ExecuteSqlRawAsync`, sem parâmetro externo), full recompute a cada execução.
- Sem escopo de `BufferType` na entrada — o próprio step é o que decide/muda o `BufferType`.
- **Mais de um `BufferAdjustmentFactor` casando pro mesmo `CenterProduct`** em qualquer um dos 3 passos não é tratado (a `UPDATE ... FROM ... JOIN` do SQL Server pega uma linha arbitrária) — mesma postura já adotada pro DAF/ZAF, depende da validação de overlap ainda não implementada (ver `TODO.md`).
- "Hoje" é `GETDATE()` (sem `CAST(... AS DATE)`), usado só nas comparações de vigência do BAF.

### Reversão imediata via API (fora do Robot)

Além do passo 1 do `ApplyBafStep` (que só reverte BAFs **já finalizados**, `EffectiveTo < hoje`), `BufferAdjustmentFactorService` também reverte na hora, fora do Robot, quando o usuário **exclui** (`DeleteAsync`) ou **desativa** (`SetActiveAsync(id, false)`) um BAF que está `IsActive = true` **e dentro do período de vigência** (`EffectiveFrom <= agora <= EffectiveTo`) **e** ainda não revertido (`AlreadyReverted = false`) — mesma lógica de reversão (campo a campo, só sobrescreve se o `*Old` não for `NULL`), aplicada direto em C# contra o `CenterProduct` (não é SQL bruto, é a mesma regra reimplementada no service). Ao **reativar** (`SetActiveAsync(id, true)`), `AlreadyReverted` sempre volta pra `false`, sem checar vigência — pra que uma desativação/expiração futura possa reverter de novo. Usa `DateTime.Now` (não `UtcNow`) pra bater com o `GETDATE()` do `ApplyBafStep`.

## Buffer Ddmrp zonas normal (`CenterProduct.RedZoneBase`/`RedZoneSafe`/`YellowZone`/`GreenZone`)

Step: `Service.Infra.Data/Calculation/Steps/CalculateNormalBufferZonesStep.cs` (nome no `calculation.config.json`: `"CalculateNormalBufferZones"`)

**Campos envolvidos**: `CenterProduct.RedZoneBase`/`RedZoneSafe`/`YellowZone`/`GreenZone` (resultado; `RedZone` é a propriedade calculada `RedZoneBase + RedZoneSafe`, nunca gravada diretamente — ver `CLAUDE.md`), `CenterProduct.Adu`/`LeadTime`/`Frequency`/`Moq`/`BufferType`/`UseSuggestedLTFactor`/`UseSuggestedVariabilityFactor`/`UseDafOnGreenZone`/`CustomLeadTimeFactor`/`CustomVariabilityFactor`/`GreenZoneParametrizationUseMoq`/`GreenZoneParametrizationUseAduXFrequency`/`GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime`, `BufferProfile.LeadTimeFactor`/`VariabilityFactor`, `DemandAdjustmentFactor.IsActive`/`EffectiveFrom`/`EffectiveTo`/`AdjustmentType`/`AdjustmentValue`.

**Escopo: só `CenterProduct.BufferType = 0` (Normal)**. `ManualFixed`/`MinMax`/`DynamicMinMax` não são tocados por esse step — `ManualFixed` tem suas zonas definidas pelo `ApplyBafStep` (ver seção "BAF" acima), `MinMax`/`DynamicMinMax` têm os próprios steps (`CalculateMinMaxBufferZonesStep`/`CalculateDynamicMinMaxBufferZonesStep`, abaixo). **Depende de `CenterProduct.Adu` já calculado** — por isso roda depois de `CalculateAduStandardDesvAndCv` em `calculation.config.json`. **Não trata itens MTO ainda** — ver `TODO.md`.

### Adjusted Adu (fator DAF aplicado ao Adu)

```
DAF ativo = DemandAdjustmentFactor com IsActive = true E EffectiveFrom <= hoje E EffectiveTo >= hoje,
            pro mesmo (IdProduct, IdCenter)

AdjustedAdu = Adu,                                    se não houver DAF ativo
            = DAF.AdjustmentValue + Adu,               se DAF.AdjustmentType = FlatValue
            = DAF.AdjustmentValue * Adu,                se DAF.AdjustmentType = Percentage
```

- Mais de um DAF ativo simultâneo pro mesmo item não é tratado (a query assume unicidade) — depende da validação de overlap de período ainda não implementada (ver `TODO.md`, "Ajustes PAF... não impedem períodos sobrepostos").

> **Importante**: DAF (`DemandAdjustmentFactor`) só afeta o cálculo de zonas do `Normal` e do `MinMax` (via `AdjustedAdu`, acima) — **`DynamicMinMax` não usa DAF em nenhum lugar da sua fórmula** (usa `Adu` puro). Não adicionar `AdjustedAdu`/lookup de DAF no step de `DynamicMinMax` sem confirmar antes — foi decisão explícita, não esquecimento.

### Yellow

```
Yellow = AdjustedAdu * LeadTime
```

### Green

```
Green = MAX(GreenCandidate1, GreenCandidate2, GreenCandidate3)
```

Cada candidato é zerado (não excluído do `MAX`, substituído por `0`) quando seu toggle correspondente em `CenterProduct` está desligado:

```
GreenCandidate1 = Moq,                                                                        se GreenZoneParametrizationUseMoq = true, senão 0
GreenCandidate2 = Adu * LeadTime * LeadTimeFactor,                                             se GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime = true, senão 0
GreenCandidate3 = Frequency * (AdjustedAdu, se UseDafOnGreenZone = true, senão Adu),            se GreenZoneParametrizationUseAduXFrequency = true, senão 0
```

- `LeadTimeFactor` em `GreenCandidate2` é `BufferProfile.LeadTimeFactor` quando `UseSuggestedLTFactor = true`, senão `CenterProduct.CustomLeadTimeFactor`.

### RedZoneSafe / RedZoneBase

```
RedZoneSafe = AdjustedAdu * LeadTime * LeadTimeFactor
RedZoneBase = AdjustedAdu * LeadTime * LeadTimeFactor * VariabilityFactor
```

- `LeadTimeFactor`: `BufferProfile.LeadTimeFactor` se `UseSuggestedLTFactor = true`, senão `CenterProduct.CustomLeadTimeFactor`.
- `VariabilityFactor`: `BufferProfile.VariabilityFactor` se `UseSuggestedVariabilityFactor = true`, senão `CenterProduct.CustomVariabilityFactor`.
- `RedZone` (a propriedade calculada) = `RedZoneSafe + RedZoneBase`.

> **Importante (2026-09-13)**: nenhuma zona pode ficar negativa — toda zona é sempre `MAX(valor calculado, 0)`. Vale pros 4 valores (`YellowZone`/`GreenZone`/`RedZoneSafe`/`RedZoneBase`) nos **3 steps** de cálculo de zona (`Normal`, `MinMax`, `DynamicMinMax`) — cada um clampa seu próprio resultado antes de gravar.
>
> **Revogado (2026-09-20)** — a regra abaixo ("arredondamento para cima sempre", 2026-09-14) não vale mais. Nenhum step de zona, `ApplyBafStep` ou `ApplyZafStep` arredonda mais o valor gravado — fica cru (`HasPrecision(18,4)`). Arredondamento agora é só de exibição, só em `GetInventoryBufferManagementQueryable` (ver a seção "CenterProduct — zonas derivadas" acima). Texto original mantido como histórico: ~~todo cálculo de zona **sempre arredonda pra cima** ("arredondamento para cima sempre") — o valor final (já clampado em `0` se negativo) passa por `CEILING()` antes de ser gravado, nos mesmos 3 steps de zona-base, em `ApplyBafStep` (o split `RedZoneSafe`/`RedZoneBase = CEILING(BufferDdmrpRed / 2)` de `ManualFixed`) e em `ApplyZafStep` (a zona final depois de somar o delta: `GreenZone = CEILING(GreenZone + Delta)`, mesma coisa pra `YellowZone`/`RedZoneBase`)~~.

### Execução

- `CalculateNormalBufferZonesStep` roda em lote (uma `UPDATE` set-based via `ExecuteSqlRawAsync`, sem parâmetro externo — nada a parametrizar), full recompute a cada execução, escopado a `BufferType = 0`.
- **Antes de calcular**, roda um `UPDATE` zerando `YellowZone`/`GreenZone`/`RedZoneSafe`/`RedZoneBase` de todo `CenterProduct` ativo com `BufferType = 0` (mesma convenção dos outros steps — escopada ao `BufferType` pra nunca tocar itens de outro tipo).
- "Hoje" é `GETDATE()` (sem `CAST(... AS DATE)`, diferente do Adu/Adi — usado apenas na comparação de vigência do DAF).

## Buffer Ddmrp zonas MinMax (`CenterProduct.RedZoneBase`/`RedZoneSafe`/`YellowZone`/`GreenZone`)

Step: `Service.Infra.Data/Calculation/Steps/CalculateMinMaxBufferZonesStep.cs` (nome no `calculation.config.json`: `"CalculateMinMaxBufferZones"`)

**Campos envolvidos**: mesmos do Normal, mais `History.Consumption`/`Date`/`DiscardStatus` (janela de `ThresholdDays` dias).

**Escopo: só `CenterProduct.BufferType = 2` (MinMax)**. **Não trata itens MTO** — mesma ressalva dos outros steps de zona, ver `TODO.md`.

```
Yellow    = 0
Green     = mesma fórmula do Green do Normal (MAX de GreenCandidate1/2/3, mesmos toggles
            GreenZoneParametrizationUse*, mesmo AdjustedAdu via DAF pro GreenCandidate3)
RedSafe   = 0
RedBase   = MAX(History.Consumption) nos últimos ThresholdDays dias corridos, terminando ontem
            (hoje não entra), excluindo linhas com DiscardStatus = Discarded — 0 se não houver
            nenhuma linha de History no período
```

- **Parâmetro `ThresholdDays`** (`calculation.config.json`, ex.: `{ "name": "ThresholdDays", "value": "180" }`, padrão `180` se omitido) — mesma mecânica do `CalculateAdiStep` (`ExecuteSqlInterpolatedAsync`, parametrizado com segurança), mas com default diferente (`180`, não `360`).
- Janela `[hoje - ThresholdDays, hoje)`, mesma convenção do Adu/Adi (dias corridos, hoje não conta).
- `RedBase` é o "maior consumo diário" da janela — não uma média nem soma; um único dia de pico define o valor.
- Zonas nunca negativas (`MAX(valor, 0)`) — ver nota importante na seção do Normal.

### Execução

- `CalculateMinMaxBufferZonesStep` roda em lote (`UPDATE`s set-based via `ExecuteSqlInterpolatedAsync`), full recompute a cada execução, escopado a `BufferType = 2`.
- **Antes de calcular**, roda um `UPDATE` zerando `YellowZone`/`GreenZone`/`RedZoneSafe`/`RedZoneBase` de todo `CenterProduct` ativo com `BufferType = 2`.

## Buffer Ddmrp zonas MinMax Dinâmico (`CenterProduct.RedZoneBase`/`RedZoneSafe`/`YellowZone`/`GreenZone`)

Step: `Service.Infra.Data/Calculation/Steps/CalculateDynamicMinMaxBufferZonesStep.cs` (nome no `calculation.config.json`: `"CalculateDynamicMinMaxBufferZones"`)

**Campos envolvidos**: mesmos do MinMax, mais `History.Consumption`/`Date`/`DiscardStatus` (janela dinâmica, sem parâmetro de config — usa `CenterProduct.HistoryAduDays` diretamente).

**Escopo: só `CenterProduct.BufferType = 3` (DynamicMinMax)**. **Não usa DAF em nenhum lugar** — `Adu` puro, sem `AdjustedAdu`/lookup de `DemandAdjustmentFactor` (ver a nota importante na seção do Normal). **Não trata itens MTO** — mesma ressalva dos outros steps de zona.

### Janela grande (`WindowStart`)

Mesmo mecanismo do Histórico do Adu — rankeia os dias de `History` **não descartados** por recência (`DiscardStatus <> 2`) e pega a data do `HistoryAduDays`-ésimo mais recente:

```
WindowStart = data do dia válido (não descartado) de rank HistoryAduDays,
              contando do mais recente pro mais antigo

Se existirem menos de HistoryAduDays dias válidos no total, usa o mais antigo disponível
(mesmo efeito prático — pega os que existirem).
```

O intervalo `[WindowStart, ontem]` automaticamente cobre `HistoryAduDays` dias válidos + quantos dias descartados caírem no meio — sem precisar calcular a extensão manualmente. **Diferente do Histórico do Adu, aqui o dia descartado não é removido do intervalo** — ele ocupa seu lugar no calendário normalmente, só não conta na soma (ver abaixo), o que na prática dá no mesmo que excluí-lo de qualquer `SUM`.

### Maior Acumulado (`MaiorAcumulado`)

Pra cada dia `d` dentro de `[WindowStart, ontem]`, soma `History.Consumption` não descartada entre `d - K` e `d` (ambos os extremos incluídos, `K+1` dias no total), onde `K = MAX(LeadTime, Frequency)`. `MaiorAcumulado` é o maior valor entre todas essas somas.

```
K = MAX(LeadTime, Frequency)

Para cada dia d em [WindowStart, ontem]:
    RollingSum(d) = SOMA(History.Consumption não descartada) para History.Date em [d - K, d]

MaiorAcumulado = MAX(RollingSum(d)) para todo d em [WindowStart, ontem]
                 = 0, se não houver nenhum dia de History no período (sem WindowStart)
```

**Importante — a janela de `RollingSum` NÃO é limitada a `WindowStart`**: pra dias `d` perto do início do intervalo grande (inclusive `d = WindowStart`), `d - K` pode cair **antes** de `WindowStart` — e isso é esperado, lê `History` normalmente nesse caso. Só o `d` em si (o dia sendo avaliado pro `MAX`) fica restrito a `[WindowStart, ontem]`; a janela de soma de cada `d` é livre. Uma versão anterior dessa query limitava (`MAX(WindowStart, d - K)`) — isso estava **errado**, corrigido 2026-09-13 após um exemplo validado pegar o bug (ver abaixo).

**Exemplo validado #1 (2026-09-13)**, `K = 3`, `HistoryAduDays = 5`, um dia descartado (9/10) dentro da janela:

| Data | Descartado | Consumo | `RollingSum` (janela de 4 dias `[d-3,d]`) |
|---|---|---|---|
| 6/10 | não | 0 | 3/10–6/10 = 10+80+20+0 = **110** |
| 7/10 | não | 30 | 4/10–7/10 = 80+20+0+30 = **130** |
| 8/10 | não | 0 | 5/10–8/10 = 20+0+30+0 = **50** |
| 9/10 | **sim** | 20 (ignorado) | 6/10–9/10 = 0+30+0+**0** = **30** |
| 10/10 | não | 100 | 7/10–10/10 = 30+0+**0**+100 = **130** |
| 11/10 | não | 50 | 8/10–11/10 = 0+**0**+100+50 = **150** |

`MaiorAcumulado = MAX(110,130,50,30,130,150) = 150`.

**Exemplo validado #2 (2026-09-13)**, `K = 5`, `HistoryAduDays = 4`, sem dias descartados — pega especificamente o caso de fronteira (`d = WindowStart`), onde `d - K` cai antes de `WindowStart`:

`WindowStart = 09-09` (os 4 dias mais recentes: 09, 10, 11, 12). Consumo: 08-28→89, 08-30→52, 09-01→92, 09-02→94, 09-04→64, 09-06→46, 09-07→60, 09-08→29, 09-09→48, 09-10→0, 09-11→38, 09-12→12 (dias sem linha = 0).

| Data (`d`) | `RollingSum` (janela de 6 dias `[d-5,d]`) |
|---|---|
| 09-09 (`= WindowStart`) | 09-04–09-09 = 64+0+46+60+29+48 = **247** |
| 09-10 | 09-05–09-10 = 0+46+60+29+48+0 = **183** |
| 09-11 | 09-06–09-11 = 46+60+29+48+0+38 = **221** |
| 09-12 | 09-07–09-12 = 60+29+48+0+38+12 = **187** |

`MaiorAcumulado = MAX(247,183,221,187) = 247`. Se a janela de `09-09` fosse limitada a `WindowStart` (bug antigo), daria `48` (só o próprio dia) em vez de `247`.

### Zonas

```
RedSafe  = 0
Yellow   = 0,                              se MaiorAcumulado = 0
         = LeadTime * Adu,                  senão
Green    = 0,                              se MaiorAcumulado = 0
         = Moq,                             senão
RedBase  = 0,                              se MaiorAcumulado = 0
         = MaiorAcumulado - (LeadTime * Adu), senão
```

- Zonas nunca negativas (`MAX(valor, 0)`) — ver nota importante na seção do Normal. Relevante em especial pro `RedBase`: se `LeadTime * Adu` for maior que `MaiorAcumulado`, o resultado natural seria negativo — vira `0`.

### Execução

- Implementação em T-SQL via CTE recursiva (`DateSpine`, gerando um dia de calendário por vez de `WindowStart` até ontem, por `CenterProduct`) + `CROSS APPLY` (subquery correlacionada por dia, somando `History.Consumption` na janela `[d-K,d]`) — **não dá pra usar `SUM() OVER (... ROWS BETWEEN N PRECEDING ...)`** porque o SQL Server exige que `N` seja uma constante no frame da window function, e aqui `K` varia por `CenterProduct` (`MAX(LeadTime, Frequency)`).
- `OPTION (MAXRECURSION 0)` é obrigatório na `UPDATE` final — a CTE recursiva por padrão trava em 100 níveis, e a janela pode facilmente passar disso (`HistoryAduDays` grande + dias descartados).
- **Antes de calcular**, roda um `UPDATE` zerando `YellowZone`/`GreenZone`/`RedZoneSafe`/`RedZoneBase` de todo `CenterProduct` ativo com `BufferType = 3`. `RedZoneSafe` nunca é escrito de novo depois (fica sempre `0`, por fórmula).
- Sem parâmetro externo — `HistoryAduDays` já é uma coluna por `CenterProduct` (mesma usada pelo Adu), não precisa de config.

## CenterProduct — zonas derivadas (Top/Execução/Analítica)

Nenhuma dessas é um calculation step — todas são propriedades C# computadas (`get`-only, `Ignore()`'d no EF em `CenterProductConfiguration`, nunca uma coluna física, sempre nullable-lifted: ficam `null` a menos que **todas** as parcelas envolvidas estejam setadas, nunca `0` por padrão). Nenhum step escreve essas colunas diretamente — elas só leem `RedZoneBase`/`RedZoneSafe`/`YellowZone`/`GreenZone` (essas sim escritas pelos steps de zona/ZAF, ver seções acima) e são lidas onde precisar (`CenterProductGetDto`, `InventoryBufferManagementRow`, `ExecutionBuffer` acima).

Fórmulas cruas, sem arredondamento (ver nota "Arredondamento passou a ser só de exibição" logo abaixo — `GetInventoryBufferManagementQueryable` é o único lugar que ainda envolve cada uma destas em `CEILING`/`Math.Ceiling`, só pra exibição):

```
RedZone               = RedZoneBase + RedZoneSafe

TopOfRed              = RedZoneBase + RedZoneSafe                            (= RedZone, nome DDMRP-padrão separado por clareza)
TopOfYellow           = RedZoneBase + RedZoneSafe + YellowZone
TopOfGreen            = RedZoneBase + RedZoneSafe + YellowZone + GreenZone      (ponto de reordem — topo do buffer inteiro)

RedZoneExecution      = TopOfRed / 2
YellowZoneExecution   = TopOfRed / 2                                         (idêntico a RedZoneExecution — é a especificação dada, não é erro)
GreenZoneExecution    = YellowZone                                           (igual à coluna YellowZone pura, não a TopOfYellow)

TopOfRedExecution     = RedZoneExecution
TopOfYellowExecution  = RedZoneExecution + YellowZoneExecution
TopOfGreenExecution   = RedZoneExecution + YellowZoneExecution + GreenZoneExecution

RedSafeAnalytical       = RedZone / 2
YellowSafeAnalytical    = RedZone / 2                                       (igual a RedSafeAnalytical — mesma "duplicata literal" de RedZoneExecution/YellowZoneExecution)
GreenAnalytical         = GreenZone                                         (GreenZone puro, não soma RedZone)

YellowExcessAnalytical  = GreenZone >= YellowZone ? 0 : YellowZone - GreenZone

RedExcessAnalytical     = TopOfGreen <= 0 ? 0
                            : TopOfGreen - (RedZone + GreenZone + YellowExcessAnalytical)
                          (equivale a MIN(YellowZone, GreenZone) quando TopOfGreen > 0 — TopOfGreen - RedZone - GreenZone = YellowZone,
                           e subtrair YellowExcessAnalytical remove o excedente de Yellow sobre Green quando houver)
```

**`UtilsDdmrp.CalculateAnaliticalZone(decimal redZone, decimal yellowZone, decimal greenZone)`** implementa as 5 fórmulas acima num único método tuple-returning (`(redSafeAnalytical, yellowSafeAnalytical, greenAnalytical, yellowExcessAnalytical, redExcessAnalytical)`), mesmo padrão de `CalculateExecutionZone`/`CalculateNetflowTops` — calcula `TopOfGreen` internamente como `redZone + yellowZone + greenZone` (não recebe um parâmetro separado). Usar esse método sempre que possível em código C# pós-materialização que já tem `redZone`/`yellowZone`/`greenZone` como variáveis soltas (não vindas de uma instância de `CenterProduct`), como `GetAccumulatedBufferHistoryAsync` (alimentado por `NetflowRedZone`/`NetflowYellowZone`/`NetflowGreenZone`). **Não** chamável dentro de `GetInventoryBufferManagementQueryable` (precisa permanecer uma única cadeia `IQueryable` pra composição OData — ver a nota de pitfall mais abaixo), onde as mesmas 5 fórmulas continuam duplicadas inline (e `RedExcessAnalytical`, por depender de `YellowExcessAnalytical` já calculado, ainda exige um estágio `.Select()` a mais — `YellowExcessAnalytical` em `withExecutionZones`, `RedExcessAnalytical` só no estágio seguinte, `withExecutionTops`, onde `TopOfGreen` e `YellowExcessAnalytical` já estão disponíveis); nem faz sentido nas próprias propriedades computadas de `CenterProduct` (que já expõem cada campo individualmente, sem instanciar tupla) — mesmo precedente de `TopOfRed`/`RedZoneExecution`/etc., que também têm equivalentes em `UtilsDdmrp` mas a entidade não os chama.

**Histórico de correções, todas em 2026-09-20** (fórmulas anteriores, incorretas/provisórias, substituídas pelas de cima): (1) `YellowSafeAnalytical` era `RedZone` puro, corrigido pra `RedZone / 2`; (2) `GreenAnalytical` no `ReportRepository` (`GetInventoryBufferManagementQueryable`'s `withExecutionZones`, e também em `AccumulatedBufferHistoryRow`) calculava `RedZone + GreenZone`, corrigido pra `GreenZone` puro, igual à entidade; (3) `YellowExcessAnalytical` tinha o termo `RedZone` nos dois lados da comparação (cancelava matematicamente, mas ficou removido da fórmula por clareza); (4) `RedExcessAnalytical` foi brevemente simplificado pro valor `GreenZone` puro (eliminando a dependência de `TopOfGreen`/`YellowExcessAnalytical`), depois revertido no mesmo dia de volta pra `TopOfGreen <= 0 ? 0 : TopOfGreen - (RedZone + GreenZone + YellowExcessAnalytical)` — a fórmula de cima é a definitiva.

**Arredondamento passou a ser só de exibição** (2026-09-20, revogando a regra "arredondamento para cima sempre" de 2026-09-14 abaixo): nenhum calculation step nem nenhuma propriedade computada de `CenterProduct` arredonda mais — `RedZoneBase`/`RedZoneSafe`/`YellowZone`/`GreenZone` e todas as derivadas (`RedZone`, `TopOfRed`/`TopOfYellow`/`TopOfGreen`, `*Execution`, `*Analytical`) ficam com o valor decimal cru (`HasPrecision(18,4)`). O único lugar que ainda arredonda pra cima é `GetInventoryBufferManagementQueryable` (reimplementação inline das mesmas fórmulas, por causa da composição OData — ver nota mais abaixo) — `withZoneTops`/`withExecutionZones`/`withExecutionTops` continuam envolvendo cada resultado em `Math.Ceiling`, propositalmente, só pra exibição nesse relatório. `ComputeBufferColors` (usado por `bufferPenetration`/`itemsByBufferColorHistory`) e `GetAccumulatedBufferHistoryAsync` (`accumulatedBufferHistory`) foram ajustados pra não arredondar mais, alinhados com a entidade. `ReportRepositoryTests.GetInventoryBufferManagementQueryable_RoundsDerivedZonesUp` agora compara `row.X` contra valores arredondados fixos (não mais contra `centerProduct.X`, que ficou cru) — a divergência entre entidade (crua) e esse report (arredondado) é esperada e intencional, não deve ser "corrigida".

**Campos envolvidos**: `CenterProduct.RedZoneBase`/`RedZoneSafe`/`YellowZone`/`GreenZone` (as únicas colunas físicas da cadeia — tudo mais deriva delas, agora sem arredondamento em nenhum step/ZAF/BAF, ver seções acima). `TopOfYellowExecution` é o denominador de `ExecutionBuffer` (acima), agora cru. As colunas `*Analytical` ainda não alimentam nenhum relatório/cálculo além de aparecerem cruas em `CenterProductGetDto`/`InventoryBufferManagementRow` — reservadas pra leitura "Buffer Analítica" ainda não escopada (ver `TODO.md`).

### Topos e cor da zona analítica (`TopOf*Analytical`, `AnalyticalBufferColor`) — adicionado 2026-09-20

Mesmo padrão de `TopOfRed`/`TopOfYellow`/`TopOfGreen` (soma acumulada de baixo pra cima) e de `NetflowBufferColor`/`ExecutionBufferColor` (classificação por faixa), só que em cima das 5 zonas analíticas em vez das 3 zonas normais/de execução:

```
TopOfRedSafeAnalytical      = RedSafeAnalytical
TopOfYellowSafeAnalytical   = RedSafeAnalytical + YellowSafeAnalytical
TopOfGreenAnalytical        = RedSafeAnalytical + YellowSafeAnalytical + GreenAnalytical
TopOfYellowExcessAnalytical = RedSafeAnalytical + YellowSafeAnalytical + GreenAnalytical + YellowExcessAnalytical
TopOfRedExcessAnalytical    = RedSafeAnalytical + YellowSafeAnalytical + GreenAnalytical + YellowExcessAnalytical + RedExcessAnalytical
```

**`UtilsDdmrp.CalculateAnalyticalTops(redSafeAnalytical, yellowSafeAnalytical, greenAnalytical, yellowExcessAnalytical, redExcessAnalytical)`** implementa as 5 fórmulas acima num único método tuple-returning (`(topOfRedSafeAnalytical, topOfYellowSafeAnalytical, topOfGreenAnalytical, topOfYellowExcessAnalytical, topOfRedExcessAnalytical)`), mesmo padrão de `CalculateNetflowTops`/`CalculateExecutionTops`.

**`UtilsDdmrp.CalculateAnalyticalBufferColor(decimal stock, decimal topOfRedSafeAnalytical, decimal topOfYellowSafeAnalytical, decimal topOfGreenAnalytical, decimal topOfYellowExcessAnalytical, decimal topOfRedExcessAnalytical)`** classifica `stock` (mesmo termo usado por `ExecutionBufferColor`, não o `Netflow`) contra os 5 topos, retornando `Service.Domain.Enums.AnalyticalBufferColor` (`RedSafe`/`YellowSafe`/`Green`/`YellowExcess`/`RedExcess`/`Blue`/`NoColor`/`Black` — enum novo, não reaproveita `BufferColor`, já que tem faixas diferentes): `NoColor` quando `TopOfRedExcessAnalytical = 0` (buffer analítico ainda não computado — checado primeiro, antes de qualquer outra condição), `Black` quando `stock <= 0` (ruptura, mesmo boundary `<=` de `CalculateBufferColor`, adicionado 2026-09-20 — antes disso essa faixa também caía em `NoColor`, sem distinguir "sem buffer" de "ruptura"), senão `RedSafe`/`YellowSafe`/`Green`/`YellowExcess`/`RedExcess` conforme a primeira faixa (`<=`) que `stock` alcançar, e `Blue` acima de `TopOfRedExcessAnalytical` (excesso além de todas as zonas analíticas).

**Report `inventoryBufferManagement`**: exposto como `TopOfRedSafeAnalytical`/`TopOfYellowSafeAnalytical`/`TopOfGreenAnalytical`/`TopOfYellowExcessAnalytical`/`TopOfRedExcessAnalytical` (arredondados pra cima, mesma convenção de exibição de `TopOfRed`/`TopOfYellowExecution`/etc. — ver "Arredondamento passou a ser só de exibição" acima) e `AnalyticalBufferColor`, todos calculados inline em `ReportRepository.GetInventoryBufferManagementQueryable` a partir dos valores crus (`RedSafeAnalytical`/etc., já existentes na cadeia `withExecutionZones`/`withExecutionTops`) — mesma convenção de expressão SQL-translatável do resto do método, não uma chamada a `UtilsDdmrp` pós-materialização (mesma razão de sempre: precisa continuar uma única cadeia `IQueryable` pra composição OData). `TopOfRedExcessAnalytical` só pode ser calculado no estágio seguinte a `withExecutionTops` (`withNetflow`), já que depende de `RedExcessAnalytical`, que é uma propriedade irmã calculada só no fim de `withExecutionTops` (não dá pra referenciar dentro do mesmo `Select`, mesma limitação documentada em `RedExcessAnalytical`/`YellowExcessAnalytical` acima).

**Resumo por cor** (`GET /api/report/inventoryBufferManagement/colorSummary`): `InventoryBufferManagementColorSummaryResult.Analytical` (lista de `AnalyticalBufferColorSummaryRow { Color, Count }`, novo — `BufferColorSummaryRow` não serve porque `Color` lá é `BufferColor`, não `AnalyticalBufferColor`), populado agrupando a mesma `IQueryable` recebida por `SummarizeInventoryBufferManagementByColorAsync` por `AnalyticalBufferColor`, ao lado de `Netflow`/`Execution` (mesmos já existentes).

## ZAF — ajuste de zona (`CenterProduct.ZafRedZone`/`ZafYellowZone`/`ZafGreenZone`)

Step: `Service.Infra.Data/Calculation/Steps/ApplyZafStep.cs` (nome no `calculation.config.json`: `"ApplyZAF"`)

**Campos envolvidos**: `CenterProduct.ZafRedZone`/`ZafYellowZone`/`ZafGreenZone` (resultado — só o delta do ajuste, nunca base+ajuste), `CenterProduct.RedZoneBase`/`RedZoneSafe`/`YellowZone`/`GreenZone` (lidos E atualizados), `ZoneAdjustmentFactor.TargetZone`/`AdjustmentType`/`AdjustmentValue`/`IsActive`/`EffectiveFrom`/`EffectiveTo`.

**Escopo: `CenterProduct.BufferType <> 1` (todos menos `ManualFixed`)** — `Normal`/`MinMax`/`DynamicMinMax` já têm zona calculada (por `CalculateNormalBufferZonesStep`/`CalculateMinMaxBufferZonesStep`/`CalculateDynamicMinMaxBufferZonesStep`, que precisam ter rodado antes na mesma execução). `ManualFixed` fica de fora de propósito — não recebe ZAF. **Não trata itens MTO** — mesma ressalva dos steps anteriores, ver `TODO.md`.

Um `ZoneAdjustmentFactor` (ZAF) ativo e vigente (`IsActive = true` E `EffectiveFrom <= hoje` E `EffectiveTo >= hoje`) tem um `TargetZone` (`RedZone`/`YellowZone`/`GreenZone`) — só afeta a zona que ele mira, e cada zona é resolvida contra um ZAF diferente (um item pode ter até 3 ZAFs ativos simultâneos, um por zona; mais de um ZAF ativo pra **mesma** zona não é tratado — mesma ressalva do DAF, depende da validação de overlap ainda não implementada, ver `TODO.md`).

### Delta do ZAF (o que vai pra `ZafRedZone`/`ZafYellowZone`/`ZafGreenZone`)

```
ZafGreenZone  = 0,                                              se não houver ZAF ativo com TargetZone = GreenZone
              = AdjustmentValue,                                 se houver e AdjustmentType = FlatValue
              = GreenZone * AdjustmentValue,                     se houver e AdjustmentType = Percentage

ZafYellowZone = mesma regra, trocando GreenZone por YellowZone e TargetZone = YellowZone

ZafRedZone    = 0,                                                                  se não houver ZAF ativo com TargetZone = RedZone
              = AdjustmentValue,                                                    se houver e AdjustmentType = FlatValue
              = (RedZoneSafe + RedZoneBase) * AdjustmentValue,                      se houver e AdjustmentType = Percentage
```

- **Importante**: essas colunas guardam só o **delta** do ajuste (nunca a zona já somada) — decisão explícita (2026-09-13), pra não duplicar a base quando o delta for somado na zona de verdade logo depois.

### Aplicação (soma o delta na zona de verdade)

```
GreenZone     = GreenZone + ZafGreenZone
YellowZone    = YellowZone + ZafYellowZone
RedZoneBase   = RedZoneBase + ZafRedZone
```

- Sem arredondamento (2026-09-20 — antes era `CEILING(...)` no valor final; ver a nota "Arredondamento passou a ser só de exibição" na seção de zonas derivadas acima).

- O delta do vermelho é somado em `RedZoneBase`, não em `RedZoneSafe` — decisão explícita, não inferida.
- `RedZone` (propriedade calculada `RedZoneSafe + RedZoneBase`) reflete o ajuste automaticamente, já que `RedZoneBase` foi incrementado.

### Execução

- `ApplyZafStep` roda em lote (`UPDATE`s set-based via `ExecuteSqlRawAsync`, sem parâmetro externo), full recompute a cada execução, escopado a `BufferType <> 1`.
- **Antes de calcular**, roda um `UPDATE` zerando `ZafRedZone`/`ZafYellowZone`/`ZafGreenZone` de **todo** `CenterProduct` ativo, sem escopo de `BufferType` — inclui `ManualFixed`, que nunca recebe um delta real depois (fica sempre `0`).
- Depende de rodar **depois** de `CalculateNormalBufferZonesStep`, `CalculateMinMaxBufferZonesStep` e `CalculateDynamicMinMaxBufferZonesStep` na mesma execução — como o pipeline inteiro roda full recompute diário (sem incremental), `GreenZone`/`YellowZone`/`RedZoneBase`/`RedZoneSafe` já estão "limpos" (recalculados do zero) antes do ZAF ser somado, então não há risco de acumular o mesmo ajuste em execuções sucessivas.
- "Hoje" é `GETDATE()`, usado só na comparação de vigência do ZAF.

## Demanda Qualificada (`CenterProduct.QualifiedDemand`)

Step: `Service.Infra.Data/Calculation/Steps/CalculateQualifiedDemandStep.cs` (nome no `calculation.config.json`: `"CalculateQualifiedDemand"`).

**Campos envolvidos**: `CenterProduct.QualifiedDemand` (resultado), `CenterProduct.LeadTime`/`Adu`/`RedZoneBase`/`RedZoneSafe`, `CenterProduct.SpikeHorizonType`/`SpikeHorizonValue`/`SpikeHorizonLTDays`, `CenterProduct.SpikeThresholdType`/`SpikeThresholdAdu`/`SpikeThresholdPercentageRedZone`, `Order.IdProduct`/`IdOriginCenter`/`IsOutbound`/`Quantity`/`DeliveredQuantity`/`DeliveryDate`.

**Sem escopo de `BufferType`** — aplica pra todo `CenterProduct`. **Sem relação com MTO** (`BufferProfile.IsMakeToOrder`) — conceito independente, confirmado.

### Base: demanda pendente por dia

`IdOriginCenter` da `Order` é casado com `CenterProduct.IdCenter` — o buffer consumido por uma ordem outbound é o do centro de origem, não o de destino. Todo agrupamento abaixo é `Order.IsOutbound = 1` e não excluída (`deletedAt IS NULL`).

**Dia de hoje é um bucket especial** (2026-09-13, correção — antes hoje ficava de fora do cálculo inteiro): o valor do dia de hoje, por `(IdProduct, IdOriginCenter)`, é

```
Hoje = (soma de Quantity - DeliveredQuantity das ordens NÃO fictícias com DeliveryDate <= hoje, ou seja atrasadas + as de hoje)
     + (soma de Quantity - DeliveredQuantity das ordens fictícias com DeliveryDate = hoje)
```

**Dias futuros** (`DeliveryDate > hoje`, sem distinguir fictícia/real — mesma regra confirmada antes): agrupa por `(IdProduct, IdOriginCenter, DeliveryDate)`, somando `Quantity - DeliveredQuantity` de cada dia.

O bucket de hoje entra na mesma lista de "dias" que os dias futuros (mesmo threshold aplicado a todos, ver abaixo) — só a forma de somar a quantidade pendente muda pra esse dia específico.

### Horizonte (quantos dias pra frente olhar)

```
Horizonte = LeadTime * SpikeHorizonLTDays,   se SpikeHorizonType = Dlt
          = SpikeHorizonValue,                se SpikeHorizonType = Days
```

Entram no cálculo: o bucket de hoje (sempre) e os dias-grupo futuros com `DeliveryDate <= hoje + Horizonte`.

### Threshold (quando o dia "qualifica")

```
Quantidade do dia entra em QualifiedDemand se:
  SUM do dia >= Adu * SpikeThresholdAdu,                               quando SpikeThresholdType = Adu
  SUM do dia >= (RedZoneBase + RedZoneSafe) * SpikeThresholdPercentageRedZone,   quando SpikeThresholdType = PlanningRedZone

Senão, o dia contribui 0.
```

### Resultado final

```
QualifiedDemand = soma dos valores de todos os dias qualificados dentro do horizonte
```

Não é o maior dia nem um valor por dia — é a soma total.

## NetFlow

Utilitário: `Service.Domain/Utils/UtilsDdmrp.cs` — `CalculateNetflow(decimal stock, decimal qualifiedDemand, decimal inbounds)`. Não é um calculation step (não escreve nenhuma coluna sozinho) — é um método estático compartilhado por toda a aplicação (`Service.Domain`, sem dependências, acessível de `Application`/`Infra.Data`/`API`), pra ser chamado de onde quer que precise do NetFlow (ex.: um futuro step ou report).

```
NetFlow = Stock + Inbounds - QualifiedDemand
```

**Campos envolvidos**: `CenterProduct.Stock`, `CenterProduct.QualifiedDemand` (ver seção acima), `Inbounds` (soma de entradas pendentes — não é uma coluna própria, passado como parâmetro por quem chama o método).

## OrderQuantity

Utilitário: `Service.Domain/Utils/UtilsDdmrp.cs` — `CalculateOrderQuantity(decimal netflow, decimal topOfYellow, decimal topOfGreen)`. Mesmo status do NetFlow: método estático compartilhado, não é um calculation step, não escreve nenhuma coluna sozinho.

```
OrderQuantity = TopOfGreen - Netflow,   se Netflow < TopOfYellow
              = 0,                      caso contrário
```

**Campos envolvidos**: `Netflow` (resultado de `CalculateNetflow`, ver seção acima), `CenterProduct.TopOfYellow`/`TopOfGreen` (ver seção "Colunas calculadas" de `CenterProduct` em `CLAUDE.md`) — todos passados como parâmetro, nenhum lido diretamente do banco pelo método.

**Validado 2026-09-14**: a fórmula correta é `BufferSize - Netflow`, e `BufferSize` é o mesmo conceito que `TopOfGreen` (o topo do buffer) — ou seja, `TopOfGreen - Netflow` já é a fórmula certa, sem mudança de código necessária. A alternativa cogitada (`BufferSize - AvailableStock - TotalInbounds`, que descartaria o termo `QualifiedDemand`) foi descartada — `QualifiedDemand` continua fazendo parte do cálculo via `Netflow`.

## OptimizedOrderQuantity

Utilitário: `Service.Domain/Utils/UtilsDdmrp.cs` — `CalculateOptimizedOrderQuantity(decimal netflow, decimal topOfYellow, decimal topOfGreen, decimal moq, decimal packQuantity)`. Mesmo status do NetFlow/OrderQuantity: método estático compartilhado, não é um calculation step.

```
OptimizedOrderQuantity = 0,                                         se PackQuantity = 0 (sem múltiplo válido pra arredondar)

Quantity = OrderQuantity(Netflow, TopOfYellow, TopOfGreen)

OptimizedOrderQuantity = 0,                                         se Quantity < Moq (pedido menor que o mínimo, não vale a pena gerar)
                        = FLOOR(Quantity / PackQuantity) * PackQuantity,   caso contrário (arredonda pra baixo pro múltiplo de PackQuantity — decidido 2026-09-14, era CEILING antes)
```

**Campos envolvidos**: `Netflow`/`TopOfYellow`/`TopOfGreen` (mesmos do `OrderQuantity`, ver seção acima), `CenterProduct.Moq`, `CenterProduct.PackQuantity` — todos passados como parâmetro. Chama `CalculateOrderQuantity` internamente (não duplica a lógica). `PackQuantity = 0` retorna `0` (guarda adicionada 2026-09-14 — antes disparava `DivideByZeroException`, mesma "se não tiver base pra calcular, retorne 0" convenção já usada em `CalculateBufferPercentage`/`CalculateBufferColor` pro caso `TopOfGreen = 0`).

**Report `inventoryBufferManagement`: override por `Workspace` (2026-09-15)** — o valor acima (`CalculateOptimizedOrderQuantity`) é exposto no report como `SystemOptimizedOrderQuantity` (renomeado, era `OptimizedOrderQuantity`). `HasSuggestion = SystemOptimizedOrderQuantity > 0`. O report faz um left join com `Workspace` (`Service.Domain.Entities.Workspace`, chave `IdCenter`+`IdProduct`+`IdUser`, escopado ao usuário autenticado da requisição) e expõe um novo campo `OptimizedOrderQuantity = Workspace.OptimizedQuantity` quando existir uma linha de `Workspace` pro usuário atual, senão `SystemOptimizedOrderQuantity`. `Approved = Workspace.Approved`, `false` quando não existe linha de `Workspace`. Implementado em `ReportRepository.GetInventoryBufferManagementQueryable` como subquery correlacionada (`FirstOrDefault`) — mesmo padrão dos outros lookups opcionais (`ProviderCode`/`BufferProfileName`), não um `GroupJoin`.

## BufferPercentage

Utilitário: `Service.Domain/Utils/UtilsDdmrp.cs` — `CalculateBufferPercentage(decimal topOfGreen, decimal delta)` (parâmetro renomeado de `netflow` pra `delta` 2026-09-14, quando passou a ser usado também com `Stock` no lugar de `Netflow` — ver `NetflowBufferPercentage`/`ExecutionBufferPercentage` no report). Mesmo status dos outros: método estático compartilhado, não é um calculation step.

```
BufferPercentage = Delta / TopOfGreen,   se TopOfGreen <> 0
                  = 0,                    se TopOfGreen = 0 (sem buffer)
```

**Campos envolvidos**: `TopOfGreen`, `Delta` — ambos passados como parâmetro; qual "topo" e qual "delta" dependem de quem chama (ver report, abaixo). `TopOfGreen = 0` é o caso comum de um item que ainda não teve as zonas calculadas pelo Robot — tratado como "sem buffer" e retorna `0`, não uma exceção.

## SimulatedNetflow / SimulatedNetflowBufferPercentage / SimulatedNetflowBufferColor

Utilitário: `Service.Domain/Utils/UtilsDdmrp.cs` — `CalculateSimulatedNetflow(decimal netflow, bool approved, decimal workspaceOptimizedQuantity)` (adicionado 2026-09-15). Mesmo status dos outros: método estático compartilhado, não é um calculation step, não escreve nenhuma coluna sozinho.

```
SimulatedNetflow = Netflow + WorkspaceOptimizedQuantity,   se Workspace.Approved = true
                  = Netflow,                                se Workspace.Approved = false ou não existe linha de Workspace

SimulatedNetflowBufferPercentage = BufferPercentage(TopOfGreen, SimulatedNetflow)
SimulatedNetflowBufferColor = BufferColor(SimulatedNetflow, TopOfRed, TopOfYellow, TopOfGreen)
```

**Campos envolvidos**: `Netflow` (ver seção acima), `Workspace.Approved`/`Workspace.OptimizedQuantity` (ver "Report `inventoryBufferManagement`: override por `Workspace`" na seção `OptimizedOrderQuantity` acima — mesmo left join, escopado ao usuário autenticado da requisição), `TopOfGreen`/`TopOfRed`/`TopOfYellow`. Reaproveita `CalculateBufferPercentage`/`CalculateBufferColor` internamente (não duplica a lógica de divisão/classificação, só o termo `SimulatedNetflow`). Não usa `OptimizedOrderQuantity`/`SystemOptimizedOrderQuantity` (que já faz o fallback pro valor do sistema quando não há `Workspace`) — quando não aprovado, o termo simplesmente some da soma, não vira `SystemOptimizedOrderQuantity`.

**Report `inventoryBufferManagement`**: exposto como `SimulatedNetflowBufferPercentage`/`SimulatedNetflowBufferColor`, calculados inline em `ReportRepository.GetInventoryBufferManagementQueryable` a partir de um `SimulatedNetflow` intermediário calculado uma vez e reaproveitado pelos dois (mesma convenção de expressão SQL-translatável das outras métricas derivadas do report, não uma chamada a `UtilsDdmrp` pós-materialização — ver o comentário no topo do método).

**`ExecutionBufferPercentage`** (corrigido 2026-09-20, em duas etapas): `CalculateBufferPercentage(topOfGreen: TopOfYellowExecution, delta: Stock)` — o denominador é `TopOfYellowExecution` (era `GreenZoneExecution` originalmente, passou por `YellowZoneExecution` antes do valor final correto).

**`ExecutionBufferColor`** (corrigido 2026-09-20): `CalculateBufferColor(quantity: Stock, topOfRed: TopOfRedExecution, topOfYellow: TopOfYellowExecution, topOfGreen: TopOfGreenExecution)` — usa os **topos** de zona (`TopOfRedExecution`/`TopOfYellowExecution`/`TopOfGreenExecution`), não as zonas em si (`RedZoneExecution`/`YellowZoneExecution`/`GreenZoneExecution`, usadas incorretamente até então) — mesma correção de "zona → topo de zona" aplicada ao `ExecutionBufferPercentage` acima.

## BufferColor

Utilitário: `Service.Domain/Utils/UtilsDdmrp.cs` — `CalculateBufferColor(decimal quantity, decimal topOfRed, decimal topOfYellow, decimal topOfGreen)` (renomeado de `CalculateNetflowBufferColor`, parâmetro `netflow` renomeado pra `quantity`, 2026-09-14 — o método é genérico o bastante pra classificar qualquer quantidade contra os topos de zona, não só o Netflow), retorna `Service.Domain.Enums.BufferColor` (`Red`/`Yellow`/`Green`/`Blue`/`Black`/`NoColor`). Mesmo status dos outros: método estático compartilhado, não é um calculation step.

```
BufferColor = NoColor,   se TopOfGreen = 0 (sem buffer calculado)
            = Black,    se Quantity <= 0
            = Blue,     se Quantity > TopOfGreen
            = Red,      se 0 < Quantity <= TopOfRed
            = Yellow,   se TopOfRed < Quantity <= TopOfYellow
            = Green,    se TopOfYellow < Quantity <= TopOfGreen
```

**Campos envolvidos**: `Quantity` (no report `InventoryBufferManagement`, é o `Netflow` — resultado de `CalculateNetflow` — que alimenta o campo `NetflowBufferColor` da linha), `CenterProduct.TopOfRed`/`TopOfYellow`/`TopOfGreen` — todos passados como parâmetro. `NoColor` = ainda não tem buffer calculado (checado primeiro, antes de qualquer outra condição — mesmo caso "sem buffer" do `BufferPercentage`), `Black` = quantidade zero ou negativa (ruptura, quando `Quantity` é o Netflow — ajustado de `< 0` pra `<= 0` em 2026-09-19), `Blue` = acima do topo do verde (excesso), `Red`/`Yellow`/`Green` = dentro do buffer normal, cada um na sua faixa.

## CoverageDays

Utilitário: `Service.Domain/Utils/UtilsDdmrp.cs` — `CalculateCoverageDays(decimal availableStock, decimal adu)`. Mesmo status dos outros: método estático compartilhado, não é um calculation step.

```
CoverageDays = AvailableStock / Adu,   se Adu > 0
             = 0,                       caso contrário
```

**Campos envolvidos**: `CenterProduct.Stock` (passado como `availableStock`), `CenterProduct.Adu` — ambos passados como parâmetro. Guarda contra `Adu <= 0` (item sem consumo médio calculado ainda, ou item genuinamente sem consumo) retornando `0` em vez de dividir por zero.

## Order — colunas calculadas (`PendingQuantity`/`OrderLeadtime`)

Não são calculation steps — propriedades C# computadas na entidade `Order` (`get`-only, `Ignore()`'d no EF em `OrderConfiguration`, nunca uma coluna física). Presentes em `OrderGetDto`/`OpenOrderRow`, ausentes de `PostDto`/`PutDto`, sem setter.

```
PendingQuantity = 0,                          se DeliveredQuantity > Quantity
                = Quantity - DeliveredQuantity, caso contrário

OrderLeadtime   = null,                              se DeliveryDate não estiver setado
                = (DeliveryDate - CreationDate).Days, caso contrário
```

**Campos envolvidos**: `Order.Quantity`/`DeliveredQuantity` (`PendingQuantity` — nunca fica negativo mesmo se a ordem foi entregue a mais); `Order.CreationDate`/`DeliveryDate` (`OrderLeadtime` — `int?`, `null` sempre que não houver `DeliveryDate`). Ambas alimentam `TimeBuffer`/`ExecutionBuffer` abaixo.

## TimeBuffer

Utilitário: `Service.Domain/Utils/UtilsDdmrp.cs` — `CalculateTimeBuffer(DateTime deliveryDate, int orderLeadtime)`. Mesmo status dos outros: método estático compartilhado, não é um calculation step. **Diferente dos outros**: depende de "hoje" (`DateTime.Now`), lido internamente pelo método — não é passado como parâmetro. Por isso não é traduzível pra SQL (não pode ir direto num `Select` do EF) — quem usa (`ReportRepository.GetOpenOrdersAsync`) materializa a query primeiro e decora `OpenOrderRow.TimeBuffer` num loop depois, mesmo padrão do `Netflow`/`OrderQuantity`.

```
TimeBuffer = (Hoje - ((DeliveryDate + 1) - OrderLeadtime)) / MaiorEntre(1, OrderLeadtime)
```

**Campos envolvidos**: `Order.DeliveryDate`, `Order.OrderLeadtime` (ver `CLAUDE.md` — já é uma coluna calculada, `null` quando `DeliveryDate` não está setado). O denominador nunca é menor que `1` (`MaiorEntre(1, OrderLeadtime)`), mesmo se `OrderLeadtime = 0`. `ReportRepository.GetOpenOrdersAsync` só calcula `TimeBuffer` quando `DeliveryDate` e `OrderLeadtime` estão ambos presentes na linha; caso contrário fica `null`.

## TimeBufferColor

Utilitário: `Service.Domain/Utils/UtilsDdmrp.cs` — `CalculateTimeBufferColor(decimal timeBufferPercentage)`. **Diferente do `TimeBuffer`**: não depende de "hoje" — é uma função pura do percentual já calculado, reutiliza `Service.Domain.Enums.BufferColor` (mesmo enum de `CalculateBufferColor`, sem enum novo).

```
TimeBufferColor = Black,    se TimeBuffer > 100%
                = Red,      se TimeBuffer > 66%
                = Yellow,   se TimeBuffer > 33%
                = Green,    se TimeBuffer > 0%
                = NoColor,  caso contrário (TimeBuffer <= 0%)
```

- `TimeBuffer` aqui é a fração já produzida por `CalculateTimeBuffer` (ex.: `0.8m` = 80%, não `80`) — os limites (`1`/`0.66`/`0.33`/`0`) são comparados na mesma escala fracionária.
- `ReportRepository.GetOpenOrdersAsync` seta `OpenOrderRow.TimeBufferColor` (`BufferColor?`) logo depois de calcular `TimeBuffer`, dentro do mesmo guard (`DeliveryDate`/`OrderLeadtime` presentes) — fica `null` sempre que `TimeBuffer` também fica `null`.

## DaysToReceive / DaysLate

Utilitário: `Service.Domain/Utils/UtilsDdmrp.cs` — `CalculateDaysToReceive(DateTime? deliveryDate)`/`CalculateDaysLate(DateTime? deliveryDate)`. Mesmo status do `TimeBuffer`: depende de "hoje" (`DateTime.Now`, lido internamente, não passado como parâmetro).

```
DaysToReceive = 0,                        se DeliveryDate for nulo ou já passou (DeliveryDate < Hoje)
              = DeliveryDate - Hoje,       caso contrário

DaysLate      = 0,                        se DeliveryDate for nulo, hoje, ou no futuro
              = Hoje - DeliveryDate,       se DeliveryDate já passou
```

- Os dois retornam `int` (nunca `null`) — `0` já é o valor "não aplicável", tanto pra ordem sem `DeliveryDate` quanto pra ordem que não está adiantada/atrasada.
- **Diferente de `TimeBuffer`/`ExecutionBuffer`, isso não fica restrito ao relatório**: `OrderService.ToGetDTO` chama os dois direto em cima da entidade já materializada (o mapeamento `Order → OrderGetDto` roda depois da consulta ao banco, não dentro de uma expressão LINQ-to-entities — sem problema de tradução pra SQL) — então `GET /api/order` já devolve `daysToReceive`/`daysLate` em toda linha, não só no report. `ReportRepository.GetOpenOrdersAsync` decora `OpenOrderRow.DaysToReceive`/`DaysLate` no mesmo loop pós-materialização do `TimeBuffer`.
- **Campos envolvidos**: `Order.DeliveryDate` — único parâmetro, `null`-safe internamente (o próprio método trata `deliveryDate == null` sem precisar de guarda no chamador, diferente de `CalculateTimeBuffer`).

## ExecutionBuffer (por ordem, `OpenOrderRow.ExecutionBuffer`)

Não é um método de `UtilsDdmrp` — calculado diretamente em `ReportRepository.GetOpenOrdersAsync`/`ApplyExecutionBufferAsync`, já que depende de somar outras ordens (não é uma função pura de parâmetros passados por quem chama). Relatório `GET /api/report/openOrders/inbounds` — escopo apenas de ordens **inbound** (outbound é outro relatório, ver `CLAUDE.md`).

```
ExecutionBuffer = (CenterProduct.Stock + Σ PendingQuantity das ordens anteriores) / CenterProduct.TopOfYellowExecution
```

Onde "ordens anteriores" = toda ordem aberta (`deletedAt IS NULL`, `Quantity > DeliveredQuantity`), **inbound**, **não fictícia** (o relatório inteiro nunca considera ordens fictícias — não existe parâmetro pra isso), do mesmo `IdProduct` + `IdDestinyCenter` da ordem atual, com `Id < Id da ordem atual` **e** `DeliveryDate <= DeliveryDate da ordem atual` (duas condições independentes, não uma única chave de ordenação). `CenterProduct` é casado por `(IdProduct, IdCenter = IdDestinyCenter da ordem)`.

**Retorna `null`** quando `TopOfYellowExecution` é `0` ou não existe (`CenterProduct` não encontrado, ou zonas ainda não calculadas pelo Robot) — mesma guarda de divisão-por-zero das outras fórmulas. Também fica `null` quando a própria ordem não tem `DeliveryDate` ou não tem `CenterProduct` casado (não dá pra posicioná-la na sequência).

**Campos envolvidos**: `CenterProduct.Stock`, `CenterProduct.TopOfYellowExecution` (ver `CLAUDE.md`), `Order.PendingQuantity`, `Order.DeliveryDate`, `Order.Id`, `Order.IdProduct`, `Order.IdDestinyCenter`.

## ExecutionBufferColor (por ordem, `OpenOrderRow.ExecutionBufferColor`)

Reaproveita `UtilsDdmrp.CalculateBufferColor` (a mesma classificação usada em `NetflowBufferColor`/`ExecutionBufferColor` do report de `inventoryBufferManagement`), aplicada em cima da mesma quantidade calculada pra `ExecutionBuffer` — calculado no mesmo loop pós-materialização de `ApplyExecutionBufferAsync`, logo depois de `ExecutionBuffer`.

```
qt = CenterProduct.Stock + Σ PendingQuantity das ordens anteriores   (mesmo "qt" do numerador de ExecutionBuffer, sem dividir)
ExecutionBufferColor = CalculateBufferColor(qt, CenterProduct.TopOfRedExecution, CenterProduct.TopOfYellowExecution, CenterProduct.TopOfGreenExecution)
```

"Ordens anteriores" é a mesma definição de `ExecutionBuffer` acima. Fica `null` exatamente nos mesmos casos em que `ExecutionBuffer` fica `null` (mesma guarda de `TopOfYellowExecution` ausente/zero, mesmo pré-requisito de `DeliveryDate`/`CenterProduct` casado) — as duas colunas são sempre `null`/não-`null` juntas.

**Campos envolvidos**: os mesmos de `ExecutionBuffer`, mais `CenterProduct.TopOfRedExecution`/`TopOfGreenExecution` (ver `CLAUDE.md`).

## History — colunas derivadas (`StockDays`/`StockTotal`)

Não são calculation steps — propriedades C# computadas na entidade `History` (`get`-only, `Ignore()`'d no EF em `HistoryConfiguration`, nunca uma coluna física, nullable-lifted: ficam `null` a menos que todas as parcelas envolvidas estejam setadas). Presentes em `HistoryGetDto`, ausentes de `PostDto`/`PutDto`, sem setter. Nenhum step escreve essas colunas diretamente — elas só leem `History.Stock`/`Adu`/`OpenInbounds` (essas sim gravadas pelo robô como colunas físicas, ver `CLAUDE.md`).

```
StockDays  = Stock / Adu,            se Stock e Adu estiverem setados e Adu <> 0
           = null,                    caso contrário

StockTotal = Stock + OpenInbounds,   se Stock e OpenInbounds estiverem setados
           = null,                    caso contrário
```

**Campos envolvidos**: `History.Stock`, `History.Adu` (`StockDays`); `History.Stock`, `History.OpenInbounds` (`StockTotal`). `StockDays` guarda contra `Adu = 0`/`null` (item sem consumo médio calculado ainda) retornando `null` em vez de dividir por zero — mesma convenção de guarda das fórmulas de `CenterProduct`/`UtilsDdmrp` acima, só que `null` em vez de `0` (segue o padrão nullable-lifted das zonas derivadas de `CenterProduct`, não o padrão `0`-sentinela dos métodos de `UtilsDdmrp`).

## ReplicateCenterProductToHistory (`History` — colunas robô, snapshot de `CenterProduct`)

Step: `Service.Infra.Data/Calculation/Steps/ReplicateCenterProductToHistoryStep.cs` (nome no `calculation.config.json`: `"ReplicateCenterProductToHistory"`) — **deve rodar por último**, depois de `CalculateQualifiedDemand`, já que grava o estado final (pós-ZAF, pós-QualifiedDemand) do `CenterProduct`.

**Campos envolvidos**: `History.Adi`/`Adu`/`Cv`/`Frequency`/`FutureAduDays`/`GreenZone`/`HistoryAduDays`/`IdBufferProfile`/`IdReason`/`IdTag`/`LeadTime`/`Moq`/`OpenInbounds`/`OpenOutbound`/`PackQuantity`/`QualifiedDemand`/`RedBaseZone`/`RedSafeZone`/`ReservedStock`/`StandardDeviation`/`Stock`/`YellowZone`/`ZafGreenZone`/`ZafRedZone`/`ZafYellowZone` (resultado), `CenterProduct` (mesmos campos, lidos), `Order.Quantity`/`DeliveredQuantity`/`IsInbound`/`IsOutbound`/`IsFictional`/`IdOriginCenter`/`IdDestinyCenter`/`IdProduct` (`OpenInbounds`/`OpenOutbound`, que não existem como coluna em `CenterProduct` — ver abaixo).

Faz um **upsert** (SQL Server `MERGE`) da linha de `History` de **hoje** por `CenterProduct` ativo, casando por `(IdProduct, IdCenter, Date = hoje)`:

```
Se já existir uma linha de History pra (IdProduct, IdCenter, hoje):
    atualiza só as colunas acima (nunca toca em Consumption/DiscardStatus — são da ingestão, não deste step)

Senão:
    insere uma nova linha, com Consumption = 0 e DiscardStatus = NotReviewed (valores default,
    já que não existe consumo do dia vindo da ingestão pra essa linha nova)

OpenInbounds = soma de (Quantity - DeliveredQuantity) das Order não excluídas, IsInbound = true,
               IsFictional = false, IdProduct = CenterProduct.IdProduct, IdDestinyCenter = CenterProduct.IdCenter

OpenOutbound = soma de (Quantity - DeliveredQuantity) das Order não excluídas, IsOutbound = true,
               IsFictional = false, IdProduct = CenterProduct.IdProduct, IdOriginCenter = CenterProduct.IdCenter

RedBaseZone = CenterProduct.RedZoneBase   (nomes na ordem trocada entre as duas tabelas — não é erro)
RedSafeZone = CenterProduct.RedZoneSafe
```

- **`OpenInbounds`/`OpenOutbound` não são uma cópia direta de coluna** — `CenterProduct` não tem essas colunas (só existem calculadas a partir de `Order`, mesma fórmula já usada pela linha de "hoje" do report `inventoryHistory`, ver seção acima). Calculadas via subquery correlacionada dentro do próprio `MERGE`.
- Escopo: todo `CenterProduct` ativo (`deletedAt IS NULL`) — sem filtro de `BufferType` — ou, se `idCenterProduct` foi enviado no `POST /api/calculation/run`, só aquele (ver nota no topo deste arquivo).
- "Hoje" é `CAST(GETDATE() AS DATE)`, usado tanto na chave de casamento (`Date`) quanto nas somas de `Order`.
- Só uma linha por `(IdProduct, IdCenter, Date)` é afetada por execução — não é um "reset depois recompute" como as colunas de `CenterProduct` (não faz sentido zerar linhas de dias passados de `History`, já que cada dia é seu próprio registro imutável depois de escrito).

## Report `inventoryHistory` (`GET /api/report/inventoryHistory`)

Repositório: `ReportRepository.GetInventoryHistoryAsync(idCenter, idProduct, dateStart, dateEnd, cancellationToken)`. Não é `IQueryable` (materializa direto, `Task<List<InventoryHistoryRow>>`) — diferente do `inventoryBufferManagement`, não precisa compor `$filter`/`$orderby` do OData por cima.

Uma linha por dia de `History` no período `[dateStart, dateEnd]` pra aquele `(IdProduct, IdCenter)` — usando as colunas físicas gravadas pelo robô (`Stock`/`QualifiedDemand`/`OpenInbounds`/`Consumption`/`Adu`/`RedSafeZone`/`RedBaseZone`/`YellowZone`/`GreenZone`, ver `CLAUDE.md`) mais `StockTotal`/`InventoryDays` (mesmas fórmulas de `History.StockTotal`/`StockDays` acima, duplicadas aqui como expressão em vez de referenciar a propriedade `Ignore()`'d da entidade, mesma convenção de segurança de tradução do `inventoryBufferManagement`) — **UNIDA a uma linha extra de "hoje"**, montada ao vivo a partir de `CenterProduct`/`Order` em vez de `History`:

```
Date            = hoje

Stock           = CenterProduct.Stock
QualifiedDemand = CenterProduct.QualifiedDemand
Adu             = CenterProduct.Adu
RedSafeZone     = CenterProduct.RedZoneSafe
RedBaseZone     = CenterProduct.RedZoneBase
RedZone         = RedSafeZone + RedBaseZone
YellowZone      = CenterProduct.YellowZone
GreenZone       = CenterProduct.GreenZone

OrdersInTransit = soma de (Quantity - DeliveredQuantity) das Order não excluídas, IsInbound = true,
                  IsFictional = false, IdProduct = idProduct, IdDestinyCenter = idCenter

OpenOutbounds   = soma de (Quantity - DeliveredQuantity) das Order não excluídas, IsOutbound = true,
                  IsFictional = false, IdProduct = idProduct, IdOriginCenter = idCenter

Consumption     = MAX(OpenOutbounds, Adu)                      (Adu tratado como 0 se null)
StockTotal      = Stock + OrdersInTransit
InventoryDays   = Stock / Adu,   se Adu <> 0 e <> null
                = null,           caso contrário
Netflow         = UtilsDdmrp.CalculateNetflow(Stock, QualifiedDemand, OrdersInTransit)   (ver seção "NetFlow" acima; campos null tratados como 0)
```

- **Por quê a linha de "hoje" existe**: `History` só ganha uma linha do dia depois que o Robot roda naquele dia (ver o pipeline de cálculo em `CLAUDE.md`) — antes disso, "hoje" simplesmente não aparece em `History`. A linha extra preenche essa lacuna com o estado atual do `CenterProduct`, pra o gráfico de tendência não ter um buraco no dia corrente.
- A linha de "hoje" é **omitida por completo** se não existir `CenterProduct` pra aquele `(IdProduct, IdCenter)` — sem dado nenhum pra construir a linha.
- `Consumption` na linha de "hoje" não é o mesmo conceito de `History.Consumption` (que é o consumo realizado, gravado pelo robô) — é uma estimativa: o maior entre a demanda de saída já pendente (`OpenOutbounds`) e o consumo médio diário (`Adu`), usada como proxy de "quanto ainda deve sair hoje".
- Resultado final ordenado por `Date` ascendente (dias de `History` primeiro, "hoje" sempre por último, já que nenhuma linha de `History` pode ter data futura).

## Report `projectedStockAlert` (`GET /api/report/projectedStockAlert`)

Repositório: `ReportRepository.GetProjectedStockAlertAsync(idCenter, idProduct, dateStart, dateEnd, useAdu, useForecast, useInbounds, useOutbounds, accumulateInboundsToday, accumulateOutboundsToday, useFictionalOrders, cancellationToken)`. Não é `IQueryable` (materializa direto, `Task<List<ProjectedStockAlertRow>>`, mesmo status do `inventoryHistory`) — e, diferente de todo outro report, não dá pra ser uma única query: cada dia depende do estoque final do dia anterior, então o `Service` valida (`CenterProduct` existe → senão `NotFoundException`; `dateEnd >= dateStart` → senão `BadRequestException`) e o repositório busca os dados de cada dia com LINQ e depois roda um **loop sequencial em C#** por cima da lista já materializada.

Diferente do `inventoryHistory` (passado, lê colunas gravadas pelo robô), esse é **projeção futura** — simula, dia a dia, qual seria o estoque de um `(IdProduct, IdCenter)` se nada mudar além do que já está no calendário/forecast/pedidos abertos.

**Parâmetros de simulação** (todos opcionais, com o comportamento original como default):

```
useAdu                   (bool, default true)  — inclui Adu na comparação de MAX que define Outbound
useForecast               (bool, default true)  — inclui ProjectedConsumption na comparação de MAX
useOutbounds              (bool, default true)  — inclui OutboundOrders na comparação de MAX
useInbounds               (bool, default true)  — se false, Inbound deixa de ser somado no ClosingStock
                                                    (a coluna Inbound da linha continua mostrando o valor bruto)
accumulateInboundsToday   (bool, default false) — se true, Order inbound com DeliveryDate < hoje é tratada
                                                    como se DeliveryDate = hoje (pedido atrasado "cai" em hoje)
accumulateOutboundsToday  (bool, default false) — mesma ideia de accumulateInboundsToday, para Order outbound
useFictionalOrders        (bool, default true)  — se true, Order com IsFictional = true entram nas somas de
                                                    Inbound/OutboundOrders; se false, só Order reais contam
```

**Passo 1 — dados de entrada por dia**, um valor por `Date` no período `[dateStart, dateEnd]`:

```
ProjectedConsumption(dia) = Forecast do dia, explodido pelo Calendar exatamente como em
                             ForecastRepository.GetFilteredAsync (ver CLAUDE.md/bullet do Forecast):
                             dia útil, Forecast.Value / quantidade de dias úteis do intervalo do Forecast;
                             dia não útil = 0. Somado entre todos os Forecast que cobrem aquele dia.

EffectiveInboundDate(Order)  = hoje,               se accumulateInboundsToday = true e Order.DeliveryDate < hoje
                              = Order.DeliveryDate,  caso contrário

Inbound(dia)  = soma de (Quantity - DeliveredQuantity) das Order não excluídas, IsInbound = true,
                (IsFictional = false OU useFictionalOrders = true), IdProduct = idProduct, IdDestinyCenter = idCenter,
                EffectiveInboundDate = dia

EffectiveOutboundDate(Order) = hoje,               se accumulateOutboundsToday = true e Order.DeliveryDate < hoje
                              = Order.DeliveryDate,  caso contrário

OutboundOrders(dia) = soma de (Quantity - DeliveredQuantity) das Order não excluídas, IsOutbound = true,
                       (IsFictional = false OU useFictionalOrders = true), IdProduct = idProduct, IdOriginCenter = idCenter,
                       EffectiveOutboundDate = dia
```

- "Hoje" = `DateTime.Today` (data, sem hora). O "acúmulo" considera **toda** Order atrasada, mesmo com `DeliveryDate` fora de `[dateStart, dateEnd]` — só não aparece na linha de "hoje" se "hoje" também estiver fora do período pedido.

**Passo 2 — simulação sequencial**, do menor `Date` pro maior, com `Adu`/`RedZoneExecution`/`YellowZoneExecution`/`GreenZoneExecution`/`TopOfRedExecution`/`TopOfYellowExecution`/`TopOfGreenExecution` fixos (lidos uma vez do `CenterProduct`, tratados como `0` se `null` — `RedZoneExecution`/`YellowZoneExecution`/`GreenZoneExecution` vão pra linha só como referência, não entram em nenhuma fórmula abaixo):

```
OpeningStock(dia) = CenterProduct.Stock,          se for o primeiro dia do período
                   = ClosingStock(dia anterior),   caso contrário

Candidatos(dia)   = { Adu se useAdu, OutboundOrders(dia) se useOutbounds, ProjectedConsumption(dia) se useForecast }
Outbound(dia)     = MAX(Candidatos(dia)),   se Candidatos(dia) não for vazio
                   = 0,                      caso contrário (os três desligados)

ClosingStock(dia) = OpeningStock(dia) - Outbound(dia) + (Inbound(dia) se useInbounds, senão 0)

ExecutionBufferColor(dia) = UtilsDdmrp.CalculateBufferColor(ClosingStock(dia),
                                CenterProduct.TopOfRedExecution, CenterProduct.TopOfYellowExecution, CenterProduct.TopOfGreenExecution)
```

- `Outbound` é a saída realmente usada pra consumir o estoque no dia — o maior entre os candidatos habilitados (`useAdu`/`useOutbounds`/`useForecast`); `OutboundOrders`/`Inbound` continuam disponíveis na linha separadamente, com o valor bruto, mesmo quando desligados da fórmula.
- Mesmas zonas de execução (`TopOfRedExecution`/`TopOfYellowExecution`/`TopOfGreenExecution`) e mesma função `UtilsDdmrp.CalculateBufferColor` já usadas em `ExecutionBufferColor` do `openOrders`/`inventoryBufferManagement`.
- Escopo sempre um único `(IdProduct, IdCenter)` por chamada — não itera sobre todos os itens (diferente do `inventoryBufferManagement`), então materializar o período inteiro não tem o mesmo problema de volume que motivou o OData naquele report.
- **Campos envolvidos**: `CenterProduct.Stock`/`Adu`/`RedZoneExecution`/`YellowZoneExecution`/`GreenZoneExecution`/`TopOfRedExecution`/`TopOfYellowExecution`/`TopOfGreenExecution`, `Forecast.Value`/`StartDate`/`EndDate`, `Calendar.Date`/`IsWorkingDay`, `Order.Quantity`/`DeliveredQuantity`/`DeliveryDate`/`IsInbound`/`IsOutbound`/`IsFictional`/`IdProduct`/`IdDestinyCenter`/`IdOriginCenter`, `Product.Reference`, `Center.Code`.
- **`useFictionalOrders` é o único report que conta ordens fictícias por padrão** — `openOrders`/`inventoryBufferManagement`/`inventoryHistory` excluem `IsFictional = true` sempre, sem opção de ligar; aqui é o oposto, conta por padrão (`true`) e só exclui se `useFictionalOrders = false` for passado explicitamente.

## Report `bufferPenetration` (`GET /api/report/bufferPenetration`, 2026-09-19, `mode` adicionado no mesmo dia, `idCenter` → `idCenters` no dia seguinte)

Repositório: `ReportRepository.GetBufferPenetrationAsync(dateStart, dateEnd, idCenters, idProduct, mode, cancellationToken)`. Não é `IQueryable` (materializa direto, `Task<List<BufferPenetrationRow>>`, mesmo status do `inventoryHistory`/`openOrders`) — sem OData, sem paginação. Diferente de todo report anterior de período (`inventoryHistory`/`projectedStockAlert`, sempre um único `(IdProduct, IdCenter)`), esse cobre **todos os pares `(IdProduct, IdCenter)`** que tiverem `History` no intervalo pedido — `idCenters`/`idProduct` são filtros opcionais (mesmo padrão do `openOrders`), não um par obrigatório. **`idCenters`** (`int[]?`, `?idCenters=1&idCenters=2` na query string) filtra por **um ou mais** centros — `null`/array vazio não filtra por centro nenhum; `idProduct` continua um único valor (`int?`), não foi convertido pra lista.

Lê apenas colunas já gravadas pelo robô em `History` (nunca `CenterProduct` — é sempre passado, nunca presente) — mesmas colunas do `inventoryHistory`: `Stock`, `QualifiedDemand`, `OpenInbounds`, `RedBaseZone`, `RedSafeZone`, `YellowZone`, `GreenZone`. `History` é inner-joined a `Product`/`Center` não deletados (mesma convenção do `InventoryBufferManagementRow`).

**`mode`** (`Service.Domain.Enums.BufferPenetrationMode`: `Netflow`/`Execution`, query param, default `Netflow` — mesma convenção global de enum-como-string do resto da API) escolhe qual quantidade/conjunto de zonas alimenta `CalculateBufferColor` por dia — mesma distinção Netflow vs. Execução já usada em `InventoryBufferManagementRow` (`NetflowBufferColor`/`ExecutionBufferColor`) e `OpenOrderRow.ExecutionBufferColor`:

```
Para cada linha de History no intervalo [dateStart, dateEnd]:
    TopOfRed(dia) = (RedBaseZone ?? 0) + (RedSafeZone ?? 0)

    Se mode = Netflow:
        YellowZoneTop(dia) = TopOfRed(dia) + (YellowZone ?? 0)
        GreenZoneTop(dia)  = YellowZoneTop(dia) + (GreenZone ?? 0)
        Quantity(dia)      = Stock ?? 0   (mudou 2026-09-21: QualifiedDemand/OpenInbounds são ignorados nesse
                                            report, apesar do nome "Netflow" — NÃO usa UtilsDdmrp.CalculateNetflow)
        Color(dia)         = UtilsDdmrp.CalculateBufferColor(Quantity(dia), TopOfRed(dia), YellowZoneTop(dia), GreenZoneTop(dia))

    Se mode = Execution:
        RedZoneExecution(dia)    = YellowZoneExecution(dia) = TopOfRed(dia) / 2
        GreenZoneExecution(dia)  = YellowZone ?? 0
        TopOfRedExecution(dia)   = RedZoneExecution(dia)
        TopOfYellowExecution(dia) = RedZoneExecution(dia) + YellowZoneExecution(dia)
        TopOfGreenExecution(dia)  = TopOfYellowExecution(dia) + GreenZoneExecution(dia)
        Quantity(dia)             = Stock ?? 0   (NÃO usa Netflow, QualifiedDemand/OpenInbounds são ignorados)
        Color(dia)                = UtilsDdmrp.CalculateBufferColor(Quantity(dia), TopOfRedExecution(dia), TopOfYellowExecution(dia), TopOfGreenExecution(dia))

Agrupado por (IdProduct, IdCenter):
    QuantityDays              = COUNT(linhas de History do par no intervalo)
    Days<Color>               = COUNT(dias com Color(dia) = <Color>), uma coluna por cor (Black/Red/Yellow/Green/Blue/NoColor)
    DaysRedAndBlack           = DaysRed + DaysBlack
    Days<Color>Percentage     = Days<Color> / QuantityDays,   0 se QuantityDays = 0
    DaysRedAndBlackPercentage = DaysRedAndBlack / QuantityDays,   0 se QuantityDays = 0
```

- **Os dois modos (`Netflow` e `Execution`) usam só o `Stock` do dia como quantidade** (mudou 2026-09-21 — antes só `Execution` ignorava `QualifiedDemand`/`OpenInbounds`; agora `ComputeBufferColors` é sempre chamado com `qualifiedDemand=0`/`openInbounds=0` nesse report especificamente, então nem `Netflow` usa `UtilsDdmrp.CalculateNetflow`). Essa é uma particularidade do report `bufferPenetration` — `itemsByBufferColorHistory`, abaixo, continua chamando `ComputeBufferColors` com os valores reais de `QualifiedDemand`/`OpenInbounds`. A diferença entre os dois modos aqui passa a ser só o conjunto de zonas usado (Netflow tops vs. zonas de execução), não mais a quantidade.
- **As zonas de execução são derivadas de `TopOfRed`/`YellowZone`, mesma fórmula de `CenterProduct.RedZoneExecution`/`YellowZoneExecution`/`GreenZoneExecution`** (ver seção "CenterProduct — zonas derivadas" acima): `RedZoneExecution = YellowZoneExecution = TopOfRed / 2` (sem arredondamento — 2026-09-20, ver a nota "Arredondamento passou a ser só de exibição" lá), `GreenZoneExecution` é o valor puro de `YellowZone` (não `GreenZone`) — não são colunas próprias de `History`, são recalculadas por dia a partir das mesmas colunas snapshot (`RedBaseZone`/`RedSafeZone`/`YellowZone`) que alimentam o modo `Netflow`.
- **Zonas nulas contam como `NoColor`, não são excluídas do `QuantityDays`**: um dia de `History` sem `RedBaseZone`/`RedSafeZone`/`YellowZone`/`GreenZone` (zona ainda não calculada naquele dia) faz o topo de verde do modo escolhido ficar `0`, que `CalculateBufferColor` já resolve pra `NoColor` (checado antes de qualquer outra condição) — confirmado explicitamente 2026-09-19, esse dia ainda soma pro `QuantityDays` do par.
- **`DaysRedAndBlack`/`DaysRedAndBlackPercentage` não são uma cor nova** — são a soma de `DaysRed` + `DaysBlack` (dias em ruptura ou na zona vermelha), calculada em cima da contagem por dia, não uma classificação própria de `CalculateBufferColor`.
- Sem linha "hoje" injetada (diferente do `inventoryHistory`) — só conta dias que já têm uma linha de `History` gravada pelo robô; um par sem `History` nenhuma no intervalo simplesmente não aparece no resultado.
- **Campos envolvidos**: `History.IdProduct`/`IdCenter`/`Date`/`Stock`/`QualifiedDemand`/`OpenInbounds`/`RedBaseZone`/`RedSafeZone`/`YellowZone`/`GreenZone`, `Product.Reference`/`Description`, `Center.Code`.

## Report `itemsByBufferColorHistory` (`GET /api/report/itemsByBufferColorHistory`, 2026-09-19, `idCenter` → `idCenters` no dia seguinte; formato de resposta trocado para `{ netflow, execution }` no mesmo dia)

Repositório: `ReportRepository.GetItemsByBufferColorHistoryAsync(dateStart, dateEnd, idCenters, idProduct, cancellationToken)`. Não é `IQueryable` (materializa direto, `Task<ItemsByBufferColorHistoryResult>`) — mesmo escopo do `bufferPenetration` (todos os `(IdProduct, IdCenter)` no intervalo, `idCenters`/`idProduct` opcionais pra filtrar — `idCenters` aceita um ou mais centros, mesma mudança do `bufferPenetration`, ver acima), mesma fonte de dados (`History`, inner-joined a `Product`/`Center` não deletados) — mas **agregado na direção oposta**: `bufferPenetration` é uma linha por item (contando dias por cor), este é **uma linha por dia, por perspectiva** (contando itens por cor, por dia) — pensado pra alimentar um gráfico de tendência (quantos itens estavam em cada cor do buffer, dia a dia).

**Sempre calcula as duas perspectivas juntas, em listas separadas** (confirmado 2026-09-19, formato revisado no mesmo dia): a resposta é um objeto `{ netflow: [...], execution: [...] }` — cada lista tem uma linha (`BufferColorHistoryDayRow`) por dia, com as 6 cores como colunas (`date`, `red`, `yellow`, `green`, `blue`, `black`, `noColor`), em vez do desenho anterior (uma linha "achatada" por `(Date, Color)` com `NetflowQuantity`/`ExecutionQuantity` lado a lado) — pedido explicitamente pra já vir no formato que um gráfico de série temporal empilhada consome direto, uma série por perspectiva.

```
Para cada linha de History no intervalo [dateStart, dateEnd]:
    (NetflowColor(item,dia), ExecutionColor(item,dia)) = as mesmas duas fórmulas do report bufferPenetration acima
                                                          (mesmo ComputeBufferColors compartilhado no código)

Agrupado por Date — uma linha por dia em CADA lista (netflow, execution), com pelo menos uma linha de History
no intervalo (filtrado ou não):
    netflow[].red      = COUNT(itens daquele dia com NetflowColor(item,dia) = Red)
    netflow[].yellow    = COUNT(itens daquele dia com NetflowColor(item,dia) = Yellow)
    netflow[].green     = COUNT(itens daquele dia com NetflowColor(item,dia) = Green)
    netflow[].blue      = COUNT(itens daquele dia com NetflowColor(item,dia) = Blue)
    netflow[].black     = COUNT(itens daquele dia com NetflowColor(item,dia) = Black)
    netflow[].noColor   = COUNT(itens daquele dia com NetflowColor(item,dia) = NoColor)
    (execution[] espelha o mesmo, usando ExecutionColor(item,dia))
```

- **Uma linha por dia, todas as 6 cores como colunas** (revisado 2026-09-19, substitui o desenho anterior de "grade densa" com uma linha por `(Date, Color)`): cada dia que aparece em `netflow`/`execution` sempre tem as 6 contagens presentes na mesma linha (`0` quando nenhum item caiu naquela cor naquele dia) — não existe mais uma linha "faltando" por cor, porque cor virou coluna, não linha. Um dia sem nenhuma linha de `History` no intervalo (filtrado ou não) simplesmente não aparece em nenhuma das duas listas — mesma regra de "não injeta dia" do `bufferPenetration`.
- **`ComputeBufferColors`** (método privado estático em `ReportRepository`, extraído 2026-09-19 ao construir este report) é o mesmo cálculo por-dia que `bufferPenetration` já fazia — fatorado num só lugar pra as duas fórmulas (Netflow/Execution) não ficarem duplicadas entre os dois reports. `bufferPenetration` continua escolhendo uma das duas (via `mode`) depois de chamá-lo; este report usa as duas.
- `netflow`/`execution` cada um ordenado por `Date` ascendente; as duas listas têm sempre o mesmo conjunto de dias (mesma fonte de `History`, só a cor considerada por item muda).
- **Campos envolvidos**: `History.IdProduct`/`IdCenter`/`Date`/`Stock`/`QualifiedDemand`/`OpenInbounds`/`RedBaseZone`/`RedSafeZone`/`YellowZone`/`GreenZone`, `Product.deletedAt` (inner-join, não aparece no row), `Center.deletedAt` (idem).

## Report `accumulatedBufferHistory` (`GET /api/report/accumulatedBufferHistory`, 2026-09-20)

Repositório: `ReportRepository.GetAccumulatedBufferHistoryAsync(dateStart, dateEnd, idCenters, cancellationToken)`. Não é `IQueryable` (materializa direto, `Task<List<AccumulatedBufferHistoryRow>>`) — mesma fonte de dados dos dois reports acima (`History`, inner-joined a `Product`/`Center` não deletados), mas **`idCenters` é obrigatório aqui** (`int[]`, não `int[]?`) — diferente de `bufferPenetration`/`itemsByBufferColorHistory`, não existe modo "todos os centros"; sem `idProduct` (soma sempre todos os produtos).

**Filtro adicional, exclusivo deste report**: só entram linhas de `History` com `(RedBaseZone ?? 0) + (RedSafeZone ?? 0) > 0` — um item sem zona vermelha ainda calculada (robô nunca rodou pra ele, ou rodou e deu `0`) fica de fora inteiramente, não soma como `0`.

```
Para cada linha de History qualificada (RedBaseZone+RedSafeZone > 0, IdCenter em idCenters, Date em [dateStart, dateEnd]):
    NetflowRedZone(item,dia)    = (RedBaseZone ?? 0) + (RedSafeZone ?? 0)
    NetflowYellowZone(item,dia) = YellowZone ?? 0
    NetflowGreenZone(item,dia)  = GreenZone ?? 0
    AvailableStock(item,dia)    = (Stock ?? 0) - (ReservedStock ?? 0)

    ExecutionRedZone(item,dia)    = ExecutionYellowZone(item,dia) = NetflowRedZone(item,dia) / 2
    ExecutionGreenZone(item,dia)  = NetflowYellowZone(item,dia)
                                     (mesma fórmula de CenterProduct.RedZoneExecution/YellowZoneExecution/GreenZoneExecution
                                      e do ComputeBufferColors dos dois reports acima)

    (RedSafeAnalytical(item,dia), YellowSafeAnalytical(item,dia), GreenAnalytical(item,dia),
     YellowExcessAnalytical(item,dia), RedExcessAnalytical(item,dia)) =
        UtilsDdmrp.CalculateAnaliticalZone(NetflowRedZone(item,dia), NetflowYellowZone(item,dia), NetflowGreenZone(item,dia))
                                     (mesmas fórmulas de CenterProduct.RedSafeAnalytical/YellowSafeAnalytical/
                                      GreenAnalytical/YellowExcessAnalytical/RedExcessAnalytical — ver a seção
                                      "CenterProduct — zonas derivadas" acima para a definição de cada uma;
                                      RedExcessAnalytical depende de TopOfGreenNetflow, calculado internamente
                                      pelo próprio CalculateAnaliticalZone como NetflowRedZone+NetflowYellowZone+NetflowGreenZone)

    Netflow(item,dia) = UtilsDdmrp.CalculateNetflow(AvailableStock(item,dia), QualifiedDemand ?? 0, OpenInbounds ?? 0)

    AverageProjectedInventory(item,dia) = NetflowRedZone(item,dia) + (NetflowGreenZone(item,dia) / 2)

    TopOfGreenNetflow(item,dia) = NetflowRedZone(item,dia) + NetflowYellowZone(item,dia) + NetflowGreenZone(item,dia)
    ExcessStock(item,dia)       = AvailableStock(item,dia) - TopOfGreenNetflow(item,dia),   se > 0
                                 = 0,                                                        caso contrário

    ExcessStockAnalytical(item,dia) = AvailableStock(item,dia) - (NetflowRedZone(item,dia) + NetflowGreenZone(item,dia)),   se > 0
                                     = 0,                                                                                    caso contrário
                                     (igual a ExcessStock, mas sem a zona amarela — "estoque excesso analítico")

    MinimumOscillationRange(item,dia) = NetflowRedZone(item,dia)
    MaximumOscillationRange(item,dia) = NetflowRedZone(item,dia) + NetflowGreenZone(item,dia)

Agrupado por Date (soma entre todos os itens — todo IdProduct/IdCenter qualificado daquele dia vira uma única linha):
    ExecutionRedZone / ExecutionYellowZone / ExecutionGreenZone           = SUM(...)
    NetflowRedZone / NetflowYellowZone / NetflowGreenZone                 = SUM(...)
    RedSafeAnalytical / YellowSafeAnalytical / GreenAnalytical            = SUM(...)
    YellowExcessAnalytical / RedExcessAnalytical                         = SUM(...)
    AverageProjectedInventory / AvailableStock / Netflow / ExcessStock / ExcessStockAnalytical = SUM(...)
    MinimumOscillationRange / MaximumOscillationRange                     = SUM(...)
```

- **Usa `AvailableStock` (`Stock - ReservedStock`), não `Stock` cru** (2026-09-20, ajustado depois do report já existir) — em todo lugar que a fórmula original usaria `Stock` (linha da própria linha de saída, `Netflow`, `ExcessStock`), é `AvailableStock` que entra, mesma convenção já usada em `InventoryBufferManagementRow.Netflow`/`CoverageDays` (`Stock - ReservedStock` alimentando o cálculo). Nulos em `Stock`/`ReservedStock` tratados como `0`, mesmo padrão do resto do report.
- **Zonas analíticas adicionadas 2026-09-20** (`RedSafeAnalytical`/`YellowSafeAnalytical`/`GreenAnalytical`/`YellowExcessAnalytical`/`RedExcessAnalytical`) — alimentadas por `NetflowRedZone`/`NetflowYellowZone`/`NetflowGreenZone` (equivalentes deste report a `CenterProduct.RedZone`/`YellowZone`/`GreenZone`) em vez das colunas do `CenterProduct`. Diferente de `GetInventoryBufferManagementQueryable` (que precisa duplicar a fórmula inline por causa da composição OData — ver a nota de pitfall mais abaixo), este método é código C# comum pós-materialização (já itera `List<History>` em memória), então chama `UtilsDdmrp.CalculateAnaliticalZone(...)` diretamente — mesmo padrão já usado ali pra `CalculateNetflowTops`/`CalculateExecutionZone`. **`GreenAnalytical` teve um erro corrigido no mesmo dia** (junto com a correção equivalente em `GetInventoryBufferManagementQueryable`, ver a seção "CenterProduct — zonas derivadas" acima) — era `NetflowRedZone + NetflowGreenZone`; a fórmula final (`GreenZone` puro) agora vem só do helper.
- **`AverageProjectedInventory = NetflowRedZone + NetflowGreenZone/2`** é a fórmula DDMRP padrão de "estoque médio projetado" (Average Projected On-Hand) — usa metade da zona verde, não a zona amarela.
- **`MaximumOscillationRange` soma só `NetflowRedZone + NetflowGreenZone`, sem a zona amarela** — dado exatamente como especificado, não é o `TopOfGreen` completo (`Red + Yellow + Green`, que aqui é só `TopOfGreenNetflow`, calculado tanto separadamente pra `ExcessStock` quanto internamente por `CalculateAnaliticalZone` pra `RedExcessAnalytical`, e não aparece como campo próprio na linha final).
- **`ExcessStockAnalytical` adicionado 2026-09-20** — variante de `ExcessStock` que ignora a zona amarela (`AvailableStock - (NetflowRedZone + NetflowGreenZone)`, em vez de `AvailableStock - TopOfGreenNetflow`), floor em `0` do mesmo jeito. Não usa `UtilsDdmrp` (é uma conta simples, sem helper dedicado) — calculado inline no mesmo `foreach` que já calcula `excessStock`.
- **`ExecutionYellowZone` é sempre idêntico a `ExecutionRedZone`** — mesma duplicação intencional já documentada em `CenterProduct.RedZoneExecution`/`YellowZoneExecution` e em `bufferPenetration`'s modo `Execution`, não é um erro de cópia. **`RedExcessAnalytical` equivale a `MIN(NetflowYellowZone, NetflowGreenZone)`** quando `TopOfGreenNetflow > 0` (`0` caso contrário) — ver a derivação na seção "CenterProduct — zonas derivadas" acima.
- **O relatório final não tem `IdProduct`/`IdCenter`** — o agrupamento é só por `Date`, então a linha resultante é a soma de todos os pares `(IdProduto, IdCentro)` qualificados naquele dia entre os centros informados; não dá pra saber pelo relatório quantos itens ou quais centros/produtos entraram na soma de um dia específico.
- Sem linha "hoje" injetada e sem `mode` (sempre soma tanto as zonas de execução quanto as de netflow/analítica na mesma linha, ao contrário de `bufferPenetration`/`itemsByBufferColorHistory` que escolhem/separam por perspectiva).
- **Campos envolvidos**: `History.IdProduct`/`IdCenter`/`Date`/`Stock`/`ReservedStock`/`QualifiedDemand`/`OpenInbounds`/`RedBaseZone`/`RedSafeZone`/`YellowZone`/`GreenZone`, `Product.deletedAt` (inner-join, não aparece no row), `Center.deletedAt` (idem).
- **Tipos de resultado** (`Service.Domain/Report/Results/`): `ItemsByBufferColorHistoryResult { List<BufferColorHistoryDayRow> Netflow, List<BufferColorHistoryDayRow> Execution }`, `BufferColorHistoryDayRow { DateTime Date, int Red, int Yellow, int Green, int Blue, int Black, int NoColor }` — substituem o antigo `ItemsByBufferColorHistoryRow` (removido).
