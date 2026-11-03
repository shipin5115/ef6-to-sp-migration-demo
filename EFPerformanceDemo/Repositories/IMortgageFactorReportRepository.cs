using EFPerformanceDemo.Models;

namespace EFPerformanceDemo.Repositories
{
    /// <summary>
    /// Dapper-based accessor for the V2 (stored-procedure backed) Mortgage Factor Report.
    /// </summary>
    public interface IMortgageFactorReportRepository
    {
        Task<List<InvalidMortgageFactorResult>> GetInvalidMortgageFactorsAsync(
            int datasetId,
            int securityId,
            DateTime startDate,
            DateTime endDate,
            double upperLimit,
            double lowerLimit);
    }
}
