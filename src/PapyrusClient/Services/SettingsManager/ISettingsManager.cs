using System.Globalization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace PapyrusClient.Services.SettingsManager;

public interface ISettingsManager : IAsyncDisposable
{
    IReadOnlyList<CultureInfo> SupportedCultures { get; }

    IReadOnlyList<DesignThemeModes> SupportedThemes { get; }

    CultureInfo Culture { get; }

    IReadOnlySet<DateOnly> Holidays { get; }

    DesignThemeModes Theme { get; }

    public string? Version { get; }

    event EventHandler<CultureChangedEventArgs>? CultureChanged;

    event EventHandler<HolidaysChangedEventArgs>? HolidaysChanged;

    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    event EventHandler<VersionLoadedEventArgs>? VersionLoaded;

    Task LoadSettingsAsync(CancellationToken cancellationToken = default);

    Task LoadVersionAsync(CancellationToken cancellationToken = default);

    Task UpdateCultureAsync(CultureInfo culture, CancellationToken cancellationToken = default);

    Task UpdateThemeAsync(DesignThemeModes theme, CancellationToken cancellationToken = default);

    Task UpdateHolidaysAsync(IReadOnlySet<DateOnly> holidays, CancellationToken cancellationToken = default);
}