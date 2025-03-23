using System.Collections.Concurrent;

namespace PapyrusClient.Services.WorkScheduleStore;

public partial class InMemoryWorkScheduleStore : IWorkScheduleStore
{
    private readonly ILogger<InMemoryWorkScheduleStore> _logger;

    private readonly ConcurrentDictionary<int, WorkSchedule> _workSchedules;

    private readonly SubscriptionStore<StoreInitializedArgs> _storeInitializedSubscriptions;
    private readonly SubscriptionStore<StoreClearedArgs> _storeClearedSubscriptions;
    private readonly SubscriptionStore<WorkScheduleAddedArgs> _workScheduleAddedSubscriptions;
    private readonly SubscriptionStore<WorkScheduleRemovedArgs> _workScheduleRemovedSubscriptions;
    private readonly SubscriptionStore<WorkScheduleUpdatedArgs> _workScheduleUpdatedSubscriptions;
    private readonly SubscriptionStore<WorkScheduleBulkAddedArgs> _workScheduleBulkAddedSubscriptions;
    private readonly SubscriptionStore<WorkScheduleBulkRemovedArgs> _workScheduleBulkRemovedSubscriptions;
    private readonly SubscriptionStore<WorkScheduleBulkUpdatedArgs> _workScheduleBulkUpdatedSubscriptions;

    private volatile bool _disposed;

    #region Constructor

    public InMemoryWorkScheduleStore(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger<InMemoryWorkScheduleStore>();

        _workSchedules = new ConcurrentDictionary<int, WorkSchedule>(-1, 512);

        _storeInitializedSubscriptions = CreateSubStore<StoreInitializedArgs>(loggerFactory);
        _storeClearedSubscriptions = CreateSubStore<StoreClearedArgs>(loggerFactory);
        _workScheduleAddedSubscriptions = CreateSubStore<WorkScheduleAddedArgs>(loggerFactory);
        _workScheduleRemovedSubscriptions = CreateSubStore<WorkScheduleRemovedArgs>(loggerFactory);
        _workScheduleUpdatedSubscriptions = CreateSubStore<WorkScheduleUpdatedArgs>(loggerFactory);
        _workScheduleBulkAddedSubscriptions = CreateSubStore<WorkScheduleBulkAddedArgs>(loggerFactory);
        _workScheduleBulkRemovedSubscriptions = CreateSubStore<WorkScheduleBulkRemovedArgs>(loggerFactory);
        _workScheduleBulkUpdatedSubscriptions = CreateSubStore<WorkScheduleBulkUpdatedArgs>(loggerFactory);
    }

    #endregion

    #region IAsyncDisposable implementation

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await _storeInitializedSubscriptions.DisposeAsync();
        await _storeClearedSubscriptions.DisposeAsync();
        await _workScheduleAddedSubscriptions.DisposeAsync();
        await _workScheduleRemovedSubscriptions.DisposeAsync();
        await _workScheduleUpdatedSubscriptions.DisposeAsync();
        await _workScheduleBulkAddedSubscriptions.DisposeAsync();
        await _workScheduleBulkRemovedSubscriptions.DisposeAsync();
        await _workScheduleBulkUpdatedSubscriptions.DisposeAsync();

        _workSchedules.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Properties

    public IQueryable<WorkSchedule> WorkSchedules
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
            return _workSchedules.Values.AsQueryable();
        }
    }

    public long Count
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
            return _workSchedules.Count;
        }
    }

    #endregion

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));

        cancellationToken.ThrowIfCancellationRequested();

        await _storeInitializedSubscriptions.RaiseAllAsync(new StoreInitializedArgs());
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));

        cancellationToken.ThrowIfCancellationRequested();

        _workSchedules.Clear();

        await _storeClearedSubscriptions.RaiseAllAsync(new StoreClearedArgs());
    }

    public async Task<bool> AddAsync(WorkSchedule workSchedule, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
        ArgumentNullException.ThrowIfNull(workSchedule);

        cancellationToken.ThrowIfCancellationRequested();

        if (!_workSchedules.TryAdd(workSchedule.Id, workSchedule))
        {
            LogFailedToAddWorkScheduleKeyAlreadyExists(_logger, workSchedule.Id);
            return false;
        }

        await _workScheduleAddedSubscriptions.RaiseAllAsync(new WorkScheduleAddedArgs(workSchedule));
        return true;
    }

    public async Task<bool> RemoveAsync(WorkSchedule workSchedule, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
        ArgumentNullException.ThrowIfNull(workSchedule);

        cancellationToken.ThrowIfCancellationRequested();

        if (!_workSchedules.TryRemove(workSchedule.Id, out _))
        {
            LogFailedToRemoveWorkScheduleNotFound(_logger, workSchedule.Id);
            return false;
        }

        await _workScheduleRemovedSubscriptions.RaiseAllAsync(new WorkScheduleRemovedArgs(workSchedule));
        return true;
    }

    public async Task<bool> UpdateAsync(WorkSchedule oldWorkSchedule, WorkSchedule newWorkSchedule,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
        ArgumentNullException.ThrowIfNull(oldWorkSchedule);
        ArgumentNullException.ThrowIfNull(newWorkSchedule);

        cancellationToken.ThrowIfCancellationRequested();

        if (!_workSchedules.TryUpdate(oldWorkSchedule.Id, newWorkSchedule, oldWorkSchedule))
        {
            LogFailedToUpdateWorkScheduleNotFound(_logger, oldWorkSchedule.Id);
            return false;
        }

        await _workScheduleUpdatedSubscriptions.RaiseAllAsync(new WorkScheduleUpdatedArgs(oldWorkSchedule,
            newWorkSchedule));
        return true;
    }

    public async Task<BulkOperationResult> AddBulkAsync(IEnumerable<WorkSchedule> workSchedules,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
        ArgumentNullException.ThrowIfNull(workSchedules);

        cancellationToken.ThrowIfCancellationRequested();

        var affected = 0;
        var total = 0;

        foreach (var workSchedule in workSchedules)
        {
            if (!_workSchedules.TryAdd(workSchedule.Id, workSchedule))
            {
                LogFailedToAddWorkScheduleKeyAlreadyExists(_logger, workSchedule.Id);
            }
            else
            {
                affected += 1;
            }

            total += 1;
        }

        if (total == 0)
        {
            return BulkOperationResult.Empty;
        }

        var result = new BulkOperationResult(affected, total);

        await _workScheduleBulkAddedSubscriptions.RaiseAllAsync(new WorkScheduleBulkAddedArgs(result));

        return result;
    }

    public async Task<BulkOperationResult> RemoveBulkAsync(IEnumerable<WorkSchedule> workSchedules,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
        ArgumentNullException.ThrowIfNull(workSchedules);

        cancellationToken.ThrowIfCancellationRequested();

        var affected = 0;
        var total = 0;

        foreach (var workSchedule in workSchedules)
        {
            if (!_workSchedules.TryRemove(workSchedule.Id, out _))
            {
                LogFailedToRemoveWorkScheduleNotFound(_logger, workSchedule.Id);
            }
            else
            {
                affected += 1;
            }

            total += 1;
        }

        if (total == 0)
        {
            return BulkOperationResult.Empty;
        }

        var result = new BulkOperationResult(affected, total);

        await _workScheduleBulkRemovedSubscriptions.RaiseAllAsync(new WorkScheduleBulkRemovedArgs(result));

        return result;
    }

    public async Task<BulkOperationResult> UpdateBulkAsync(
        IEnumerable<(WorkSchedule Old, WorkSchedule New)> workSchedules,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
        ArgumentNullException.ThrowIfNull(workSchedules);

        cancellationToken.ThrowIfCancellationRequested();

        var affected = 0;
        var total = 0;

        foreach (var workSchedule in workSchedules)
        {
            if (!_workSchedules.TryUpdate(workSchedule.Old.Id, workSchedule.New, workSchedule.Old))
            {
                LogFailedToUpdateWorkScheduleNotFound(_logger, workSchedule.Old.Id);
            }
            else
            {
                affected += 1;
            }

            total += 1;
        }

        if (total == 0)
        {
            return BulkOperationResult.Empty;
        }

        var result = new BulkOperationResult(affected, total);

        await _workScheduleBulkUpdatedSubscriptions.RaiseAllAsync(new WorkScheduleBulkUpdatedArgs(result));

        return result;
    }

    #region Subscriptions

    public IAsyncDisposable OnStoreInitialized(Func<StoreInitializedArgs, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
        ArgumentNullException.ThrowIfNull(callback);

        return _storeInitializedSubscriptions.Add(callback);
    }

    public IAsyncDisposable OnStoreCleared(Func<StoreClearedArgs, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
        ArgumentNullException.ThrowIfNull(callback);

        return _storeClearedSubscriptions.Add(callback);
    }

    public IAsyncDisposable OnWorkScheduleAdded(Func<WorkScheduleAddedArgs, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
        ArgumentNullException.ThrowIfNull(callback);

        return _workScheduleAddedSubscriptions.Add(callback);
    }

    public IAsyncDisposable OnWorkScheduleRemoved(Func<WorkScheduleRemovedArgs, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
        ArgumentNullException.ThrowIfNull(callback);

        return _workScheduleRemovedSubscriptions.Add(callback);
    }

    public IAsyncDisposable OnWorkScheduleUpdated(Func<WorkScheduleUpdatedArgs, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
        ArgumentNullException.ThrowIfNull(callback);

        return _workScheduleUpdatedSubscriptions.Add(callback);
    }

    public IAsyncDisposable OnWorkScheduleBulkAdded(Func<WorkScheduleBulkAddedArgs, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
        ArgumentNullException.ThrowIfNull(callback);

        return _workScheduleBulkAddedSubscriptions.Add(callback);
    }

    public IAsyncDisposable OnWorkScheduleBulkRemoved(Func<WorkScheduleBulkRemovedArgs, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
        ArgumentNullException.ThrowIfNull(callback);

        return _workScheduleBulkRemovedSubscriptions.Add(callback);
    }

    public IAsyncDisposable OnWorkScheduleBulkUpdated(Func<WorkScheduleBulkUpdatedArgs, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(InMemoryWorkScheduleStore));
        ArgumentNullException.ThrowIfNull(callback);

        return _workScheduleBulkUpdatedSubscriptions.Add(callback);
    }

    #endregion

    #region Helpers

    private static SubscriptionStore<TSubscriptionArgs> CreateSubStore<TSubscriptionArgs>(
        ILoggerFactory loggerFactory)
        where TSubscriptionArgs : ISubscriptionArgs
    {
        return new SubscriptionStore<TSubscriptionArgs>(
            loggerFactory.CreateLogger<SubscriptionStore<TSubscriptionArgs>>());
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "Failed to add work schedule to the store: The work schedule with ID '{workScheduleId}' already exists.")]
    private static partial void LogFailedToAddWorkScheduleKeyAlreadyExists(ILogger logger, int workScheduleId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "Failed to remove work schedule from the store: The work schedule with ID '{workScheduleId}' was not found.")]
    private static partial void LogFailedToRemoveWorkScheduleNotFound(ILogger logger, int workScheduleId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "Failed to update work schedule in the store: The work schedule with ID '{workScheduleId}' was not found.")]
    private static partial void LogFailedToUpdateWorkScheduleNotFound(ILogger logger, int workScheduleId);

    #endregion
}