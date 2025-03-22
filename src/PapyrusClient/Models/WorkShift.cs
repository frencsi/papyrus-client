namespace PapyrusClient.Models;

public record WorkShift(DateOnly Date, Employee Employee, WorkShiftTime Start, WorkShiftTime End)
{
    public TimeSpan ExactDuration()
    {
        return Start.HasContinuationMarker && End.HasContinuationMarker
            ? TimeSpan.FromHours(24)
            : End.Value - Start.Value;
    }

    public TimeSpan RoundedDuration()
    {
        var exactDuration = ExactDuration();

        var roundedDuration = TimeSpan.FromMinutes(Math.Round(exactDuration.TotalMinutes));

        return roundedDuration;
    }
}