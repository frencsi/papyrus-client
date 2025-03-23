using System.Collections.Concurrent;

namespace PapyrusClient.Utilities;

public partial class SubscriptionStore<TSubscriptionArgs>(
    ILogger<SubscriptionStore<TSubscriptionArgs>> logger)
    : IAsyncDisposable
    where TSubscriptionArgs : ISubscriptionArgs
{
    private int _nextSubscriptionId;

    private readonly ConcurrentDictionary<int, Subscription<TSubscriptionArgs>> _subscriptions = new(-1, 16);

    private volatile bool _disposed;

    protected virtual ValueTask DisposeAsyncCore()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        _subscriptions.Clear();

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    public Subscription<TSubscriptionArgs> Add(Func<TSubscriptionArgs, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SubscriptionStore<TSubscriptionArgs>));

        var subscription = new Subscription<TSubscriptionArgs>(
            id: Interlocked.Increment(ref _nextSubscriptionId),
            callback: callback,
            onDispose: InternalRemove);

        if (!_subscriptions.TryAdd(subscription.Id, subscription))
        {
            LogFailedToAddSubscriptionKeyAlreadyExists(logger, typeof(TSubscriptionArgs), subscription.Id);
            throw new InvalidOperationException(
                $"Failed to add subscription: A duplicate key ('{subscription.Id}') with type '{typeof(TSubscriptionArgs)}' was detected in the subscription store.");
        }

        return subscription;
    }

    public async Task RaiseAllAsync(TSubscriptionArgs args)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SubscriptionStore<TSubscriptionArgs>));

        var tasks = _subscriptions.Select(pair => pair.Value.ExecuteAsync(args));

        await foreach (var task in Task.WhenEach(tasks))
        {
            if (task.Exception is not null)
            {
                LogFailedToExecuteSubscriptionCallback(logger, task.Exception, typeof(TSubscriptionArgs), task.Id);
            }
        }
    }

    private ValueTask InternalRemove(Subscription<TSubscriptionArgs> subscription)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SubscriptionStore<TSubscriptionArgs>));

        _subscriptions.TryRemove(subscription.Id, out _);

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "Failed to add subscription: A duplicate key ('{SubscriptionId}') with type '{SubscriptionType}' was detected in the subscription store.")]
    private static partial void LogFailedToAddSubscriptionKeyAlreadyExists(ILogger logger, Type subscriptionType,
        int subscriptionId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "Failed to execute subscription callback for subscription with id '{SubscriptionId}' and type '{SubscriptionType}'.")]
    private static partial void LogFailedToExecuteSubscriptionCallback(ILogger logger, Exception ex,
        Type subscriptionType, int subscriptionId);
}

public interface ISubscriptionArgs;

public sealed class Subscription<TSubscriptionArgs>(
    int id,
    Func<TSubscriptionArgs, Task> callback,
    Func<Subscription<TSubscriptionArgs>, ValueTask> onDispose)
    : IAsyncDisposable
    where TSubscriptionArgs : ISubscriptionArgs
{
    public int Id => id;

    public Task ExecuteAsync(TSubscriptionArgs args) => callback(args);

    public ValueTask DisposeAsync() => onDispose(this);
}