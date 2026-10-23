using EFPerformanceDemo.DBContext;
using EFPerformanceDemo.Reporting;
using EFPerformanceDemo.Reports;
using Microsoft.AspNetCore.Mvc;

namespace EFPerformanceDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MortgageFactorReportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MortgageFactorReportController(AppDbContext context)
        {
            _context = context;
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
    }
}
