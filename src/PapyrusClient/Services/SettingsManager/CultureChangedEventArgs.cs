using System.Globalization;

namespace PapyrusClient.Services.SettingsManager;

public class CultureChangedEventArgs(CultureInfo newCulture) : EventArgs
{
    public CultureInfo NewCulture { get; } = newCulture;
}