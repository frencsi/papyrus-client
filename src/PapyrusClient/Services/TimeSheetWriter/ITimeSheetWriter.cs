using PapyrusClient.Models;

namespace PapyrusClient.Services.TimeSheetWriter;

public interface ITimeSheetWriter
{
    Task<string> CreateFileNameAsync(TimeSheet timeSheet, CancellationToken cancellationToken = default);

    Task WriteAsync(Stream destination, TimeSheet timeSheet, IReadOnlySet<DateOnly> holidays,
        CancellationToken cancellationToken = default);
}