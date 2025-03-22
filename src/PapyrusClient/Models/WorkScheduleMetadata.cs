namespace PapyrusClient.Models;

public enum WorkScheduleSource : byte
{
    Unknown = 0,
    File = 1
}

public record WorkScheduleMetadata(string Name, WorkScheduleSource Source, DateTimeOffset ReadAt);