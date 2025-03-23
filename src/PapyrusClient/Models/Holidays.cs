using System.Collections.Frozen;

namespace PapyrusClient.Models;

public record Holidays
{
    public static readonly Holidays Empty = new(FrozenSet<DateOnly>.Empty);

    public Holidays(IEnumerable<DateOnly> dates)
    {
        Dates = dates.ToFrozenSet();
    }

    public FrozenSet<DateOnly> Dates { get; }
}