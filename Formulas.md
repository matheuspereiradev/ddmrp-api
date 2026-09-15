# Formulas — Robot (cálculo DDMRP)

Lista centralizada das fórmulas usadas pelos steps de cálculo do Robot (`POST /api/calculation/run` / `POST /api/robot/run`). Cada step é uma classe C# em `Service.Infra.Data/Calculation/Steps/*.cs` (implementa `ICalculationStep`, tem acesso ao `ApplicationDbContext`/EF Core) — não são stored procedures no banco, então não precisam de migration pra "instalar"; mudar a fórmula é só editar o código. Toda fórmula nova deve ser documentada aqui antes (ou junto) de o step correspondente ser implementado.

## Adu (`CenterProduct.Adu`)

Step: `Service.Infra.Data/Calculation/Steps/CalculateAduStandardDesvAndCvStep.cs` (nome no `calculation.config.json`: `"CalculateAduStandardDesvAndCv"`)

**Campos envolvidos**: `CenterProduct.Adu` (resultado), `CenterProduct.HistoryAduDays`, `CenterProduct.FutureAduDays`, `History.Consumption`/`Date`/`DiscardStatus`, `Forecast.Quantity`/`Date`.

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

```
Futuro = (Soma de Forecast.Quantity dos próximos FutureAduDays dias) / FutureAduDays
```

- "Próximos N dias" = a partir de amanhã (hoje não entra), `FutureAduDays` dias corridos pra frente.
- Não existe descarte no Forecast (`DiscardStatus` é só de `History`) — todo dia no intervalo entra, mesmo sem previsão cadastrada (conta como 0).
- Divisor sempre `FutureAduDays` (fixo).

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
> **Importante (2026-09-14)**: todo cálculo de zona **sempre arredonda pra cima** ("arredondamento para cima sempre") — o valor final (já clampado em `0` se negativo) passa por `CEILING()` antes de ser gravado, nos mesmos 3 steps de zona-base, em `ApplyBafStep` (o split `RedZoneSafe`/`RedZoneBase = CEILING(BufferDdmrpRed / 2)` de `ManualFixed`) e em `ApplyZafStep` (a zona final depois de somar o delta: `GreenZone = CEILING(GreenZone + Delta)`, mesma coisa pra `YellowZone`/`RedZoneBase`). **Não** se aplica a cópias diretas de valor já informado pelo usuário (`ApplyBafStep`'s `YellowZone`/`GreenZone = baf.BufferDdmrpYellow`/`BufferDdmrpGreen`, ou o reverter `ISNULL(*Old, atual)`) — só arredonda o que é de fato uma conta feita pelo robô.

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

```
RedZone               = CEILING(RedZoneBase + RedZoneSafe)

TopOfRed              = CEILING(RedZoneBase + RedZoneSafe)                            (= RedZone, nome DDMRP-padrão separado por clareza)
TopOfYellow           = CEILING(RedZoneBase + RedZoneSafe + YellowZone)
TopOfGreen            = CEILING(RedZoneBase + RedZoneSafe + YellowZone + GreenZone)      (ponto de reordem — topo do buffer inteiro)

RedZoneExecution      = CEILING(TopOfRed / 2)
YellowZoneExecution   = CEILING(TopOfRed / 2)                                         (idêntico a RedZoneExecution — é a especificação dada, não é erro)
GreenZoneExecution    = CEILING(YellowZone)                                           (igual à coluna YellowZone pura, não a TopOfYellow)

TopOfRedExecution     = CEILING(RedZoneExecution)
TopOfYellowExecution  = CEILING(RedZoneExecution + YellowZoneExecution)
TopOfGreenExecution   = CEILING(RedZoneExecution + YellowZoneExecution + GreenZoneExecution)

RedSafeAnalytical       = CEILING(RedZone / 2)
YellowSafeAnalytical    = CEILING(RedZone)
GreenAnalytical         = CEILING(RedZone + GreenZone)
YellowExcessAnalytical  = CEILING(RedZone + YellowZone)
RedSafeExcessAnalytical = CEILING(RedZone / 2)                                        (idêntico a RedSafeAnalytical — mesmo padrão de valor duplicado)
```

**Arredondamento pra cima sempre** (2026-09-14, "arredondamento para cima sempre" — mesma regra das zonas-base acima, aplicada aqui via `Math.Ceiling` em C#, não `CEILING()` SQL, já que são propriedades computadas em memória — mas `Math.Ceiling(decimal)` é traduzido pelo EF Core/SqlServer da mesma forma quando a propriedade é referenciada dentro de um `Select`, então o efeito final é o mesmo). Nullable-safe: cada `CEILING(...)` acima só roda se todas as parcelas envolvidas tiverem valor — senão a propriedade inteira fica `null`, mesmo comportamento de antes (só ganhou o arredondamento por cima).

**Campos envolvidos**: `CenterProduct.RedZoneBase`/`RedZoneSafe`/`YellowZone`/`GreenZone` (as únicas colunas físicas da cadeia — tudo mais deriva delas, e essas 4 já chegam arredondadas pra cima dos steps/ZAF/BAF, ver seções acima). `TopOfYellowExecution` é o denominador de `ExecutionBuffer` (acima). As colunas `*Analytical` ainda não alimentam nenhum relatório/cálculo além de aparecerem cruas em `CenterProductGetDto`/`InventoryBufferManagementRow` — reservadas pra leitura "Buffer Analítica" ainda não escopada (ver `TODO.md`).

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
GreenZone     = CEILING(GreenZone + ZafGreenZone)
YellowZone    = CEILING(YellowZone + ZafYellowZone)
RedZoneBase   = CEILING(RedZoneBase + ZafRedZone)
```

- Arredondamento pra cima aplicado no valor final (depois de somar o delta), não no delta guardado em `ZafRedZone`/`ZafYellowZone`/`ZafGreenZone` (esse continua cru — ver "Importante" acima). Mesma regra "sempre arredonda pra cima" das seções de zona-base, ver a nota importante 2026-09-14 lá.

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

## BufferPercentage

Utilitário: `Service.Domain/Utils/UtilsDdmrp.cs` — `CalculateBufferPercentage(decimal topOfGreen, decimal delta)` (parâmetro renomeado de `netflow` pra `delta` 2026-09-14, quando passou a ser usado também com `Stock` no lugar de `Netflow` — ver `NetflowBufferPercentage`/`ExecutionBufferPercentage` no report). Mesmo status dos outros: método estático compartilhado, não é um calculation step.

```
BufferPercentage = Delta / TopOfGreen,   se TopOfGreen <> 0
                  = 0,                    se TopOfGreen = 0 (sem buffer)
```

**Campos envolvidos**: `TopOfGreen`, `Delta` — ambos passados como parâmetro; qual "topo" e qual "delta" dependem de quem chama (ver report, abaixo). `TopOfGreen = 0` é o caso comum de um item que ainda não teve as zonas calculadas pelo Robot — tratado como "sem buffer" e retorna `0`, não uma exceção.

## BufferColor

Utilitário: `Service.Domain/Utils/UtilsDdmrp.cs` — `CalculateBufferColor(decimal quantity, decimal topOfRed, decimal topOfYellow, decimal topOfGreen)` (renomeado de `CalculateNetflowBufferColor`, parâmetro `netflow` renomeado pra `quantity`, 2026-09-14 — o método é genérico o bastante pra classificar qualquer quantidade contra os topos de zona, não só o Netflow), retorna `Service.Domain.Enums.BufferColor` (`Red`/`Yellow`/`Green`/`Blue`/`Black`/`NoColor`). Mesmo status dos outros: método estático compartilhado, não é um calculation step.

```
BufferColor = NoColor,   se TopOfGreen = 0 (sem buffer calculado)
            = Black,    se Quantity < 0
            = Blue,     se Quantity > TopOfGreen
            = Red,      se 0 <= Quantity <= TopOfRed
            = Yellow,   se TopOfRed < Quantity <= TopOfYellow
            = Green,    se TopOfYellow < Quantity <= TopOfGreen
```

**Campos envolvidos**: `Quantity` (no report `InventoryBufferManagement`, é o `Netflow` — resultado de `CalculateNetflow` — que alimenta o campo `NetflowBufferColor` da linha), `CenterProduct.TopOfRed`/`TopOfYellow`/`TopOfGreen` — todos passados como parâmetro. `NoColor` = ainda não tem buffer calculado (checado primeiro, antes de qualquer outra condição — mesmo caso "sem buffer" do `BufferPercentage`), `Black` = quantidade negativa (ruptura, quando `Quantity` é o Netflow), `Blue` = acima do topo do verde (excesso), `Red`/`Yellow`/`Green` = dentro do buffer normal, cada um na sua faixa.

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
