namespace PapyrusClient.Models;

public record WorkSchedule(
    string Company,
    string Location,
    (int Year, int Month) YearMonth,
    WorkType Type,
    IReadOnlyList<WorkShift> Shifts)
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

    public IEnumerable<TimeSheet> ToTimeSheets()
    {
        return Shifts
            .GroupBy(shift => shift.Employee)
            .Select(employeeShifts =>
                new TimeSheet(
                    Employee: employeeShifts.Key,
                    Company: Company,
                    Location: Location,
                    YearMonth: YearMonth,
                    Type: Type,
                    Shifts: employeeShifts)
                {
                    Options = Options
                });
    }
}