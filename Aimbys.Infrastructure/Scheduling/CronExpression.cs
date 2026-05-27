namespace Aimbys.Infrastructure.Scheduling;

/// <summary>
/// Minimal 5-field cron parser: <c>minute hour day-of-month month day-of-week</c>.
///
/// <para>
/// Supported syntax in each field:
/// </para>
/// <list type="bullet">
///   <item><c>*</c> &mdash; wildcard (every value).</item>
///   <item><c>N</c> &mdash; literal value.</item>
///   <item><c>N,M,...</c> &mdash; explicit list.</item>
///   <item><c>N-M</c> &mdash; inclusive range.</item>
///   <item><c>*&#47;N</c> &mdash; step (e.g. <c>*/15</c> in minutes for every 15 min).</item>
/// </list>
///
/// Day-of-week: 0..6 (Sunday=0). Month: 1..12.
///
/// <para>
/// Suitable for the platform's scheduling needs (hourly sweeps, weekly
/// retention runs); not a full-fidelity cron implementation. Switch to
/// NCronTab/Hangfire when richer expressions become necessary.
/// </para>
/// </summary>
public sealed class CronExpression
{
    private readonly bool[] _minutes;
    private readonly bool[] _hours;
    private readonly bool[] _dayOfMonth;
    private readonly bool[] _month;
    private readonly bool[] _dayOfWeek;

    /// <summary>The original expression string, kept for diagnostics.</summary>
    public string Expression { get; }

    private CronExpression(
        string expression,
        bool[] minutes,
        bool[] hours,
        bool[] dayOfMonth,
        bool[] month,
        bool[] dayOfWeek)
    {
        Expression = expression;
        _minutes = minutes;
        _hours = hours;
        _dayOfMonth = dayOfMonth;
        _month = month;
        _dayOfWeek = dayOfWeek;
    }

    /// <summary>
    /// Parses a 5-field cron expression. Throws
    /// <see cref="FormatException"/> on malformed input so calling
    /// code (<c>SchedulingService.ScheduleRecurringAsync</c>) can
    /// validate at registration time rather than at first dispatch.
    /// </summary>
    public static CronExpression Parse(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new FormatException("Cron expression must be non-empty.");

        var fields = expression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 5)
            throw new FormatException(
                $"Cron expression must have 5 fields (minute hour day-of-month month day-of-week); got {fields.Length}.");

        return new CronExpression(
            expression,
            ParseField(fields[0], min: 0,  max: 59, name: "minute"),
            ParseField(fields[1], min: 0,  max: 23, name: "hour"),
            ParseField(fields[2], min: 1,  max: 31, name: "day-of-month"),
            ParseField(fields[3], min: 1,  max: 12, name: "month"),
            ParseField(fields[4], min: 0,  max: 6,  name: "day-of-week"));
    }

    /// <summary>
    /// Returns the next UTC instant strictly greater than
    /// <paramref name="afterUtc"/> that matches this expression.
    /// Walks minute-by-minute; bounded to a one-year search window so
    /// a pathological expression can never spin forever.
    /// </summary>
    public DateTime GetNextOccurrence(DateTime afterUtc)
    {
        // Start from the next whole minute.
        var candidate = new DateTime(
            afterUtc.Year, afterUtc.Month, afterUtc.Day,
            afterUtc.Hour, afterUtc.Minute, 0,
            DateTimeKind.Utc).AddMinutes(1);

        var ceiling = candidate.AddYears(1);
        while (candidate < ceiling)
        {
            if (Matches(candidate))
            {
                return candidate;
            }
            candidate = candidate.AddMinutes(1);
        }

        throw new InvalidOperationException(
            $"Cron expression '{Expression}' has no occurrence within a year of {afterUtc:u}.");
    }

    private bool Matches(DateTime utc)
    {
        return _minutes[utc.Minute]
            && _hours[utc.Hour]
            && _dayOfMonth[utc.Day]
            && _month[utc.Month]
            && _dayOfWeek[(int)utc.DayOfWeek];
    }

    /// <summary>
    /// Parses one cron field into a boolean lookup. Uses an inclusive
    /// 0..max array (with index 0 unused for 1-based fields like month
    /// and day-of-month) so <see cref="Matches"/> can index by the raw
    /// <see cref="DateTime"/> component.
    /// </summary>
    private static bool[] ParseField(string field, int min, int max, string name)
    {
        var allowed = new bool[max + 1];

        foreach (var part in field.Split(','))
        {
            var token = part.Trim();
            if (token.Length == 0)
                throw new FormatException($"Cron {name} field has empty list element.");

            // Step form: */N or N-M/N
            int step = 1;
            string rangePart = token;
            var slashIdx = token.IndexOf('/');
            if (slashIdx >= 0)
            {
                rangePart = token[..slashIdx];
                if (!int.TryParse(token[(slashIdx + 1)..], out step) || step <= 0)
                    throw new FormatException($"Cron {name} step must be a positive integer.");
            }

            int rangeStart, rangeEnd;
            if (rangePart == "*")
            {
                rangeStart = min;
                rangeEnd = max;
            }
            else if (rangePart.Contains('-'))
            {
                var parts = rangePart.Split('-');
                if (parts.Length != 2
                    || !int.TryParse(parts[0], out rangeStart)
                    || !int.TryParse(parts[1], out rangeEnd))
                    throw new FormatException($"Cron {name} range '{rangePart}' is invalid.");
            }
            else
            {
                if (!int.TryParse(rangePart, out var literal))
                    throw new FormatException($"Cron {name} value '{rangePart}' is not an integer.");
                rangeStart = rangeEnd = literal;
            }

            if (rangeStart < min || rangeEnd > max || rangeStart > rangeEnd)
                throw new FormatException(
                    $"Cron {name} range {rangeStart}-{rangeEnd} out of bounds {min}-{max}.");

            for (int i = rangeStart; i <= rangeEnd; i += step)
            {
                allowed[i] = true;
            }
        }

        return allowed;
    }
}
