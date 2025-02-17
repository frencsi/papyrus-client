namespace PapyrusClient.Services.SettingsManager;

public enum VersionState : byte
{
    Invalid = 0,
    Valid = 1
}

public class Version
{
    private static readonly Version InvalidVersion = new(
        VersionState.Invalid,
        ReadOnlyMemory<char>.Empty,
        ReadOnlyMemory<char>.Empty,
        ReadOnlyMemory<char>.Empty);

    private Version(
        VersionState state,
        ReadOnlyMemory<char> value,
        ReadOnlyMemory<char> commitHash,
        ReadOnlyMemory<char> shortCommitHash)
    {
        State = state;
        Value = value;
        CommitHash = commitHash;
        ShortCommitHash = shortCommitHash;
    }

    public VersionState State { get; }

    public ReadOnlyMemory<char> Value { get; }

    public ReadOnlyMemory<char> CommitHash { get; }

    public ReadOnlyMemory<char> ShortCommitHash { get; }

    public static Version Valid(ReadOnlyMemory<char> value, ReadOnlyMemory<char> commitHash,
        ReadOnlyMemory<char> shortCommitHash)
    {
        return new Version(VersionState.Valid, value, commitHash, shortCommitHash);
    }

    public static Version Invalid()
    {
        return InvalidVersion;
    }
}