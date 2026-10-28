namespace Application.Common.Validation;

/// <summary>Rejects empty or wildcard values where the stored procedure requires equality.</summary>
public static class ConcreteFilter
{
    private static readonly string[] Wildcards = ["Todos", "%", "_", "*"];

    public static string Required(string message, string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || Wildcards.Any(wildcard =>
                string.Equals(trimmed, wildcard, StringComparison.OrdinalIgnoreCase)
                || trimmed.Contains('%', StringComparison.Ordinal)
                || trimmed.Contains('_', StringComparison.Ordinal)))
            throw new ArgumentException(message);
        return trimmed;
    }
}
