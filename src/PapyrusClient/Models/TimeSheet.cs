namespace PapyrusClient.Models;

public record TimeSheet(
    string Employee,
    string Company,
    string Location,
    (int Year, int Month) YearMonth,
    WorkType Type,
    IEnumerable<WorkShift> Shifts)
{
    public WorkOptions? Options { get; init; }
    
    public DateOnly FirstDateOfYearMonth => new(
        year: YearMonth.Year,
        month: YearMonth.Month,
        day: 1);

    public DateOnly LastDateOfYearMonth => new(
        year: YearMonth.Year,
        month: YearMonth.Month,
        day: DateTime.DaysInMonth(YearMonth.Year, YearMonth.Month));
    
    public IEnumerable<DateOnly> GetYearMonthDates()
    {
        return Enumerable
            .Range(FirstDateOfYearMonth.Day, LastDateOfYearMonth.Day)
            .Select(day => new DateOnly(
                year: YearMonth.Year,
                month: YearMonth.Month,
                day: day));
    }
}