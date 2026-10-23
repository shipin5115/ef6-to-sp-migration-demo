namespace EFPerformanceDemo.Reporting
{
    /// <summary>
    /// Well-known filter parameter names accepted by the Mortgage Factor Report.
    /// </summary>
    public static class FilterParametersName
    {
        public const string UpperLimitValueFilterName = "UpperLimitValueFilter";
        public const string LowerLimitValueFilterName = "LowerLimitValueFilter";
        public const string StartDateFilterName = "StartDateFilter";
        public const string EndDateFilterName = "EndDateFilter";
    }

    /// <summary>
    /// A single filter parameter name/value pair submitted by the report caller.
    /// </summary>
    public class FilterParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Carries the request-scoped information needed to build a report query:
    /// the raw filter parameters supplied by the caller, the selected dataset,
    /// and the security identifier (or -1 for "all securities").
    /// </summary>
    public class ReportContext
    {
        public List<FilterParameter> FilterParameters { get; set; } = new();
        public int DatasetId { get; set; }
        public int SecurityId { get; set; } = -1;

        /// <summary>
        /// Business calendar used to resolve "previous business day" and
        /// filter-driven date ranges. A simple weekday-only calendar is used by default.
        /// </summary>
        public IBusinessCalendar Calendar { get; set; } = new WeekdayBusinessCalendar();
    }

    /// <summary>
    /// Abstraction over a trading/business-day calendar.
    /// </summary>
    public interface IBusinessCalendar
    {
        DateTime GetPreviousBusinessDate(DateTime fromDate);
    }

    /// <summary>
    /// Simple calendar that treats Saturday/Sunday as non-business days.
    /// Does not account for holidays.
    /// </summary>
    public class WeekdayBusinessCalendar : IBusinessCalendar
    {
        public DateTime GetPreviousBusinessDate(DateTime fromDate)
        {
            var date = fromDate.Date.AddDays(-1);
            while (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                date = date.AddDays(-1);
            }
            return date;
        }
    }
}
