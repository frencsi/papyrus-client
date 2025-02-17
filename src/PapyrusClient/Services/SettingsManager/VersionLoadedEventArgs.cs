namespace PapyrusClient.Services.SettingsManager;

public class VersionLoadedEventArgs(Version loadedVersion) : EventArgs
{
    public Version LoadedVersion { get; } = loadedVersion;
}