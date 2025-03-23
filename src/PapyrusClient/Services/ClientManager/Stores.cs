namespace PapyrusClient.Services.ClientManager;

public abstract record StorageData(int Version);

public record CultureStorageData(string Name) : StorageData(1);

public record ThemeStorageData(string Mode) : StorageData(1);

public record HolidaysStorageData(IEnumerable<DateOnly> Dates) : StorageData(1);