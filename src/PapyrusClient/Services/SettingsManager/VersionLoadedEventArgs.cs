namespace PapyrusClient.Services.SettingsManager;

public class VersionLoadedEventArgs(string loadedVersion) : EventArgs
{
    public string LoadedVersion { get; } = loadedVersion;
}