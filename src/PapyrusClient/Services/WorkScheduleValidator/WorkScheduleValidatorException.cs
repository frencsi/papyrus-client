namespace PapyrusClient.Services.WorkScheduleValidator;

public class WorkScheduleValidatorException : Exception
{
    public WorkScheduleValidatorException()
    {
    }

    public WorkScheduleValidatorException(string message)
        : base(message)
    {
    }

    public WorkScheduleValidatorException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public required string Details { get; init; }
}