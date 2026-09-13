# Formulas — Robot (cálculo DDMRP)

Lista centralizada das fórmulas usadas pelos steps de cálculo do Robot (`POST /api/calculation/run` / `POST /api/robot/run`). Cada step é uma classe C# em `Service.Infra.Data/Calculation/Steps/*.cs` (implementa `ICalculationStep`, tem acesso ao `ApplicationDbContext`/EF Core) — não são stored procedures no banco, então não precisam de migration pra "instalar"; mudar a fórmula é só editar o código. Toda fórmula nova deve ser documentada aqui antes (ou junto) de o step correspondente ser implementado.

## Adu (`CenterProduct.Adu`)

Step: `Service.Infra.Data/Calculation/Steps/CalculateAduStep.cs` (nome no `calculation.config.json`: `"CalculateAdu"`)

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

Calculados no **mesmo step** (`CalculateAduStep`), reaproveitando a mesma janela de dias válidos usada pelo Histórico (últimos `HistoryAduDays` dias não descartados, mesma regra de pular `Discarded` e estender a janela pra trás) — não participam do Histórico/Futuro/Misto, dependem só de `HistoryAduDays` (mesmo que `FutureAduDays` também esteja preenchido).

```
StandardDeviation = STDEVP(ISNULL(History.Quantity, 0)) dos dias do Histórico
Cv = StandardDeviation / AVG(ISNULL(History.Quantity, 0)) dos mesmos dias
```

- `STDEVP` é desvio padrão populacional (não amostral).
- Se `AVG(ISNULL(History.Quantity, 0)) = 0` (sem consumo nos dias considerados), `Cv = 0` em vez de dividir por zero.
- Mesma condição de aplicação do Histórico: só calculado quando `HistoryAduDays > 0`; caso contrário, `0`.

### Execução

- `CalculateAduStep` roda em lote (uma `UPDATE` set-based cobrindo todo `CenterProduct` ativo, executada via `ExecuteSqlRawAsync` dentro da classe C#), não em loop por linha — full recompute a cada execução, sem cálculo incremental (consistente com a regra geral do Robot).
- **Antes de calcular**, roda um `UPDATE` zerando `Adu`/`StandardDeviation`/`Cv` de todo `CenterProduct` ativo — convenção aplicada a toda coluna calculada do Robot (ver também `CalculateAdiStep`), pra garantir que nenhuma coluna fique com um valor de uma execução anterior caso a lógica de cálculo mude e deixe de cobrir algum caso.
- "Hoje" é `CAST(GETDATE() AS DATE)` — não é parametrizado.

## Adi (`CenterProduct.Adi`)

Step: `Service.Infra.Data/Calculation/Steps/CalculateAdiStep.cs` (nome no `calculation.config.json`: `"CalculateAdi"`)

**Campos envolvidos**: `CenterProduct.Adi` (resultado), `History.Quantity`/`Date` (todas as linhas contam, independente de `DiscardStatus`).

**Average Demand Interval** — mede o quão intermitente é a demanda: quanto maior o `Adi`, mais espaçadas as ocorrências de consumo.

```
Adi = (quantidade de linhas de History no período) / (quantidade dessas linhas com Quantity > 0)
```

- **Parâmetro `ThresholdDays`** (recebido pelo step via `calculation.config.json`, ex.: `{ "name": "ThresholdDays", "value": "360", "type": "int" }`, padrão `360` se omitido) — é **global pra execução inteira**, não um campo por `CenterProduct` como `HistoryAduDays`/`FutureAduDays`. Define o período: os últimos `ThresholdDays` dias corridos, terminando ontem (hoje não entra, mesma convenção do Adu).
- O numerador é a contagem real de linhas de `History` existentes no período — **não** o valor de `ThresholdDays` (se só existirem 200 linhas nos últimos 360 dias, numerador é 200, não 360).
- **Todas** as linhas contam, mesmo as com `DiscardStatus = Discarded` (diferente do Adu — aqui não há filtro de descarte).
- Se não existir nenhuma linha com `Quantity > 0` no período (incluindo o caso de não existir nenhuma linha de `History` no período), `Adi = 0` (não `NULL`).
- Não depende de `HistoryAduDays`/`FutureAduDays` — roda pra todo `CenterProduct` ativo que tenha (ou não) histórico no período.

### Execução

- `CalculateAdiStep` roda em lote (uma `UPDATE` set-based via `ExecuteSqlInterpolatedAsync`, que parametriza `ThresholdDays` com segurança em vez de concatenar a string), full recompute a cada execução.
- **Antes de calcular**, roda um `UPDATE` zerando `Adi` de todo `CenterProduct` ativo (mesma convenção do `CalculateAduStep`).
- "Hoje" é `CAST(GETDATE() AS DATE)`; a janela é `[hoje - ThresholdDays, hoje)`.
