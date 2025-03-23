namespace PapyrusClient.Services.TimeSheetWriter;

public interface ITimeSheetWriter
{
    Task<string> WriteAsZipAsync(IEnumerable<WorkSchedule> workSchedules, Holidays holidays, Stream zipStream,
        bool leaveOpen, CancellationToken cancellationToken = default);
}