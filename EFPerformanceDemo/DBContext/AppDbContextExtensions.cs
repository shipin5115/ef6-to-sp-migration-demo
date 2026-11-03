using EFPerformanceDemo.Models;

namespace EFPerformanceDemo.DBContext
{
    /// <summary>
    /// Query helpers that stand in for stored-procedure/view backed lookups
    /// used by the Mortgage Factor Report.
    /// </summary>
    public static class AppDbContextExtensions
    {
        /// <summary>
        /// Returns the unfactored security prices for the given security type(s)/dataset/date range,
        /// optionally narrowed to a single security (pass -1 for "all securities").
        /// </summary>
        public static List<SecurityPriceRangeResult> GetCalculatedSecurityFactorPriceByRange(
            this AppDbContext context,
            IEnumerable<int> securityTypeIds,
            int datasetId,
            int securityId,
            DateTime startDate,
            DateTime endDate,
            int unused)
        {
            var typeNames = securityTypeIds
                .Select(id => ((PortfolioSecurityType)id).ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // NOTE: The security-type filter is intentionally applied client-side (after
            // materialization) rather than as `typeNames.Contains(sp.SecurityType)` inside
            // the LINQ-to-Entities query below. EF6 translates a Contains() over a
            // variable-length collection into a parameterized `IN (@p0, @p1, ..., @pN)`
            // clause, and EF6's query-plan cache key includes that literal shape. Since
            // callers invoke this method with differing numbers of securityTypeIds, every
            // distinct count produces a brand-new cached query plan (both in EF6's
            // internal view/compiled-query cache and in SQL Server's plan cache). Over
            // time this causes unbounded plan-cache growth, increasing compilation
            // overhead on each new shape until executions exceed the command timeout.
            // Keeping the SQL predicate shape constant (independent of securityTypeIds
            // count) avoids that cache bloat entirely.
            var query = from sp in context.SecurityPrices
                        join sm in context.SecurityMasters on sp.SecurityID equals sm.Id
                        where sp.DatasetId == datasetId
                           && sp.PriceDate >= startDate
                           && sp.PriceDate <= endDate
                           && (securityId == -1 || sp.SecurityID == securityId)
                        select new SecurityPriceRangeResult
                        {
                            SecurityId = sp.SecurityID,
                            PriceDate = sp.PriceDate,
                            Price = sp.Price,
                            SecurityDate = new SecurityDateInfo { PriceDate = sp.PriceDate },
                            DatasetName = new DatasetNameInfo { DatasetName = sp.DatasetName },
                            EntityFactorName = sp.EntityFactorName,
                            TickerName = sp.TickerName,
                            SecurityType = sp.SecurityType,
                            SecurityData = sm
                        };

            return query
                .ToList();
                //.Where(r => typeNames.Contains(r.SecurityType))
                //.ToList();
        }
    }
}
