namespace PapyrusClient.Services.WorkScheduleStore;

public interface IWorkScheduleStore : IAsyncDisposable
{
    IQueryable<WorkSchedule> WorkSchedules { get; }

    public long Count { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);

    Task<bool> AddAsync(WorkSchedule workSchedule, CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(WorkSchedule workSchedule, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(WorkSchedule oldWorkSchedule, WorkSchedule newWorkSchedule,
        CancellationToken cancellationToken = default);

    Task<BulkOperationResult> AddBulkAsync(IEnumerable<WorkSchedule> workSchedules,
        CancellationToken cancellationToken = default);

    Task<BulkOperationResult> RemoveBulkAsync(IEnumerable<WorkSchedule> workSchedules,
        CancellationToken cancellationToken = default);

    Task<BulkOperationResult> UpdateBulkAsync(IEnumerable<(WorkSchedule Old, WorkSchedule New)> workSchedules,
        CancellationToken cancellationToken = default);

    IAsyncDisposable OnStoreInitialized(Func<StoreInitializedArgs, Task> callback);

    IAsyncDisposable OnStoreCleared(Func<StoreClearedArgs, Task> callback);

    IAsyncDisposable OnWorkScheduleAdded(Func<WorkScheduleAddedArgs, Task> callback);

    IAsyncDisposable OnWorkScheduleRemoved(Func<WorkScheduleRemovedArgs, Task> callback);

    IAsyncDisposable OnWorkScheduleUpdated(Func<WorkScheduleUpdatedArgs, Task> callback);

    IAsyncDisposable OnWorkScheduleBulkAdded(Func<WorkScheduleBulkAddedArgs, Task> callback);

    IAsyncDisposable OnWorkScheduleBulkRemoved(Func<WorkScheduleBulkRemovedArgs, Task> callback);

    IAsyncDisposable OnWorkScheduleBulkUpdated(Func<WorkScheduleBulkUpdatedArgs, Task> callback);
}