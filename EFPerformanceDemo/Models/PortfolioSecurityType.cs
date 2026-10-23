using System.ComponentModel.DataAnnotations.Schema;

namespace EFPerformanceDemo.Models
{
    /// <summary>
    /// Known security type codes used by the Mortgage Factor Report.
    /// </summary>
    public enum PortfolioSecurityType
    {
        Unknown = 0,
        Mortgage = 1,
        Equity = 2,
        Bond = 3
    }

    /// <summary>
    /// Maps a dataset to the security type and the factor time range (in days)
    /// used to determine whether a factor price is "stale" relative to a security price.
    /// </summary>
    [Table("PortfolioSecurityTypes")]
    public class PortfolioSecurityTypeEntity
    {
        public int Id { get; set; }
        public int SecurityTypeId { get; set; }
        public int DataId { get; set; }
        public int FactorTimeRange { get; set; }
    }
}
