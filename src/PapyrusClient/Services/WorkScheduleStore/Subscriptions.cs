namespace PapyrusClient.Services.WorkScheduleStore;

public readonly record struct StoreInitializedArgs : ISubscriptionArgs;

public readonly record struct StoreClearedArgs : ISubscriptionArgs;

public readonly record struct WorkScheduleAddedArgs(WorkSchedule NewWorkSchedule) : ISubscriptionArgs;

public readonly record struct WorkScheduleRemovedArgs(WorkSchedule RemovedSchedule) : ISubscriptionArgs;

public readonly record struct WorkScheduleUpdatedArgs(WorkSchedule OldWorkSchedule, WorkSchedule NewWorkSchedule)
    : ISubscriptionArgs;

public readonly record struct WorkScheduleBulkAddedArgs(BulkOperationResult Result) : ISubscriptionArgs;

public readonly record struct WorkScheduleBulkRemovedArgs(BulkOperationResult Result) : ISubscriptionArgs;

public readonly record struct WorkScheduleBulkUpdatedArgs(BulkOperationResult Result) : ISubscriptionArgs;