namespace EFPerformanceDemo.Models
{
    /// <summary>
    /// Wrapper exposing the security's own price date, used when joining
    /// a price row back to its originating security-price record.
    /// </summary>
    public class SecurityDateInfo
    {
        public DateTime PriceDate { get; set; }
    }

    /// <summary>
    /// Wrapper exposing the human-readable dataset name for a price row.
    /// </summary>
    public class DatasetNameInfo
    {
        public string DatasetName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Row returned by <c>AppDbContext.GetCalculatedSecurityFactorPriceByRange</c>:
    /// one unfactored security price within the requested date range, along with
    /// the descriptive fields needed to project the final report row.
    /// </summary>
    public class SecurityPriceRangeResult
    {
        public int SecurityId { get; set; }
        public DateTime PriceDate { get; set; }
        public decimal Price { get; set; }

        public SecurityDateInfo SecurityDate { get; set; } = new();
        public DatasetNameInfo DatasetName { get; set; } = new();
        public string EntityFactorName { get; set; } = string.Empty;
        public string TickerName { get; set; } = string.Empty;
        public string SecurityType { get; set; } = string.Empty;
        public SecurityMaster SecurityData { get; set; } = new();
    }
}
