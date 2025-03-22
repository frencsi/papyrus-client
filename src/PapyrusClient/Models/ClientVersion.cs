using System.Diagnostics;

namespace PapyrusClient.Models;

public enum ClientVersionState : byte
{
    Unknown = 0,
    Invalid = 1,
    Valid = 2
}

public record ClientVersion
{
    public static readonly ClientVersion Unknown = new(ClientVersionState.Unknown, string.Empty, string.Empty);

    public static readonly ClientVersion Invalid = new(ClientVersionState.Invalid, string.Empty, string.Empty);

    private ClientVersion(ClientVersionState state, string release, string commitHash)
    {
        State = state;
        Release = release;
        CommitHash = commitHash;

        if (state == ClientVersionState.Valid)
        {
            Debug.Assert(commitHash.Length > 7,
                $"Commit hash is too short, expected at least 7 characters, got {commitHash.Length}.");

            ShortCommitHash = commitHash[..7];
        }
        else
        {
            ShortCommitHash = string.Empty;
        }
    }

    public ClientVersionState State { get; }

    public string Release { get; }

    public string CommitHash { get; }

    public string ShortCommitHash { get; }

    public static ClientVersion Valid(string release, string commitHash)
    {
        return new ClientVersion(ClientVersionState.Valid, release, commitHash);
    }
}