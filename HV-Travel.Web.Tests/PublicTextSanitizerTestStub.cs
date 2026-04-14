namespace HVTravel.Web.Services;

public static class PublicTextSanitizer
{
    public static string NormalizeText(string? value) => value?.Trim() ?? string.Empty;
}
