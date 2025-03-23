namespace PapyrusClient.Models;

public record YearMonth(int Year, int Month)
{
    public static readonly YearMonth Default = new(1, 1);

    public DateOnly FirstDay { get; } = new(
        year: Year,
        month: Month,
        day: 1);

    public DateOnly LastDay { get; } = new(
        year: Year,
        month: Month,
        day: DateTime.DaysInMonth(Year, Month));

    public IEnumerable<DateOnly> GetDates()
    {
        var current = FirstDay;

        while (current <= LastDay)
        {
            yield return current;
            current = current.AddDays(1);
        }
    }
}