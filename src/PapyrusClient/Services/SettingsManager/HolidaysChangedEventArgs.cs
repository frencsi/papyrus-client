namespace PapyrusClient.Services.SettingsManager;

public class HolidaysChangedEventArgs(IReadOnlySet<DateOnly> newHolidays) : EventArgs
{
    public IReadOnlySet<DateOnly> NewHolidays { get; } = newHolidays;
}