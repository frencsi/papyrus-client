namespace PapyrusClient.Models;

public record Location(string Address)
{
    public static readonly Location Empty = new(string.Empty);
}

public abstract class LocationComparer
{
    public static readonly IEqualityComparer<Location>
        AddressOrdinalIgnoreCase = new AddressOrdinalIgnoreCaseComparer();

    private class AddressOrdinalIgnoreCaseComparer : IEqualityComparer<Location>
    {
        public bool Equals(Location? x, Location? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Address.Equals(y.Address, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(Location obj)
        {
            return string.GetHashCode(obj.Address, StringComparison.OrdinalIgnoreCase);
        }
    }
}