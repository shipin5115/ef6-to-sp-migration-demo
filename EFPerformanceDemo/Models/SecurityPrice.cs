using System.ComponentModel.DataAnnotations.Schema;

namespace EFPerformanceDemo.Models
{
    /// <summary>
    /// Raw security price record, including the factor applicable on that price date.
    /// A "factor" is the amortization multiplier applied to a mortgage-backed security's
    /// face value to arrive at its current outstanding (factored) value.
    /// </summary>
    [Table("SecurityPrices")]
    public class SecurityPrice
    {
        public int Id { get; set; }
        public int SecurityID { get; set; }
        public int DatasetId { get; set; }
        public DateTime PriceDate { get; set; }
        public decimal Price { get; set; }
        public double Factor { get; set; }

        public string DatasetName { get; set; } = string.Empty;
        public string EntityFactorName { get; set; } = string.Empty;
        public string TickerName { get; set; } = string.Empty;
        public string SecurityType { get; set; } = string.Empty;

        [ForeignKey(nameof(SecurityID))]
        public SecurityMaster? SecurityData { get; set; }
    }

    /// <summary>
    /// Static/reference data for a security (e.g. its ticker symbol).
    /// </summary>
    [Table("SecurityMasters")]
    public class SecurityMaster
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
    }
}
