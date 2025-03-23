namespace PapyrusClient.Models;

public record Employee(string Name)
{
    public static readonly Employee Empty = new(string.Empty);
}

public abstract class EmployeeComparer
{
    public static readonly IEqualityComparer<Employee> NameOrdinalIgnoreCase = new NameOrdinalIgnoreCaseComparer();

    private class NameOrdinalIgnoreCaseComparer : IEqualityComparer<Employee>
    {
        public bool Equals(Employee? x, Employee? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Name.Equals(y.Name, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(Employee obj)
        {
            return string.GetHashCode(obj.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}