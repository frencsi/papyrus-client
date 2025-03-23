namespace PapyrusClient.Services.WorkScheduleReader;

public interface IWorkScheduleReader
{
    IReadOnlySet<string> SupportedFileExtensions { get; }

    long MaxFileSizeInBytes { get; }

    Task<WorkSchedule> ReadAsync(string fileName, Stream fileSource, WorkScheduleOptions options,
        CancellationToken cancellationToken = default);
}