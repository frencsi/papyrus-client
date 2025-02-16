namespace PapyrusClient.Models;

public record WorkScheduleFile
{
    private static long _counter = 0;

    public long Id { get; } = Interlocked.Increment(ref _counter);

    public required string Name { get; init; }

    public required long SizeInyBytes { get; init; }

    public required WorkSchedule? WorkSchedule { get; init; }

    public required WorkScheduleFileStatus Status { get; init; }
}