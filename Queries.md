# Queries úteis

Queries T-SQL para validar manualmente o resultado dos steps de cálculo do Robot (`Service.Infra.Data/Calculation/Steps/*.cs`), rodando direto no banco pra conferir contra o valor gravado. Ver `Formulas.md` para a regra de negócio de cada campo.

## Adu — Histórico

Valida `CenterProduct.Adu` quando `HistoryAduDays > 0` e `FutureAduDays = 0` (fórmula pura Histórico). Espelha o `RankedHistory`/`HistoricalAdu` do `CalculateAduStandardDesvAndCvStep`: pega os últimos `HistoryAduDays` dias não descartados (pula `Discarded`, estende a janela pra trás); se não houver `HistoryAduDays` dias válidos disponíveis, soma o que existir (janela incompleta, mesmo divisor fixo); se não houver nenhum dia válido, `ExpectedHistorico` é `0` (não `NULL` — `LEFT JOIN`).

```sql
;WITH RankedHistory AS (
    SELECT
        h.IdProduct, h.IdCenter, h.Quantity,
        ROW_NUMBER() OVER (PARTITION BY h.IdProduct, h.IdCenter ORDER BY h.Date DESC) AS rn
    FROM Histories h
    WHERE h.deletedAt IS NULL
      AND h.DiscardStatus <> 2          -- exclui Discarded
      AND h.Date < CAST(GETDATE() AS DATE)  -- hoje não entra
)
SELECT cp.IdProduct, cp.IdCenter, cp.Adu,
       ISNULL(SUM(rh.Quantity), 0) / cp.HistoryAduDays AS ExpectedHistorico
FROM CenterProducts cp
LEFT JOIN RankedHistory rh
    ON rh.IdProduct = cp.IdProduct AND rh.IdCenter = cp.IdCenter AND rh.rn <= cp.HistoryAduDays
WHERE cp.HistoryAduDays > 0 AND cp.FutureAduDays = 0
GROUP BY cp.IdProduct, cp.IdCenter, cp.Adu, cp.HistoryAduDays;
```

## Adu — StandardDeviation e Cv

Valida `CenterProduct.StandardDeviation`/`Cv`. Mesma janela do Histórico (`RankedHistory` acima) — não depende de `FutureAduDays`.

```sql
;WITH RankedHistory AS (
    SELECT
        h.IdProduct, h.IdCenter, h.Quantity,
        ROW_NUMBER() OVER (PARTITION BY h.IdProduct, h.IdCenter ORDER BY h.Date DESC) AS rn
    FROM Histories h
    WHERE h.deletedAt IS NULL
      AND h.DiscardStatus <> 2
      AND h.Date < CAST(GETDATE() AS DATE)
)
SELECT
    cp.IdProduct, cp.IdCenter, cp.StandardDeviation, cp.Cv,
    CAST(STDEVP(ISNULL(rh.Quantity, 0)) AS DECIMAL(18,4)) AS ExpectedStandardDeviation,
    CASE WHEN CAST(AVG(ISNULL(rh.Quantity, 0)) AS DECIMAL(18,4)) = 0 THEN 0
         ELSE CAST(STDEVP(ISNULL(rh.Quantity, 0)) AS DECIMAL(18,4)) / CAST(AVG(ISNULL(rh.Quantity, 0)) AS DECIMAL(18,4))
    END AS ExpectedCv
FROM CenterProducts cp
LEFT JOIN RankedHistory rh
    ON rh.IdProduct = cp.IdProduct AND rh.IdCenter = cp.IdCenter AND rh.rn <= cp.HistoryAduDays
WHERE cp.HistoryAduDays > 0
GROUP BY cp.IdProduct, cp.IdCenter, cp.StandardDeviation, cp.Cv;
```

## Adu — Futuro

Valida `CenterProduct.Adu` quando `HistoryAduDays = 0` e `FutureAduDays > 0` (fórmula pura Futuro). Espelha o `FutureAdu` do step: soma `Forecast.Quantity` dos próximos `FutureAduDays` dias corridos (dia sem forecast conta como `0` — `LEFT JOIN`, sem descarte, sem checagem de "dias insuficientes").

```sql
SELECT
    cp.IdProduct, cp.IdCenter, cp.Adu,
    ISNULL(SUM(f.Quantity), 0) / cp.FutureAduDays AS ExpectedFuturo
FROM CenterProducts cp
LEFT JOIN Forecasts f
    ON f.IdProduct = cp.IdProduct
    AND f.IdCenter = cp.IdCenter
    AND f.deletedAt IS NULL
    AND f.Date > CAST(GETDATE() AS DATE)                                   -- hoje não entra
    AND f.Date <= DATEADD(DAY, cp.FutureAduDays, CAST(GETDATE() AS DATE))  -- próximos N dias corridos
WHERE cp.HistoryAduDays = 0 AND cp.FutureAduDays > 0
GROUP BY cp.IdProduct, cp.IdCenter, cp.Adu, cp.FutureAduDays;
```

## Adu — Misto

Valida `CenterProduct.Adu` quando `HistoryAduDays > 0` e `FutureAduDays > 0` (`Adu = (Historico + Futuro) / 2`). Junta as duas anteriores — cada componente já vem `0` (não `NULL`) quando não há dados, então a média sempre é calculável.

```sql
;WITH RankedHistory AS (
    SELECT
        h.IdProduct, h.IdCenter, h.Quantity,
        ROW_NUMBER() OVER (PARTITION BY h.IdProduct, h.IdCenter ORDER BY h.Date DESC) AS rn
    FROM Histories h
    WHERE h.deletedAt IS NULL
      AND h.DiscardStatus <> 2
      AND h.Date < CAST(GETDATE() AS DATE)
),
Historico AS (
    SELECT
        cp.Id AS CenterProductId,
        ISNULL(SUM(rh.Quantity), 0) / cp.HistoryAduDays AS Value
    FROM CenterProducts cp
    LEFT JOIN RankedHistory rh
        ON rh.IdProduct = cp.IdProduct AND rh.IdCenter = cp.IdCenter AND rh.rn <= cp.HistoryAduDays
    WHERE cp.HistoryAduDays > 0 AND cp.FutureAduDays > 0
    GROUP BY cp.Id, cp.HistoryAduDays
),
Futuro AS (
    SELECT
        cp.Id AS CenterProductId,
        ISNULL(SUM(f.Quantity), 0) / cp.FutureAduDays AS Value
    FROM CenterProducts cp
    LEFT JOIN Forecasts f
        ON f.IdProduct = cp.IdProduct AND f.IdCenter = cp.IdCenter AND f.deletedAt IS NULL
        AND f.Date > CAST(GETDATE() AS DATE)
        AND f.Date <= DATEADD(DAY, cp.FutureAduDays, CAST(GETDATE() AS DATE))
    WHERE cp.HistoryAduDays > 0 AND cp.FutureAduDays > 0
    GROUP BY cp.Id, cp.FutureAduDays
)
SELECT
    cp.IdProduct, cp.IdCenter, cp.Adu,
    hi.Value AS Historico,
    fu.Value AS Futuro,
    (hi.Value + fu.Value) / 2 AS ExpectedMisto
FROM CenterProducts cp
LEFT JOIN Historico hi ON hi.CenterProductId = cp.Id
LEFT JOIN Futuro fu ON fu.CenterProductId = cp.Id
WHERE cp.HistoryAduDays > 0 AND cp.FutureAduDays > 0;
```

## Adi

Valida `CenterProduct.Adi`. Espelha o `HistoryWindow`/`AdiAgg` do `CalculateAdiStep`: total de linhas de `History` no período (`ThresholdDays` dias corridos, terminando ontem) dividido pelas linhas com `Quantity > 0` — sem filtro de `DiscardStatus` (todas contam), `0` quando não há nenhuma linha com `Quantity > 0`. Troque `360` pelo valor de `ThresholdDays` usado no `calculation.config.json`.

```sql
DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @WindowStart DATE = DATEADD(DAY, -360, @Today);  -- mesmo ThresholdDays do calculation.config.json

;WITH HistoryWindow AS (
    SELECT h.IdProduct, h.IdCenter, h.Quantity
    FROM Histories h
    WHERE h.deletedAt IS NULL
      AND h.Date >= @WindowStart
      AND h.Date < @Today
),
AdiAgg AS (
    SELECT
        IdProduct, IdCenter,
        COUNT(*) AS TotalRecords,
        SUM(CASE WHEN Quantity > 0 THEN 1 ELSE 0 END) AS PositiveRecords
    FROM HistoryWindow
    GROUP BY IdProduct, IdCenter
)
SELECT
    cp.IdProduct, cp.IdCenter, cp.Adi,
    CASE WHEN ISNULL(aa.PositiveRecords, 0) = 0 THEN 0
         ELSE CAST(aa.TotalRecords AS DECIMAL(18,4)) / aa.PositiveRecords
    END AS ExpectedAdi
FROM CenterProducts cp
LEFT JOIN AdiAgg aa ON aa.IdProduct = cp.IdProduct AND aa.IdCenter = cp.IdCenter;
```
