using System.Data;
using Dapper;
using EFPerformanceDemo.DBContext;
using EFPerformanceDemo.Models;

namespace EFPerformanceDemo.Repositories
{
    /// <summary>
    /// ADO.NET/Dapper-based implementation of <see cref="IMortgageFactorReportRepository"/>.
    /// Executes dbo.sp_GetInvalidMortgageFactors in a single, set-based round-trip,
    /// replacing the full SecurityPrices.ToList() plus in-memory grouping/joins
    /// performed by MortgageBackedSecuritiesWithinInvalidFactorsReport.GetQuery.
    /// </summary>
    public class MortgageFactorReportRepository : IMortgageFactorReportRepository
    {
        private readonly AppDbContext _context;

        public MortgageFactorReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<InvalidMortgageFactorResult>> GetInvalidMortgageFactorsAsync(
            int datasetId,
            int securityId,
            DateTime startDate,
            DateTime endDate,
            double upperLimit,
            double lowerLimit)
        {
            var connection = _context.Database.Connection;
            var wasClosed = connection.State != ConnectionState.Open;

            if (wasClosed)
                await connection.OpenAsync();

            try
            {
                var rows = await connection.QueryAsync<InvalidMortgageFactorResult>(
                    "dbo.sp_GetInvalidMortgageFactors",
                    new
                    {
                        DatasetId = datasetId,
                        SecurityId = securityId,
                        StartDate = startDate,
                        EndDate = endDate,
                        UpperLimit = upperLimit,
                        LowerLimit = lowerLimit
                    },
                    commandType: CommandType.StoredProcedure);

                return rows.ToList();
            }
            finally
            {
                if (wasClosed)
                    await connection.CloseAsync();
            }
        }
    }
}
