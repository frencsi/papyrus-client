namespace PapyrusClient.Models;

public record Company(string Name)
{
    public static readonly Company Empty = new(string.Empty);
}

public abstract class CompanyComparer
{
    public static readonly IEqualityComparer<Company> NameOrdinalIgnoreCase = new NameOrdinalIgnoreCaseComparer();

    private class NameOrdinalIgnoreCaseComparer : IEqualityComparer<Company>
    {
        public bool Equals(Company? x, Company? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Name.Equals(y.Name, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(Company obj)
        {
            return string.GetHashCode(obj.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}