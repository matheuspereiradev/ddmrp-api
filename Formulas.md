# Formulas — Robot (cálculo DDMRP)

Lista centralizada das fórmulas usadas pelos steps de cálculo do Robot (`POST /api/calculation/run` / `POST /api/robot/run`). Cada step é uma classe C# em `Service.Infra.Data/Calculation/Steps/*.cs` (implementa `ICalculationStep`, tem acesso ao `ApplicationDbContext`/EF Core) — não são stored procedures no banco, então não precisam de migration pra "instalar"; mudar a fórmula é só editar o código. Toda fórmula nova deve ser documentada aqui antes (ou junto) de o step correspondente ser implementado.

## Adu (`CenterProduct.Adu`)

Step: `Service.Infra.Data/Calculation/Steps/CalculateAduStandardDesvAndCvStep.cs` (nome no `calculation.config.json`: `"CalculateAduStandardDesvAndCv"`)

**Campos envolvidos**: `CenterProduct.Adu` (resultado), `CenterProduct.HistoryAduDays`, `CenterProduct.FutureAduDays`, `History.Quantity`/`Date`/`DiscardStatus`, `Forecast.Quantity`/`Date`.

### Qual fórmula usar (por `CenterProduct`)

| `HistoryAduDays` | `FutureAduDays` | Fórmula |
|---|---|---|
| > 0 | = 0 (ou nulo) | **Histórico** |
| = 0 (ou nulo) | > 0 | **Futuro** |
| > 0 | > 0 | **Misto** |
| = 0 (ou nulo) | = 0 (ou nulo) | `Adu` fica `NULL` (nada a calcular) |

### Histórico

```
Histórico = (Soma de History.Quantity dos últimos HistoryAduDays dias não descartados) / HistoryAduDays
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
StandardDeviation = STDEVP(ISNULL(History.Quantity, 0)) dos dias do Histórico
Cv = StandardDeviation / AVG(ISNULL(History.Quantity, 0)) dos mesmos dias
```

- `STDEVP` é desvio padrão populacional (não amostral).
- Se `AVG(ISNULL(History.Quantity, 0)) = 0` (sem consumo nos dias considerados), `Cv = 0` em vez de dividir por zero.
- Mesma condição de aplicação do Histórico: só calculado quando `HistoryAduDays > 0`; caso contrário, `0`.

### Execução

- `CalculateAduStandardDesvAndCvStep` roda em lote (uma `UPDATE` set-based cobrindo todo `CenterProduct` ativo, executada via `ExecuteSqlRawAsync` dentro da classe C#), não em loop por linha — full recompute a cada execução, sem cálculo incremental (consistente com a regra geral do Robot).
- **Antes de calcular**, roda um `UPDATE` zerando `Adu`/`StandardDeviation`/`Cv` de todo `CenterProduct` ativo — convenção aplicada a toda coluna calculada do Robot (ver também `CalculateAdiStep`), pra garantir que nenhuma coluna fique com um valor de uma execução anterior caso a lógica de cálculo mude e deixe de cobrir algum caso.
- "Hoje" é `CAST(GETDATE() AS DATE)` — não é parametrizado.

## Adi (`CenterProduct.Adi`)

Step: `Service.Infra.Data/Calculation/Steps/CalculateAdiStep.cs` (nome no `calculation.config.json`: `"CalculateAdi"`)

**Campos envolvidos**: `CenterProduct.Adi` (resultado), `History.Quantity`/`Date` (todas as linhas contam, independente de `DiscardStatus`).

**Average Demand Interval** — mede o quão intermitente é a demanda: quanto maior o `Adi`, mais espaçadas as ocorrências de consumo.

```
Adi = (quantidade de linhas de History no período) / (quantidade dessas linhas com Quantity > 0)
```

- **Parâmetro `ThresholdDays`** (recebido pelo step via `calculation.config.json`, ex.: `{ "name": "ThresholdDays", "value": "360" }`, padrão `360` se omitido) — é **global pra execução inteira**, não um campo por `CenterProduct` como `HistoryAduDays`/`FutureAduDays`. Define o período: os últimos `ThresholdDays` dias corridos, terminando ontem (hoje não entra, mesma convenção do Adu).
- O numerador é a contagem real de linhas de `History` existentes no período — **não** o valor de `ThresholdDays` (se só existirem 200 linhas nos últimos 360 dias, numerador é 200, não 360).
- **Todas** as linhas contam, mesmo as com `DiscardStatus = Discarded` (diferente do Adu — aqui não há filtro de descarte).
- Se não existir nenhuma linha com `Quantity > 0` no período (incluindo o caso de não existir nenhuma linha de `History` no período), `Adi = 0` (não `NULL`).
- Não depende de `HistoryAduDays`/`FutureAduDays` — roda pra todo `CenterProduct` ativo que tenha (ou não) histórico no período.

### Execução

- `CalculateAdiStep` roda em lote (uma `UPDATE` set-based via `ExecuteSqlInterpolatedAsync`, que parametriza `ThresholdDays` com segurança em vez de concatenar a string), full recompute a cada execução.
- **Antes de calcular**, roda um `UPDATE` zerando `Adi` de todo `CenterProduct` ativo (mesma convenção do `CalculateAduStandardDesvAndCvStep`).
- "Hoje" é `CAST(GETDATE() AS DATE)`; a janela é `[hoje - ThresholdDays, hoje)`.

## Buffer Ddmrp zonas normal (`CenterProduct.RedZoneBase`/`RedZoneSafe`/`YellowZone`/`GreenZone`)

Step: `Service.Infra.Data/Calculation/Steps/CalculateNormalBufferZonesStep.cs` (nome no `calculation.config.json`: `"CalculateNormalBufferZones"`)

**Campos envolvidos**: `CenterProduct.RedZoneBase`/`RedZoneSafe`/`YellowZone`/`GreenZone` (resultado; `RedZone` é a propriedade calculada `RedZoneBase + RedZoneSafe`, nunca gravada diretamente — ver `CLAUDE.md`), `CenterProduct.Adu`/`LeadTime`/`Frequency`/`Moq`/`BufferType`/`UseSuggestedLTFactor`/`UseSuggestedVariabilityFactor`/`UseDafOnGreenZone`/`CustomLeadTimeFactor`/`CustomVariabilityFactor`/`GreenZoneParametrizationUseMoq`/`GreenZoneParametrizationUseAduXFrequency`/`GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime`, `BufferProfile.LeadTimeFactor`/`VariabilityFactor`, `DemandAdjustmentFactor.IsActive`/`EffectiveFrom`/`EffectiveTo`/`AdjustmentType`/`AdjustmentValue`.

**Escopo: só `CenterProduct.BufferType = 0` (Normal)**. `ManualFixed`/`MinMax`/`DynamicMinMax` não são tocados por esse step — `ManualFixed` presumivelmente usa `BufferAdjustmentFactor.BufferDdmrpRed`/`YellowOld`/`GreenOld`... (a ser confirmado), `MinMax`/`DynamicMinMax` teriam lógica própria futura. **Depende de `CenterProduct.Adu` já calculado** — por isso roda depois de `CalculateAduStandardDesvAndCv` em `calculation.config.json`. **Não trata itens MTO ainda** — ver `TODO.md`.

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

### Execução

- `CalculateNormalBufferZonesStep` roda em lote (uma `UPDATE` set-based via `ExecuteSqlRawAsync`, sem parâmetro externo — nada a parametrizar), full recompute a cada execução, escopado a `BufferType = 0`.
- **Antes de calcular**, roda um `UPDATE` zerando `YellowZone`/`GreenZone`/`RedZoneSafe`/`RedZoneBase` de todo `CenterProduct` ativo com `BufferType = 0` (mesma convenção dos outros steps — escopada ao `BufferType` pra nunca tocar itens de outro tipo).
- "Hoje" é `GETDATE()` (sem `CAST(... AS DATE)`, diferente do Adu/Adi — usado apenas na comparação de vigência do DAF).

## Buffer Ddmrp zonas MinMax (`CenterProduct.RedZoneBase`/`RedZoneSafe`/`YellowZone`/`GreenZone`)

Step: `Service.Infra.Data/Calculation/Steps/CalculateMinMaxBufferZonesStep.cs` (nome no `calculation.config.json`: `"CalculateMinMaxBufferZones"`)

**Campos envolvidos**: mesmos do Normal, mais `History.Quantity`/`Date`/`DiscardStatus` (janela de `ThresholdDays` dias).

**Escopo: só `CenterProduct.BufferType = 2` (MinMax)**. **Não trata itens MTO** — mesma ressalva dos outros steps de zona, ver `TODO.md`.

```
Yellow    = 0
Green     = mesma fórmula do Green do Normal (MAX de GreenCandidate1/2/3, mesmos toggles
            GreenZoneParametrizationUse*, mesmo AdjustedAdu via DAF pro GreenCandidate3)
RedSafe   = 0
RedBase   = MAX(History.Quantity) nos últimos ThresholdDays dias corridos, terminando ontem
            (hoje não entra), excluindo linhas com DiscardStatus = Discarded — 0 se não houver
            nenhuma linha de History no período
```

- **Parâmetro `ThresholdDays`** (`calculation.config.json`, ex.: `{ "name": "ThresholdDays", "value": "180" }`, padrão `180` se omitido) — mesma mecânica do `CalculateAdiStep` (`ExecuteSqlInterpolatedAsync`, parametrizado com segurança), mas com default diferente (`180`, não `360`).
- Janela `[hoje - ThresholdDays, hoje)`, mesma convenção do Adu/Adi (dias corridos, hoje não conta).
- `RedBase` é o "maior consumo diário" da janela — não uma média nem soma; um único dia de pico define o valor.

### Execução

- `CalculateMinMaxBufferZonesStep` roda em lote (`UPDATE`s set-based via `ExecuteSqlInterpolatedAsync`), full recompute a cada execução, escopado a `BufferType = 2`.
- **Antes de calcular**, roda um `UPDATE` zerando `YellowZone`/`GreenZone`/`RedZoneSafe`/`RedZoneBase` de todo `CenterProduct` ativo com `BufferType = 2`.

## Buffer Ddmrp zonas MinMax Dinâmico (`CenterProduct.RedZoneBase`/`RedZoneSafe`/`YellowZone`/`GreenZone`)

Step: `Service.Infra.Data/Calculation/Steps/CalculateDynamicMinMaxBufferZonesStep.cs` (nome no `calculation.config.json`: `"CalculateDynamicMinMaxBufferZones"`)

**Campos envolvidos**: mesmos do MinMax, mais `History.Quantity`/`Date`/`DiscardStatus` (janela dinâmica, sem parâmetro de config — usa `CenterProduct.HistoryAduDays` diretamente).

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

Pra cada dia `d` dentro de `[WindowStart, ontem]`, soma `History.Quantity` não descartada entre `d - K` e `d` (ambos os extremos incluídos, `K+1` dias no total), onde `K = MAX(LeadTime, Frequency)`. `MaiorAcumulado` é o maior valor entre todas essas somas.

```
K = MAX(LeadTime, Frequency)

Para cada dia d em [WindowStart, ontem]:
    RollingSum(d) = SOMA(History.Quantity não descartada) para History.Date em [d - K, d]

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

### Execução

- Implementação em T-SQL via CTE recursiva (`DateSpine`, gerando um dia de calendário por vez de `WindowStart` até ontem, por `CenterProduct`) + `CROSS APPLY` (subquery correlacionada por dia, somando `History.Quantity` na janela `[d-K,d]`) — **não dá pra usar `SUM() OVER (... ROWS BETWEEN N PRECEDING ...)`** porque o SQL Server exige que `N` seja uma constante no frame da window function, e aqui `K` varia por `CenterProduct` (`MAX(LeadTime, Frequency)`).
- `OPTION (MAXRECURSION 0)` é obrigatório na `UPDATE` final — a CTE recursiva por padrão trava em 100 níveis, e a janela pode facilmente passar disso (`HistoryAduDays` grande + dias descartados).
- **Antes de calcular**, roda um `UPDATE` zerando `YellowZone`/`GreenZone`/`RedZoneSafe`/`RedZoneBase` de todo `CenterProduct` ativo com `BufferType = 3`. `RedZoneSafe` nunca é escrito de novo depois (fica sempre `0`, por fórmula).
- Sem parâmetro externo — `HistoryAduDays` já é uma coluna por `CenterProduct` (mesma usada pelo Adu), não precisa de config.

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

- O delta do vermelho é somado em `RedZoneBase`, não em `RedZoneSafe` — decisão explícita, não inferida.
- `RedZone` (propriedade calculada `RedZoneSafe + RedZoneBase`) reflete o ajuste automaticamente, já que `RedZoneBase` foi incrementado.

### Execução

- `ApplyZafStep` roda em lote (`UPDATE`s set-based via `ExecuteSqlRawAsync`, sem parâmetro externo), full recompute a cada execução, escopado a `BufferType <> 1`.
- **Antes de calcular**, roda um `UPDATE` zerando `ZafRedZone`/`ZafYellowZone`/`ZafGreenZone` de **todo** `CenterProduct` ativo, sem escopo de `BufferType` — inclui `ManualFixed`, que nunca recebe um delta real depois (fica sempre `0`).
- Depende de rodar **depois** de `CalculateNormalBufferZonesStep`, `CalculateMinMaxBufferZonesStep` e `CalculateDynamicMinMaxBufferZonesStep` na mesma execução — como o pipeline inteiro roda full recompute diário (sem incremental), `GreenZone`/`YellowZone`/`RedZoneBase`/`RedZoneSafe` já estão "limpos" (recalculados do zero) antes do ZAF ser somado, então não há risco de acumular o mesmo ajuste em execuções sucessivas.
- "Hoje" é `GETDATE()`, usado só na comparação de vigência do ZAF.
