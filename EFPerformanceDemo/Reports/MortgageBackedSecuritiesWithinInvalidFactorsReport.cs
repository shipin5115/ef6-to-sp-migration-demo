using EFPerformanceDemo.DBContext;
using EFPerformanceDemo.Models;
using EFPerformanceDemo.Reporting;

namespace EFPerformanceDemo.Reports
{
    /// <summary>
    /// "Mortgage Backed Securities Within Invalid Factors" report.
    ///
    /// Flags mortgage-backed security prices whose applicable amortization factor is
    /// missing, zero, or falls outside caller-configured upper/lower limits.
    /// </summary>
    public class MortgageBackedSecuritiesWithinInvalidFactorsReport
    {
        private readonly AppDbContext _dbContext;
        private readonly List<FilterParameter> _filterParameters;
        private readonly int _securityId;

        public MortgageBackedSecuritiesWithinInvalidFactorsReport(
            AppDbContext dbContext,
            List<FilterParameter> filterParameters,
            int securityId = -1)
        {
            _dbContext = dbContext;
            _filterParameters = filterParameters;
            _securityId = securityId;
        }

        public List<MortgageBackedSecuritiesWithinInvalidFactorsItem> GetQuery(ReportContext reportContext)
        {
            // Defining default end date for this report: previous business day.
            DateTime endDate = reportContext.Calendar.GetPreviousBusinessDate(DateTime.UtcNow);

            var parameterValues = SystemLimitFilter.ProcessFilterParameterValues(
                GetReportFilterParameters(false), _filterParameters, false);

            // Extract filter parameters
            double upperLimitFactorFilter = SystemLimitFilter.ProcessFilterParametersName(
                parameterValues, FilterParametersName.UpperLimitValueFilterName, false, double.MaxValue);
            double lowerLimitFactorFilter = SystemLimitFilter.ProcessFilterParametersName(
                parameterValues, FilterParametersName.LowerLimitValueFilterName, false, double.MinValue);

            // Setup query parameters and filter
            int datasetId = GetSelectedDatasetId(reportContext);

            var mortgageSecurity = new int[] { (int)PortfolioSecurityType.Mortgage };
            var startAndEndDate = SystemLimitFilterParameters.GetDateRangeFromFilterParameters(
                parameterValues, reportContext.Calendar, endDate, false);
            endDate = startAndEndDate.EndDate;
            DateTime startDate = startAndEndDate.StartDate;

            // Get Factor range for Mortgage security type
            var factorRange = (from ss in _dbContext.PortfolioSecurityTypes
                                where ss.SecurityTypeId == (int)PortfolioSecurityType.Mortgage || ss.DataId == datasetId
                                select ss.FactorTimeRange).FirstOrDefault();

            // Materialize once up front: the lookups below mix with other in-memory
            // sequences and are not expressible as a single server-side query.
            var allSecurityPrices = _dbContext.SecurityPrices.ToList();

            // Get latest factor and price data for each security
            var securityFactorPriceData = (from s in allSecurityPrices
                                            select new
                                            {
                                                SecurityID = s.SecurityID,
                                                Factor = s.Factor,
                                                FactorPriceDate = s.PriceDate
                                            }).ToList();

            // Get Security price info for Mortgage within given date range.
            var securityPrices = _dbContext.GetCalculatedSecurityFactorPriceByRange(
                mortgageSecurity, datasetId, _securityId, startDate, endDate, -1);

            // This Factor-Price lookup logic is very similar to CalculatePriceDimension in the Tension/Entity/MBS project.
            // Look for CalculateSecurityPrice method in Portfolio.
            // Ideally, these two implementations should be combined moving to future.

            // Get applicable FactorPriceData for given price date of each security.
            // FactorPriceDate should be less than PriceDate
            // Note difference between FactorPriceData and PriceDate should be less than FactorRange
            // Latest (max) FactorPriceDate meets above conditions
            var factorPriceData = from sp in securityPrices
                                   join s in allSecurityPrices on sp.SecurityId equals s.SecurityID
                                   where s.PriceDate <= sp.SecurityDate.PriceDate && factorRange >= 0
                                      && SystemLimitFilter.CheckFactor(s.PriceDate, sp.SecurityDate.PriceDate, factorRange)
                                   group s by new { SecurityID = sp.SecurityId, PriceDate = sp.SecurityDate.PriceDate } into gp
                                   select new
                                   {
                                       SecurityID = gp.Key.SecurityID,
                                       PriceDate = gp.Key.PriceDate,
                                       FactorPriceDate = gp.Max(x => x.PriceDate)
                                   };

            // Result
            var query = from sp in securityPrices
                        join fpd in factorPriceData on new { sp.SecurityId, PriceDate = sp.SecurityDate.PriceDate }
                            equals new { SecurityId = fpd.SecurityID, fpd.PriceDate } into fpdJoin
                        from fpd in fpdJoin.DefaultIfEmpty()
                        join sfpd in securityFactorPriceData on new { SecurityID = fpd?.SecurityID ?? -1, FactorPriceDate = fpd?.FactorPriceDate ?? DateTime.MinValue }
                            equals new { sfpd.SecurityID, sfpd.FactorPriceDate } into sfpdJoin
                        from sfpd in sfpdJoin.DefaultIfEmpty()
                        where sfpd == null
                           || sfpd.Factor == 0
                           || sfpd.Factor > upperLimitFactorFilter
                           || sfpd.Factor < lowerLimitFactorFilter
                        select new MortgageBackedSecuritiesWithinInvalidFactorsItem
                        {
                            DatasetName = sp.DatasetName.DatasetName,
                            EntityName = sp.EntityFactorName,
                            TickerName = sp.TickerName,
                            SecurityType = sp.SecurityType,
                            Symbol = sp.SecurityData.Symbol,
                            UnfactoredPrice = sp.Price,
                            Factor = sfpd == null ? 1 : sfpd.Factor,
                            FactoredPrice = sp.Price * (decimal)(sfpd == null ? 1 : sfpd.Factor),
                            FactorStatus = sfpd == null || sfpd.Factor == 0
                                ? PositionFactorStateConstants.NullOrZeroFactor
                                : PositionFactorStateConstants.InvalidFactor
                        };

            return FormatReportQuery(query);
        }

        private static List<FilterParameter> GetReportFilterParameters(bool required)
        {
            return new List<FilterParameter>
            {
                new() { Name = FilterParametersName.UpperLimitValueFilterName },
                new() { Name = FilterParametersName.LowerLimitValueFilterName },
                new() { Name = FilterParametersName.StartDateFilterName },
                new() { Name = FilterParametersName.EndDateFilterName }
            };
        }

        private static int GetSelectedDatasetId(ReportContext reportContext) => reportContext.DatasetId;

        private static List<MortgageBackedSecuritiesWithinInvalidFactorsItem> FormatReportQuery(
            IEnumerable<MortgageBackedSecuritiesWithinInvalidFactorsItem> query) => query.ToList();
    }
}
