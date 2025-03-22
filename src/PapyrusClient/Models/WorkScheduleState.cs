namespace PapyrusClient.Models;

public enum WorkScheduleState : byte
{
    Ok = 0,
    ReadError = 1,
    ValidateError = 2,
    GeneralError = 3
}