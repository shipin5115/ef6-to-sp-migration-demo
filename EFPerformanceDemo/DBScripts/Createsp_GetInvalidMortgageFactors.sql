-- Set-based replacement for MortgageBackedSecuritiesWithinInvalidFactorsReport.GetQuery
-- (the "invalid-factors" EF report).
--
-- V1 EF behavior being preserved exactly:
--   1. FactorTimeRange is looked up from PortfolioSecurityTypes where
--      SecurityTypeId = Mortgage(1) OR DataId = @DatasetId (TOP 1, arbitrary tie-break
--      exactly like EF's FirstOrDefault() with no ORDER BY).
--   2. Primary rows are SecurityPrices joined to SecurityMasters, filtered to
--      SecurityType = 'Mortgage', @DatasetId, the date range, and optionally a
--      single @SecurityId (-1 = all securities).
--   3. For each primary row, the "applicable factor price date" is the MAX PriceDate
--      found in SecurityPrices for the SAME SecurityID (ACROSS ALL DATASETS/TYPES,
--      exactly like the V1 LINQ join which only matches on SecurityID) that is
--      on/before the primary row's PriceDate and within FactorTimeRange days of it.
--   4. The Factor value used is then looked up again from SecurityPrices by
--      SecurityID + that FactorPriceDate (again across ALL datasets/types, and
--      intentionally NOT collapsed to a single row if duplicates exist, to match
--      the LINQ join's fan-out behavior for duplicate keys) via OUTER APPLY.
--   5. A row is flagged (returned) when no applicable factor was found, the
--      factor is zero, or the factor falls outside the caller's upper/lower limits.
--
-- This report takes a single DatasetId/optional single SecurityId (not a bulk
-- account/security ID set), so no TVP/account-chunking is used here -- a single
-- bounded, set-based round-trip is the correct fit (same pattern as
-- dbo.sp_GetOrdersByCategory / dbo.sp_SearchOrdersByProductKeyword in this repo).

CREATE OR ALTER PROCEDURE dbo.sp_GetInvalidMortgageFactors
    @DatasetId   INT,
    @SecurityId  INT = -1,
    @StartDate   DATETIME2(7),
    @EndDate     DATETIME2(7),
    @UpperLimit  FLOAT,
    @LowerLimit  FLOAT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        DECLARE @FactorTimeRange INT;

        -- Step 1: factorRange lookup (matches EF FirstOrDefault(), default 0 when none found).
        SELECT TOP (1)
            @FactorTimeRange = FactorTimeRange
        FROM dbo.PortfolioSecurityTypes
        WHERE SecurityTypeId = 1 OR DataId = @DatasetId;

        SET @FactorTimeRange = ISNULL(@FactorTimeRange, 0);

        -- Step 2: materialize the bounded primary rows once (mirrors
        -- GetCalculatedSecurityFactorPriceByRange). Only columns the final
        -- projection/joins actually need.
        SELECT
            sp.SecurityID,
            sp.PriceDate,
            sp.Price,
            sp.DatasetName,
            sp.EntityFactorName,
            sp.TickerName,
            sp.SecurityType,
            sm.Symbol
        INTO #PrimaryRows
        FROM dbo.SecurityPrices sp
        INNER JOIN dbo.SecurityMasters sm ON sp.SecurityID = sm.Id
        WHERE sp.DatasetId = @DatasetId
          AND sp.PriceDate >= @StartDate
          AND sp.PriceDate <= @EndDate
          AND sp.SecurityType = N'Mortgage'
          AND (@SecurityId = -1 OR sp.SecurityID = @SecurityId);

        CREATE CLUSTERED INDEX IX_PrimaryRows_SecurityID_PriceDate
            ON #PrimaryRows (SecurityID, PriceDate);

        -- Step 3 + 4 + 5: for each primary row, resolve its applicable
        -- FactorPriceDate, look up the Factor at that date/security (fanning
        -- out on duplicates via OUTER APPLY to match LINQ join semantics),
        -- then flag rows whose resolved factor is missing/zero/out-of-range.
        ;WITH FactorDates AS (
            SELECT
                pr.SecurityID,
                pr.PriceDate,
                MAX(s.PriceDate) AS FactorPriceDate
            FROM #PrimaryRows pr
            CROSS APPLY (
                SELECT s.PriceDate
                FROM dbo.SecurityPrices s
                WHERE s.SecurityID = pr.SecurityID
                  AND @FactorTimeRange >= 0
                  AND s.PriceDate <= pr.PriceDate
                  AND DATEDIFF(DAY, s.PriceDate, pr.PriceDate) <= @FactorTimeRange
            ) s
            GROUP BY pr.SecurityID, pr.PriceDate
        )
        SELECT
            pr.DatasetName                                      AS DatasetName,
            pr.EntityFactorName                                  AS EntityName,
            pr.TickerName                                        AS TickerName,
            pr.SecurityType                                      AS SecurityType,
            pr.Symbol                                            AS Symbol,
            pr.Price                                             AS UnfactoredPrice,
            sfpd.Factor                                          AS Factor,
            pr.Price * CAST(ISNULL(sfpd.Factor, 1) AS DECIMAL(18, 6)) AS FactoredPrice,
            CASE
                WHEN sfpd.Factor IS NULL OR sfpd.Factor = 0 THEN 0 -- NullOrZeroFactor
                ELSE 1                                              -- InvalidFactor
            END                                                   AS FactorStatus
        FROM #PrimaryRows pr
        LEFT JOIN FactorDates fd
            ON fd.SecurityID = pr.SecurityID AND fd.PriceDate = pr.PriceDate
        OUTER APPLY (
            SELECT s2.Factor
            FROM dbo.SecurityPrices s2
            WHERE s2.SecurityID = fd.SecurityID
              AND s2.PriceDate = fd.FactorPriceDate
        ) sfpd
        WHERE sfpd.Factor IS NULL
           OR sfpd.Factor = 0
           OR sfpd.Factor > @UpperLimit
           OR sfpd.Factor < @LowerLimit;

        DROP TABLE IF EXISTS #PrimaryRows;
    END TRY
    BEGIN CATCH
        DROP TABLE IF EXISTS #PrimaryRows;
        THROW;
    END CATCH;
END
GO
