namespace HVTravel.Application.Services;

internal static class MeilisearchQueryHelpers
{
    public static string Quote(string value)
    {
        return $"\"{value.Trim().Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }

    public static string JoinAnd(IEnumerable<string> filters)
    {
        return string.Join(" AND ", filters.Where(static item => !string.IsNullOrWhiteSpace(item)));
    }

    public static string JoinOr(IEnumerable<string> filters)
    {
        return string.Join(" OR ", filters.Where(static item => !string.IsNullOrWhiteSpace(item)));
    }

    public static string NormalizePhone(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());
    }
}
