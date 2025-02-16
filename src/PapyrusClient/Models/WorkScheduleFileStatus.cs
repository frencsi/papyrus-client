using PapyrusClient.Services.WorkScheduleReader;
using PapyrusClient.Services.WorkScheduleValidator;

namespace PapyrusClient.Models;

public enum WorkScheduleFileState : byte
{
    Ok = 0,
    ReadError = 1,
    ValidateError = 2,
    GeneralError = 3
}

public record WorkScheduleFileStatus
{
    private static readonly WorkScheduleFileStatus OkInstance = new()
    {
        State = WorkScheduleFileState.Ok,
        Details = null
    };

    public required WorkScheduleFileState State { get; init; }

    public required string? Details { get; init; }

    public static WorkScheduleFileStatus Ok()
    {
        return OkInstance;
    }

    public static WorkScheduleFileStatus Error(Exception exception)
    {
        return exception switch
        {
            WorkScheduleReaderException ex => new WorkScheduleFileStatus
            {
                State = WorkScheduleFileState.ReadError,
                Details = ex.Details
            },
            WorkScheduleValidatorException ex => new WorkScheduleFileStatus
            {
                State = WorkScheduleFileState.ValidateError,
                Details = ex.Details
            },
            _ => new WorkScheduleFileStatus
            {
                State = WorkScheduleFileState.GeneralError,
                Details = exception.ToString()
            }
        };
    }
}