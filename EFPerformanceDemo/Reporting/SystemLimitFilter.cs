namespace EFPerformanceDemo.Reporting
{
    /// <summary>
    /// Result of resolving a start/end date range from report filter parameters.
    /// </summary>
    public class DateRangeResult
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// Helpers for parsing/validating report filter parameters and for
    /// evaluating whether a factor price date is "stale" relative to a security price date.
    /// </summary>
    public static class SystemLimitFilter
    {
        /// <summary>
        /// Validates/normalizes the raw filter parameters supplied by the caller.
        /// When <paramref name="required"/> is true and no parameters are present, throws.
        /// </summary>
        public static List<FilterParameter> ProcessFilterParameterValues(
            IEnumerable<FilterParameter> availableParameters,
            IEnumerable<FilterParameter>? submittedParameters,
            bool required)
        {
            var submitted = submittedParameters?.ToList() ?? new List<FilterParameter>();

            if (required && !submitted.Any())
            {
                throw new ArgumentException("No filter parameters were supplied.");
            }

            var allowedNames = availableParameters.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return submitted.Where(p => allowedNames.Contains(p.Name)).ToList();
        }

        /// <summary>
        /// Extracts a numeric filter value by name. Returns <paramref name="defaultValue"/>
        /// when not present (or throws when <paramref name="required"/> is true and missing).
        /// </summary>
        public static double ProcessFilterParametersName(
            IEnumerable<FilterParameter> filterParameters,
            string parameterName,
            bool required,
            double defaultValue = 0)
        {
            var match = filterParameters.FirstOrDefault(p =>
                string.Equals(p.Name, parameterName, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                if (required)
                    throw new ArgumentException($"Required filter parameter '{parameterName}' was not supplied.");

                return defaultValue;
            }

            if (!double.TryParse(match.Value, out var value))
            {
                throw new FormatException($"Filter parameter '{parameterName}' value '{match.Value}' is not a valid number.");
            }

            return value;
        }

        /// <summary>
        /// Returns true when the factor price date is within the allowed factor time range
        /// of the security price date (factorPriceDate should be on or before priceDate).
        /// </summary>
        public static bool CheckFactor(DateTime factorPriceDate, DateTime priceDate, int factorTimeRangeDays)
        {
            if (factorPriceDate > priceDate)
                return false;

            return (priceDate - factorPriceDate).TotalDays <= factorTimeRangeDays;
        }
    }

    /// <summary>
    /// Resolves the start/end date range for a report from its filter parameters,
    /// falling back to calendar-derived defaults when not explicitly supplied.
    /// </summary>
    public static class SystemLimitFilterParameters
    {
        public static DateRangeResult GetDateRangeFromFilterParameters(
            IEnumerable<FilterParameter> filterParameters,
            IBusinessCalendar calendar,
            DateTime defaultEndDate,
            bool required)
        {
            var parameters = filterParameters.ToList();

            var startDateText = parameters.FirstOrDefault(p =>
                string.Equals(p.Name, FilterParametersName.StartDateFilterName, StringComparison.OrdinalIgnoreCase))?.Value;
            var endDateText = parameters.FirstOrDefault(p =>
                string.Equals(p.Name, FilterParametersName.EndDateFilterName, StringComparison.OrdinalIgnoreCase))?.Value;

            if (required && startDateText == null && endDateText == null)
            {
                throw new ArgumentException("No date range filter parameters were supplied.");
            }

            var endDate = endDateText != null && DateTime.TryParse(endDateText, out var parsedEnd)
                ? parsedEnd
                : defaultEndDate;

            var startDate = startDateText != null && DateTime.TryParse(startDateText, out var parsedStart)
                ? parsedStart
                : calendar.GetPreviousBusinessDate(endDate);

            return new DateRangeResult
            {
                StartDate = startDate,
                EndDate = endDate
            };
        }
    }
}
