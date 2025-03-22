namespace PapyrusClient.Models;

public record WorkSchedule(
    Company Company,
    Location Location,
    YearMonth YearMonth,
    WorkScheduleType Type,
    IReadOnlyList<WorkShift> Shifts,
    WorkScheduleMetadata Metadata,
    WorkScheduleValidationRule? Rule,
    Exception? Exception)
{
    private static int _nextId = 0;

    public int Id { get; } = Interlocked.Increment(ref _nextId);

    public WorkScheduleState State => Exception switch
    {
        null => WorkScheduleState.Ok,
        WorkScheduleReaderException => WorkScheduleState.ReadError,
        WorkScheduleValidatorException => WorkScheduleState.ValidateError,
        _ => WorkScheduleState.GeneralError
    };
}