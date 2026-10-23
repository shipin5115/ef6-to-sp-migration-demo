-- Bulk data generator for the Mortgage Factor Report tables.
-- Produces a large volume of SecurityMasters + SecurityPrices rows so that
-- MortgageBackedSecuritiesWithinInvalidFactorsReport.GetQuery(...) has enough
-- data to measurably slow down (in-memory joins over large sequences).
--
-- Adjust @SecurityCount / @DaysOfHistory to scale the load up or down.
-- Safe to re-run: uses a dedicated symbol prefix ('MBS-BULK-%') and clears
-- its own previously generated rows before re-inserting.

SET NOCOUNT ON;

DECLARE @DatasetId        INT = 1;
DECLARE @SecurityCount    INT = 20000;  -- number of distinct securities to generate
DECLARE @DaysOfHistory    INT = 30;     -- price/factor rows per security (one per day)
DECLARE @FactorTimeRange  INT = 5;

-- 1. Ensure PortfolioSecurityTypes config exists for this dataset/Mortgage type.
IF NOT EXISTS (SELECT 1 FROM [dbo].[PortfolioSecurityTypes] WHERE [DataId] = @DatasetId AND [SecurityTypeId] = 1)
BEGIN
    INSERT INTO [dbo].[PortfolioSecurityTypes] ([SecurityTypeId], [DataId], [FactorTimeRange])
    VALUES (1, @DatasetId, @FactorTimeRange);
END

-- 2. Clear any previously generated bulk rows so this script is idempotent.
DELETE sp
FROM [dbo].[SecurityPrices] sp
INNER JOIN [dbo].[SecurityMasters] sm ON sm.Id = sp.SecurityID
WHERE sm.Symbol LIKE 'MBS-BULK-%';

DELETE FROM [dbo].[SecurityMasters] WHERE [Symbol] LIKE 'MBS-BULK-%';

-- 3. Generate @SecurityCount SecurityMasters rows using a numbers CTE.
;WITH Numbers AS (
    SELECT TOP (@SecurityCount)
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N
    FROM sys.all_objects a
    CROSS JOIN sys.all_objects b
)
INSERT INTO [dbo].[SecurityMasters] ([Symbol])
SELECT 'MBS-BULK-' + RIGHT('000000' + CAST(N AS VARCHAR(10)), 6)
FROM Numbers;

-- 4. Generate @DaysOfHistory SecurityPrices rows per security (one per calendar day).
--    Factor drifts slightly day-over-day; roughly 1 in 20 securities gets a
--    zero/invalid factor so the report actually has rows to flag.
;WITH Securities AS (
    SELECT sm.Id AS SecurityID, ROW_NUMBER() OVER (ORDER BY sm.Id) AS SeqNo
    FROM [dbo].[SecurityMasters] sm
    WHERE sm.Symbol LIKE 'MBS-BULK-%'
),
Days AS (
    SELECT TOP (@DaysOfHistory)
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS DayOffset
    FROM sys.all_objects
)
INSERT INTO [dbo].[SecurityPrices]
    ([SecurityID], [DatasetId], [PriceDate], [Price], [Factor], [DatasetName], [EntityFactorName], [TickerName], [SecurityType])
SELECT
    s.SecurityID,
    @DatasetId,
    DATEADD(DAY, -d.DayOffset, CAST(SYSUTCDATETIME() AS DATE)),
    100.00 - (d.DayOffset * 0.05),
    CASE
        WHEN s.SeqNo % 20 = 0 THEN 0.0                                   -- forces NullOrZeroFactor
        WHEN s.SeqNo % 20 = 1 THEN 1.5                                   -- forces InvalidFactor (too high)
        WHEN s.SeqNo % 20 = 2 THEN 0.01                                  -- forces InvalidFactor (too low)
        ELSE 0.80 - (d.DayOffset * 0.001)                                -- normal, gently drifting factor
    END,
    'Default Dataset',
    'Bulk Factor ' + CAST(s.SecurityID AS VARCHAR(10)),
    'BULK' + CAST(s.SecurityID AS VARCHAR(10)),
    'Mortgage'
FROM Securities s
CROSS JOIN Days d;

PRINT 'Inserted ' + CAST(@SecurityCount AS VARCHAR(10)) + ' securities x '
    + CAST(@DaysOfHistory AS VARCHAR(10)) + ' days = '
    + CAST(@SecurityCount * @DaysOfHistory AS VARCHAR(20)) + ' SecurityPrices rows.';
