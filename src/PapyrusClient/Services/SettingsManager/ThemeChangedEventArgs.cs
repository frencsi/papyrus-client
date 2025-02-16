using Microsoft.FluentUI.AspNetCore.Components;

namespace PapyrusClient.Services.SettingsManager;

public class ThemeChangedEventArgs(DesignThemeModes newTheme) : EventArgs
{
    public DesignThemeModes NewTheme { get; } = newTheme;
}