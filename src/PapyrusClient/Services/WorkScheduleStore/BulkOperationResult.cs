namespace PapyrusClient.Services.WorkScheduleStore;

public record BulkOperationResult(int Affected, int Total)
{
    public static readonly BulkOperationResult Empty = new(0, 0);
}