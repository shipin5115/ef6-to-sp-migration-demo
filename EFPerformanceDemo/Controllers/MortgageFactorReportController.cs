using EFPerformanceDemo.DBContext;
using EFPerformanceDemo.Reporting;
using EFPerformanceDemo.Reports;
using EFPerformanceDemo.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EFPerformanceDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MortgageFactorReportController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMortgageFactorReportRepository _mortgageFactorReportRepository;

        public MortgageFactorReportController(
            AppDbContext context,
            IMortgageFactorReportRepository mortgageFactorReportRepository)
        {
            _context = context;
            _mortgageFactorReportRepository = mortgageFactorReportRepository;
        }

        /// <summary>
        /// Returns mortgage-backed security prices whose applicable factor is
        /// missing, zero, or outside the given upper/lower limits.
        /// </summary>
        [HttpGet("invalid-factors")]
        public IActionResult GetInvalidFactors(
            [FromQuery] int datasetId,
            [FromQuery] int securityId = -1,
            [FromQuery] double? upperLimit = null,
            [FromQuery] double? lowerLimit = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var filterParameters = new List<FilterParameter>();

            if (upperLimit.HasValue)
                filterParameters.Add(new FilterParameter { Name = FilterParametersName.UpperLimitValueFilterName, Value = upperLimit.Value.ToString() });

            if (lowerLimit.HasValue)
                filterParameters.Add(new FilterParameter { Name = FilterParametersName.LowerLimitValueFilterName, Value = lowerLimit.Value.ToString() });

            if (startDate.HasValue)
                filterParameters.Add(new FilterParameter { Name = FilterParametersName.StartDateFilterName, Value = startDate.Value.ToString("o") });

            if (endDate.HasValue)
                filterParameters.Add(new FilterParameter { Name = FilterParametersName.EndDateFilterName, Value = endDate.Value.ToString("o") });

            var reportContext = new ReportContext
            {
                DatasetId = datasetId,
                SecurityId = securityId,
                FilterParameters = filterParameters
            };

            var report = new MortgageBackedSecuritiesWithinInvalidFactorsReport(_context, filterParameters, securityId);
            var result = report.GetQuery(reportContext);

            return Ok(result);
        }

        /// <summary>
        /// Stored-procedure/Dapper backed equivalent of <see cref="GetInvalidFactors"/>.
        /// Pushes the factor-range lookup, date/dataset/security filtering, and
        /// invalid-factor resolution into dbo.sp_GetInvalidMortgageFactors (set-based,
        /// single round-trip) instead of materializing the full SecurityPrices table
        /// and performing in-memory joins/grouping, as the original EF-based
        /// <see cref="GetInvalidFactors"/> endpoint does.
        /// </summary>
        [HttpGet("invalid-factors-v2")]
        public async Task<IActionResult> GetInvalidFactorsV2(
            [FromQuery] int datasetId,
            [FromQuery] int securityId = -1,
            [FromQuery] double? upperLimit = null,
            [FromQuery] double? lowerLimit = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var filterParameters = new List<FilterParameter>();

            if (upperLimit.HasValue)
                filterParameters.Add(new FilterParameter { Name = FilterParametersName.UpperLimitValueFilterName, Value = upperLimit.Value.ToString() });

            if (lowerLimit.HasValue)
                filterParameters.Add(new FilterParameter { Name = FilterParametersName.LowerLimitValueFilterName, Value = lowerLimit.Value.ToString() });

            if (startDate.HasValue)
                filterParameters.Add(new FilterParameter { Name = FilterParametersName.StartDateFilterName, Value = startDate.Value.ToString("o") });

            if (endDate.HasValue)
                filterParameters.Add(new FilterParameter { Name = FilterParametersName.EndDateFilterName, Value = endDate.Value.ToString("o") });

            var reportContext = new ReportContext
            {
                DatasetId = datasetId,
                SecurityId = securityId,
                FilterParameters = filterParameters
            };

            // Defining default end date for this report: previous business day (same as V1).
            DateTime resolvedEndDate = reportContext.Calendar.GetPreviousBusinessDate(DateTime.UtcNow);

            var parameterValues = SystemLimitFilter.ProcessFilterParameterValues(
                new List<FilterParameter>
                {
                    new() { Name = FilterParametersName.UpperLimitValueFilterName },
                    new() { Name = FilterParametersName.LowerLimitValueFilterName },
                    new() { Name = FilterParametersName.StartDateFilterName },
                    new() { Name = FilterParametersName.EndDateFilterName }
                },
                filterParameters,
                false);

            double upperLimitFactorFilter = SystemLimitFilter.ProcessFilterParametersName(
                parameterValues, FilterParametersName.UpperLimitValueFilterName, false, double.MaxValue);
            double lowerLimitFactorFilter = SystemLimitFilter.ProcessFilterParametersName(
                parameterValues, FilterParametersName.LowerLimitValueFilterName, false, double.MinValue);

            var startAndEndDate = SystemLimitFilterParameters.GetDateRangeFromFilterParameters(
                parameterValues, reportContext.Calendar, resolvedEndDate, false);
            resolvedEndDate = startAndEndDate.EndDate;
            DateTime resolvedStartDate = startAndEndDate.StartDate;

            var result = await _mortgageFactorReportRepository.GetInvalidMortgageFactorsAsync(
                datasetId,
                securityId,
                resolvedStartDate,
                resolvedEndDate,
                upperLimitFactorFilter,
                lowerLimitFactorFilter);

            return Ok(result);
        }
    }
}
