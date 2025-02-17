using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PapyrusClient.Services.SettingsManager;

public partial class SettingsManager(
    IJSRuntime jsRuntime,
    ILogger<SettingsManager> logger) : ISettingsManager
{
    private const string
        CultureStorageKey = "Culture",
        HolidaysStorageKey = "Holidays",
        ThemeStorageKey = "Theme",
        VersionFilePath = "wwwroot/version.txt";

    private static readonly IReadOnlyList<CultureInfo> Cultures =
    [
        new("en-US"),
        new("hu-HU")
    ];

    private static readonly IReadOnlyList<DesignThemeModes> Themes =
    [
        DesignThemeModes.System,
        DesignThemeModes.Light,
        DesignThemeModes.Dark
    ];

    private volatile bool _disposed;

    public IReadOnlyList<CultureInfo> SupportedCultures => Cultures;

    public IReadOnlyList<DesignThemeModes> SupportedThemes => Themes;

    public CultureInfo Culture { get; private set; } = Cultures[0];

    public DesignThemeModes Theme { get; private set; } = Themes[0];

    public IReadOnlySet<DateOnly> Holidays { get; private set; } = ReadOnlySet<DateOnly>.Empty;

    public string Version { get; private set; } = "Unknown";

    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    public event EventHandler<HolidaysChangedEventArgs>? HolidaysChanged;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    protected virtual ValueTask DisposeAsyncCore()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        CultureChanged = null;
        HolidaysChanged = null;
        ThemeChanged = null;

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    public async Task LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SettingsManager));

        var culture = await LoadCultureAsync(cancellationToken);

        if (culture == null || Cultures.All(x => x.Name != culture.Name))
        {
            LogInvalidCultureInStorage(logger, culture?.Name ?? string.Empty, Cultures[0].Name);

            culture = Cultures[0];
            await UpdateCultureAsync(culture, false, cancellationToken);
        }

        var holidays = await LoadHolidaysAsync(cancellationToken);

        if (holidays == null)
        {
            LogInvalidHolidaysInStorage(logger);

            holidays = ReadOnlySet<DateOnly>.Empty;
            await UpdateHolidaysAsync(holidays, false, cancellationToken);
        }

        var theme = await LoadThemeAsync(cancellationToken);

        if (theme == null)
        {
            LogInvalidThemeInStorage(logger);

            theme = Themes[0];
            await UpdateThemeAsync(theme.Value, false, cancellationToken);
        }

        string? version = null;

#if DEBUG
        version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
#else
        Console.WriteLine($"Loaded version: {version}");
        version = await File.ReadAllTextAsync(VersionFilePath, cancellationToken);
#endif

        Culture = culture;
        Holidays = holidays;
        Theme = theme.Value;

        if (!string.IsNullOrWhiteSpace(version))
        {
            Version = version;
        }

        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;

        CultureChanged?.Invoke(this, new CultureChangedEventArgs(Culture));
        HolidaysChanged?.Invoke(this, new HolidaysChangedEventArgs(Holidays));
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(Theme));
    }

    public async Task UpdateCultureAsync(CultureInfo culture, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SettingsManager));

        if (SupportedCultures.All(x => x.Name != culture.Name))
        {
            LogCouldNotSaveInvalidCulture(logger, culture.Name);
            return;
        }

        await UpdateCultureAsync(culture, true, cancellationToken);
    }

    public async Task UpdateThemeAsync(DesignThemeModes theme, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SettingsManager));

        if (SupportedThemes.All(x => x != theme))
        {
            LogCouldNotSaveInvalidTheme(logger, theme.ToString("G"));
            return;
        }

        await UpdateThemeAsync(theme, true, cancellationToken);
    }

    public async Task UpdateHolidaysAsync(IReadOnlySet<DateOnly> holidays,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(SettingsManager));

        await UpdateHolidaysAsync(holidays, true, cancellationToken);
    }

    private async Task UpdateCultureAsync(CultureInfo culture, bool notifyChange,
        CancellationToken cancellationToken = default)
    {
        var data = new CultureLocalStorageData
        {
            Name = culture.Name
        };

        await jsRuntime.InvokeVoidAsync("settingsManager.set", cancellationToken, CultureStorageKey, data);

        Culture = culture;

        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;

        if (notifyChange)
        {
            CultureChanged?.Invoke(this, new CultureChangedEventArgs(Culture));
        }
    }

    private async Task UpdateHolidaysAsync(IReadOnlySet<DateOnly> holidays, bool notifyChange,
        CancellationToken cancellationToken = default)
    {
        await jsRuntime.InvokeVoidAsync("settingsManager.set", cancellationToken, HolidaysStorageKey,
            holidays.AsEnumerable());

        Holidays = holidays;

        if (notifyChange)
        {
            HolidaysChanged?.Invoke(this, new HolidaysChangedEventArgs(Holidays));
        }
    }

    private async Task UpdateThemeAsync(DesignThemeModes theme, bool notifyChange,
        CancellationToken cancellationToken = default)
    {
        var data = new ThemeLocalStorageData
        {
            Mode = theme.ToString("G")
        };

        await jsRuntime.InvokeVoidAsync("settingsManager.set", cancellationToken, ThemeStorageKey, data);

        Theme = theme;

        if (notifyChange)
        {
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(Theme));
        }
    }

    private async Task<CultureInfo?> LoadCultureAsync(CancellationToken cancellationToken = default)
    {
        CultureLocalStorageData? data;

        try
        {
            data = await jsRuntime.InvokeAsync<CultureLocalStorageData?>("settingsManager.get", cancellationToken,
                CultureStorageKey);
        }
        catch (Exception)
        {
            data = null;
        }

        CultureInfo? culture;

        try
        {
            culture = data == null ? null : new CultureInfo(data.Name);
        }
        catch (Exception)
        {
            culture = null;
        }

        return culture;
    }

    private async Task<IReadOnlySet<DateOnly>?> LoadHolidaysAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<DateOnly>? data;

        try
        {
            data = await jsRuntime.InvokeAsync<IEnumerable<DateOnly>?>("settingsManager.get", cancellationToken,
                HolidaysStorageKey);
        }
        catch (Exception)
        {
            data = null;
        }

        return data?.ToFrozenSet();
    }

    private async Task<DesignThemeModes?> LoadThemeAsync(CancellationToken cancellationToken = default)
    {
        ThemeLocalStorageData? data = null;

        try
        {
            data = await jsRuntime.InvokeAsync<ThemeLocalStorageData?>("settingsManager.get", cancellationToken,
                ThemeStorageKey);
        }
        catch (Exception)
        {
            data = null;
        }

        if (data == null || !Enum.TryParse<DesignThemeModes>(data.Mode, out var theme))
        {
            return null;
        }

        return theme;
    }

    private record ThemeLocalStorageData
    {
        public required string Mode { get; init; }
    }

    private record CultureLocalStorageData
    {
        public required string Name { get; init; }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not save invalid culture: {IntendedCultureName}.")]
    private static partial void LogCouldNotSaveInvalidCulture(ILogger logger, string intendedCultureName);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not save invalid theme: {IntendedTheme}.")]
    private static partial void LogCouldNotSaveInvalidTheme(ILogger logger, string intendedTheme);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Invalid culture detected in storage: {StorageCultureName}. Defaulting to: {DefaultCultureName}")]
    private static partial void LogInvalidCultureInStorage(ILogger logger, string storageCultureName,
        string defaultCultureName);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Invalid holidays detected in storage, resetting to empty holidays.")]
    private static partial void LogInvalidHolidaysInStorage(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Invalid theme detected in storage, resetting to system.")]
    private static partial void LogInvalidThemeInStorage(ILogger logger);
}