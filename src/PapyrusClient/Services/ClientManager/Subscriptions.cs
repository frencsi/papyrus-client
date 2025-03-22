using Microsoft.FluentUI.AspNetCore.Components;

namespace PapyrusClient.Services.ClientManager;

public readonly record struct CultureChangedArgs(CultureInfo NewCulture) : ISubscriptionArgs;

public readonly record struct ThemeChangedArgs(DesignThemeModes NewTheme) : ISubscriptionArgs;

public readonly record struct HolidaysChangedArgs(Holidays NewHolidays) : ISubscriptionArgs;

public readonly record struct VersionChangedArgs(ClientVersion NewVersion) : ISubscriptionArgs;

public readonly record struct FileDownloadedArgs(string FileName, long SizeInBytes) : ISubscriptionArgs;