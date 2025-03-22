using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PapyrusClient.Services.ClientManager;

public partial class ClientManager : IClientManager
{
    private const string
        EnumToString = "G",
        CultureStorageKey = "Culture",
        ThemeStorageKey = "Theme",
        HolidaysStorageKey = "Holidays",
        VersionFilePath = "/version.txt",
        DownloadFile = "downloadFileFromStream",
        JsStorageSet = "clientManagerStorage.set",
        JsStorageGet = "clientManagerStorage.get";

    private readonly ILogger<ClientManager> _logger;
    private readonly IJSRuntime _jsRuntime;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly SubscriptionStore<CultureChangedArgs> _cultureChangedSubscriptions;
    private readonly SubscriptionStore<ThemeChangedArgs> _themeChangedSubscriptions;
    private readonly SubscriptionStore<HolidaysChangedArgs> _holidaysChangedSubscriptions;
    private readonly SubscriptionStore<VersionChangedArgs> _versionChangedSubscriptions;
    private readonly SubscriptionStore<FileDownloadedArgs> _fileDownloadedSubscriptions;

    private volatile bool _disposed;

    private volatile bool _initialized;

    private readonly Lock _propertyLock;

    #region Constructor

    public ClientManager(ILoggerFactory loggerFactory, IJSRuntime jsRuntime, IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        _logger = loggerFactory.CreateLogger<ClientManager>();
        _jsRuntime = jsRuntime;
        _httpClientFactory = httpClientFactory;

        _cultureChangedSubscriptions = CreateSubscriptionStore<CultureChangedArgs>(loggerFactory);
        _themeChangedSubscriptions = CreateSubscriptionStore<ThemeChangedArgs>(loggerFactory);
        _holidaysChangedSubscriptions = CreateSubscriptionStore<HolidaysChangedArgs>(loggerFactory);
        _versionChangedSubscriptions = CreateSubscriptionStore<VersionChangedArgs>(loggerFactory);
        _fileDownloadedSubscriptions = CreateSubscriptionStore<FileDownloadedArgs>(loggerFactory);

        SupportedCultures =
        [
            new CultureInfo("en-US"),
            new CultureInfo("hu-HU")
        ];

        SupportedThemes =
        [
            DesignThemeModes.System,
            DesignThemeModes.Light,
            DesignThemeModes.Dark
        ];

        Culture = SupportedCultures[0];
        Theme = SupportedThemes[0];
        Holidays = Holidays.Empty;
        Version = ClientVersion.Unknown;

        _propertyLock = new Lock();
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

        await _cultureChangedSubscriptions.DisposeAsync();
        await _themeChangedSubscriptions.DisposeAsync();
        await _holidaysChangedSubscriptions.DisposeAsync();
        await _versionChangedSubscriptions.DisposeAsync();
        await _fileDownloadedSubscriptions.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Properties

    public IReadOnlyList<CultureInfo> SupportedCultures
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));
            return field;
        }
    }

    public IReadOnlyList<DesignThemeModes> SupportedThemes
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));
            return field;
        }
    }

    public CultureInfo Culture
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));
            return field;
        }
        private set;
    }

    public DesignThemeModes Theme
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));
            return field;
        }
        private set;
    }

    public Holidays Holidays
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));
            return field;
        }
        private set;
    }

    public ClientVersion Version
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));
            return field;
        }
        private set;
    }

    #endregion

    public async Task InitializeAsync(bool reload, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));

        if (_initialized && !reload)
        {
            return;
        }

        _initialized = true;

        var culture = await LoadCultureAsync(cancellationToken);
        var theme = await LoadThemeAsync(cancellationToken);
        var holidays = await LoadHolidaysAsync(cancellationToken);

        lock (_propertyLock)
        {
            Culture = culture;
            Theme = theme;
            Holidays = holidays;

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        if (reload)
        {
            await _cultureChangedSubscriptions.RaiseAllAsync(new CultureChangedArgs(culture));
            await _themeChangedSubscriptions.RaiseAllAsync(new ThemeChangedArgs(theme));
            await _holidaysChangedSubscriptions.RaiseAllAsync(new HolidaysChangedArgs(holidays));
        }
    }

    public async Task LoadVersionAsync(bool reload, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));

        if (Version != ClientVersion.Unknown && !reload)
        {
            return;
        }

        var version = await LoadVersionAsync(cancellationToken);

        lock (_propertyLock)
        {
            Version = version;
        }

        await _versionChangedSubscriptions.RaiseAllAsync(new VersionChangedArgs(version));
    }

    public async Task ChangeCultureAsync(CultureInfo culture, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));
        ArgumentNullException.ThrowIfNull(culture);

        lock (_propertyLock)
        {
            if (Culture.Equals(culture))
            {
                return;
            }
        }

        culture = await TrySaveItemOrDefaultAsync(
            key: CultureStorageKey,
            storage: new CultureStorageData(culture.Name),
            onDefault: () => new CultureStorageData(SupportedCultures[0].Name),
            onCheck: storage => SupportedCultures.Any(c => c.Name == storage.Name),
            onResult: storage => new CultureInfo(storage.Name),
            cancellationToken: cancellationToken);

        lock (_propertyLock)
        {
            Culture = culture;

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        await _cultureChangedSubscriptions.RaiseAllAsync(new CultureChangedArgs(culture));
    }

    public async Task ChangeThemeAsync(DesignThemeModes theme, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));

        lock (_propertyLock)
        {
            if (Theme == theme)
            {
                return;
            }
        }

        theme = await TrySaveItemOrDefaultAsync(
            key: ThemeStorageKey,
            storage: new ThemeStorageData(theme.ToString(EnumToString)),
            onDefault: () => new ThemeStorageData(SupportedThemes[0].ToString(EnumToString)),
            onCheck: storage => Enum.TryParse<DesignThemeModes>(storage.Mode, true, out var mode) &&
                                SupportedThemes.Contains(mode),
            onResult: storage => Enum.Parse<DesignThemeModes>(storage.Mode, true),
            cancellationToken: cancellationToken);

        lock (_propertyLock)
        {
            Theme = theme;
        }

        await _themeChangedSubscriptions.RaiseAllAsync(new ThemeChangedArgs(theme));
    }

    public async Task ChangeHolidaysAsync(Holidays holidays, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));
        ArgumentNullException.ThrowIfNull(holidays);

        lock (_propertyLock)
        {
            if (holidays.Equals(Holidays))
            {
                return;
            }
        }

        holidays = await TrySaveItemOrDefaultAsync(
            key: HolidaysStorageKey,
            storage: new HolidaysStorageData(holidays.Dates),
            onDefault: () => new HolidaysStorageData(Holidays.Empty.Dates),
            onCheck: _ => true,
            onResult: storage => new Holidays(storage.Dates),
            cancellationToken: cancellationToken);

        lock (_propertyLock)
        {
            Holidays = holidays;
        }

        await _holidaysChangedSubscriptions.RaiseAllAsync(new HolidaysChangedArgs(holidays));
    }

    public async Task DownloadFileAsync(string fileName, Stream stream, bool leaveOpen,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(stream);

        var sizeInBytes = stream.Length;

        await stream.FlushAsync(cancellationToken);
        stream.Seek(0, SeekOrigin.Begin);

        using (var jsStreamReference = new DotNetStreamReference(stream, leaveOpen))
        {
            await _jsRuntime.InvokeVoidAsync(DownloadFile, cancellationToken, fileName,
                jsStreamReference);
        }

        await _fileDownloadedSubscriptions.RaiseAllAsync(new FileDownloadedArgs(fileName, sizeInBytes));
    }

    #region Subscriptions

    public IAsyncDisposable OnCultureChanged(Func<CultureChangedArgs, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));
        ArgumentNullException.ThrowIfNull(callback);

        return _cultureChangedSubscriptions.Add(callback);
    }

    public IAsyncDisposable OnThemeChanged(Func<ThemeChangedArgs, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));
        ArgumentNullException.ThrowIfNull(callback);

        return _themeChangedSubscriptions.Add(callback);
    }

    public IAsyncDisposable OnHolidaysChanged(Func<HolidaysChangedArgs, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));
        ArgumentNullException.ThrowIfNull(callback);

        return _holidaysChangedSubscriptions.Add(callback);
    }

    public IAsyncDisposable OnVersionChanged(Func<VersionChangedArgs, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));
        ArgumentNullException.ThrowIfNull(callback);

        return _versionChangedSubscriptions.Add(callback);
    }

    public IAsyncDisposable OnFileDownloaded(Func<FileDownloadedArgs, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ClientManager));
        ArgumentNullException.ThrowIfNull(callback);

        return _fileDownloadedSubscriptions.Add(callback);
    }

    #endregion

    #region Internal loads

    private async Task<ClientVersion> LoadVersionAsync(CancellationToken cancellationToken = default)
    {
        ClientVersion version;

        using var httpClient = _httpClientFactory.CreateClient(nameof(ClientManager));

        try
        {
            const char separator = '+';

            var data = await httpClient.GetStringAsync(VersionFilePath, cancellationToken);

            if (data.IndexOf(separator) != data.LastIndexOf(separator) || !data.Contains(separator))
            {
                version = ClientVersion.Invalid;
            }
            else
            {
                var separatorIndex = data.IndexOf('+');

                version = ClientVersion.Valid(
                    release: data[..separatorIndex],
                    commitHash: data[(separatorIndex + 1)..]);
            }
        }
        catch
        {
            version = ClientVersion.Invalid;
        }

        return version;
    }

    private async Task<Holidays> LoadHolidaysAsync(CancellationToken cancellationToken = default)
    {
        return await TryGetItemOrSaveDefaultAsync<HolidaysStorageData, Holidays>(
            key: HolidaysStorageKey,
            onDefault: () => new HolidaysStorageData(Holidays.Empty.Dates),
            onCheck: _ => true,
            onResult: storage => new Holidays(storage.Dates),
            cancellationToken: cancellationToken);
    }

    private async Task<CultureInfo> LoadCultureAsync(CancellationToken cancellationToken = default)
    {
        return await TryGetItemOrSaveDefaultAsync<CultureStorageData, CultureInfo>(
            key: CultureStorageKey,
            onDefault: () => new CultureStorageData(SupportedCultures[0].Name),
            onCheck: storage => SupportedCultures.Any(c => c.Name == storage.Name),
            onResult: storage => new CultureInfo(storage.Name),
            cancellationToken: cancellationToken);
    }

    private async Task<DesignThemeModes> LoadThemeAsync(CancellationToken cancellationToken = default)
    {
        return await TryGetItemOrSaveDefaultAsync(
            key: ThemeStorageKey,
            onDefault: () => new ThemeStorageData(SupportedThemes[0].ToString(EnumToString)),
            onCheck: storage => Enum.TryParse<DesignThemeModes>(storage.Mode, true, out var mode) &&
                                SupportedThemes.Contains(mode),
            onResult: storage => Enum.Parse<DesignThemeModes>(storage.Mode, true),
            cancellationToken: cancellationToken);
    }

    #endregion

    #region Helpers

    private async Task<TItem> TryGetItemOrSaveDefaultAsync<TStorageData, TItem>(
        string key,
        Func<TStorageData> onDefault,
        Func<TStorageData, bool> onCheck,
        Func<TStorageData, TItem> onResult,
        CancellationToken cancellationToken = default)
        where TStorageData : StorageData
    {
        ArgumentNullException.ThrowIfNull(onCheck);
        ArgumentNullException.ThrowIfNull(onDefault);
        ArgumentNullException.ThrowIfNull(onResult);

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

        TStorageData? storage;
        Exception? exception;

        try
        {
            storage = await _jsRuntime.InvokeAsync<TStorageData?>(JsStorageGet, cancellationToken, key);
            exception = null;
        }
        catch (Exception ex)
        {
            storage = null;
            exception = ex;
        }

        if (exception == null && storage is not null && onCheck(storage))
        {
            return onResult(storage);
        }

        storage = onDefault();

        LogInvalidItemInStorage(_logger, exception, key, storage.ToString());

        return await TrySaveItemOrDefaultAsync(key, storage, onDefault, onCheck, onResult, cancellationToken);
    }


    private async Task<TItem> TrySaveItemOrDefaultAsync<TStorageData, TItem>(
        string key,
        TStorageData storage,
        Func<TStorageData> onDefault,
        Func<TStorageData, bool> onCheck,
        Func<TStorageData, TItem> onResult,
        CancellationToken cancellationToken = default)
        where TStorageData : StorageData
    {
        ArgumentNullException.ThrowIfNull(onCheck);
        ArgumentNullException.ThrowIfNull(onDefault);
        ArgumentNullException.ThrowIfNull(onResult);

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

        if (onCheck(storage))
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync(JsStorageSet, cancellationToken, key, storage);

                return onResult(storage);
            }
            catch (Exception ex)
            {
                var storageAsString = storage.ToString();

                storage = onDefault();

                var defaultStorageAsString = storage.ToString();

                LogCouldNotSaveItemToStorage(_logger, ex, key, storageAsString, defaultStorageAsString);

                await _jsRuntime.InvokeVoidAsync(JsStorageSet, cancellationToken, key, storage);

                return onResult(storage);
            }
        }
        else
        {
            var storageAsString = storage.ToString();

            storage = onDefault();

            var defaultStorageAsString = storage.ToString();

            LogCouldNotSaveItemToStorage(_logger, null, key, storageAsString, defaultStorageAsString);

            await _jsRuntime.InvokeVoidAsync(JsStorageSet, cancellationToken, key, storage);

            return onResult(storage);
        }
    }

    private static SubscriptionStore<TSubscriptionArgs> CreateSubscriptionStore<TSubscriptionArgs>(
        ILoggerFactory loggerFactory)
        where TSubscriptionArgs : ISubscriptionArgs
    {
        return new SubscriptionStore<TSubscriptionArgs>(
            loggerFactory.CreateLogger<SubscriptionStore<TSubscriptionArgs>>());
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Invalid '{key}' detected in storage, resetting to '{defaultStorageAsString}'.")]
    private static partial void LogInvalidItemInStorage(ILogger logger, Exception? ex, string key,
        string defaultStorageAsString);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Could not save '{key}' to storage, with value: '{storageAsString}', resetting to '{defaultStorageAsString}'.")]
    private static partial void LogCouldNotSaveItemToStorage(ILogger logger, Exception? ex, string key,
        string storageAsString, string defaultStorageAsString);

    #endregion
}