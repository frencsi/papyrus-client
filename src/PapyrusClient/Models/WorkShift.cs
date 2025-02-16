namespace PapyrusClient.Models;

public record WorkShift(DateOnly Date, string Employee, WorkShift.Time Start, WorkShift.Time End)
{
    public readonly record struct Time(TimeSpan Value, bool HasContinuationMarker);

    public TimeSpan Duration => Start.HasContinuationMarker && End.HasContinuationMarker
        ? TimeSpan.FromHours(24)
        : End.Value - Start.Value;
}