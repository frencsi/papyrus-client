using PapyrusClient.Services.WorkScheduleReader;
using PapyrusClient.Services.WorkScheduleValidator;

namespace PapyrusClient.Models;

public record WorkScheduleFile
{
    public enum State : byte
    {
        Ok = 0,
        ReadError = 1,
        ValidateError = 2,
        GeneralError = 3
    }

    private static long _counter = 0;

    public WorkScheduleFile(string name, long sizeInyBytes, WorkSchedule workSchedule)
    {
        Id = Interlocked.Increment(ref _counter);

        Name = name;
        SizeInyBytes = sizeInyBytes;

        WorkSchedule = workSchedule;
        Exception = null;

        Status = State.Ok;
    }

    public WorkScheduleFile(string name, long sizeInyBytes, Exception exception)
    {
        Id = Interlocked.Increment(ref _counter);

        Name = name;
        SizeInyBytes = sizeInyBytes;

        WorkSchedule = null;
        Exception = exception;

        Status = exception switch
        {
            WorkScheduleExcelReaderException => State.ReadError,
            WorkScheduleValidatorException => State.ValidateError,
            _ => State.GeneralError
        };
    }


    public long Id { get; }

    public string Name { get; }

    public long SizeInyBytes { get; }

    public WorkSchedule? WorkSchedule { get; }

    public Exception? Exception { get; }

    public State Status { get; }
}