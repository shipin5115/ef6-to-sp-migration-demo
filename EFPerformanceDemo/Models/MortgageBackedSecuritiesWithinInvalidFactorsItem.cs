namespace EFPerformanceDemo.Models
{
    /// <summary>
    /// Indicates why a security price row is flagged by the Mortgage Factor Report.
    /// </summary>
    public enum PositionFactorStateConstants
    {
        /// <summary>No applicable factor was found, or the factor was zero.</summary>
        NullOrZeroFactor,

        /// <summary>A factor was found but it falls outside the configured upper/lower limits.</summary>
        InvalidFactor
    }

    /// <summary>
    /// Row returned by the Mortgage Factor Report, highlighting mortgage-backed
    /// securities whose applicable factor is missing, zero, or outside of configured limits.
    /// </summary>
    public class MortgageBackedSecuritiesWithinInvalidFactorsItem
    {
        public string DatasetName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string TickerName { get; set; } = string.Empty;
        public string SecurityType { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal UnfactoredPrice { get; set; }
        public double Factor { get; set; }
        public decimal FactoredPrice { get; set; }
        public PositionFactorStateConstants FactorStatus { get; set; }
    }
}
