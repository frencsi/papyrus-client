namespace PapyrusClient.Services.WorkScheduleReader;

public class WorkScheduleReaderException : Exception
{
    public WorkScheduleReaderException()
    {
    }

    public WorkScheduleReaderException(string message)
        : base(message)
    {
    }

    public WorkScheduleReaderException(string message, Exception inner)
        : base(message, inner)
    {
    }

    public required string Details { get; init; }
}