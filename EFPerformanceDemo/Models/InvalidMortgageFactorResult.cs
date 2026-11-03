namespace EFPerformanceDemo.Models
{
    /// <summary>
    /// Row returned by <c>dbo.sp_GetInvalidMortgageFactors</c>, mapped by Dapper.
    /// Column names/order must match the stored procedure's final SELECT exactly.
    /// </summary>
    public class InvalidMortgageFactorResult
    {
        public string DatasetName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string TickerName { get; set; } = string.Empty;
        public string SecurityType { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal UnfactoredPrice { get; set; }
        public double? Factor { get; set; }
        public decimal FactoredPrice { get; set; }
        public PositionFactorStateConstants FactorStatus { get; set; }
    }
}
