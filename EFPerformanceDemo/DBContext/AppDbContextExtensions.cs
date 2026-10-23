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

            var query = from sp in context.SecurityPrices
                        join sm in context.SecurityMasters on sp.SecurityID equals sm.Id
                        where sp.DatasetId == datasetId
                           && sp.PriceDate >= startDate
                           && sp.PriceDate <= endDate
                           && typeNames.Contains(sp.SecurityType)
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

            return query.ToList();
        }
    }
}
