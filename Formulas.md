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
- Se não existirem `HistoryAduDays` dias válidos disponíveis no histórico (produto muito novo, por exemplo), o resultado fica `NULL` em vez de calcular com uma janela incompleta.

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

- Só calcula quando os dois componentes (Histórico e Futuro) puderam ser calculados — se um dos dois ficar indisponível (ex.: histórico insuficiente), `Adu` fica `NULL` em vez de tratar o componente faltante como zero.

### Execução

- `CalculateAduStep` roda em lote (uma `UPDATE` set-based cobrindo todo `CenterProduct` ativo, executada via `ExecuteSqlRawAsync` dentro da classe C#), não em loop por linha — full recompute a cada execução, sem cálculo incremental (consistente com a regra geral do Robot).
- "Hoje" é `CAST(GETDATE() AS DATE)` — não é parametrizado.
