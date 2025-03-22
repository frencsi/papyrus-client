using Microsoft.FluentUI.AspNetCore.Components;

namespace PapyrusClient.Services.ClientManager;

public interface IClientManager : IAsyncDisposable
{
    IReadOnlyList<CultureInfo> SupportedCultures { get; }

    IReadOnlyList<DesignThemeModes> SupportedThemes { get; }

    CultureInfo Culture { get; }

    DesignThemeModes Theme { get; }

    Holidays Holidays { get; }

    ClientVersion Version { get; }

    Task InitializeAsync(bool reload, CancellationToken cancellationToken = default);

    Task LoadVersionAsync(bool reload, CancellationToken cancellationToken = default);

    Task ChangeCultureAsync(CultureInfo culture, CancellationToken cancellationToken = default);

    Task ChangeThemeAsync(DesignThemeModes theme, CancellationToken cancellationToken = default);

    Task ChangeHolidaysAsync(Holidays holidays, CancellationToken cancellationToken = default);

    Task DownloadFileAsync(string fileName, Stream stream, bool leaveOpen,
        CancellationToken cancellationToken = default);

    IAsyncDisposable OnCultureChanged(Func<CultureChangedArgs, Task> callback);

    IAsyncDisposable OnThemeChanged(Func<ThemeChangedArgs, Task> callback);

    IAsyncDisposable OnHolidaysChanged(Func<HolidaysChangedArgs, Task> callback);

    IAsyncDisposable OnVersionChanged(Func<VersionChangedArgs, Task> callback);

    IAsyncDisposable OnFileDownloaded(Func<FileDownloadedArgs, Task> callback);
}