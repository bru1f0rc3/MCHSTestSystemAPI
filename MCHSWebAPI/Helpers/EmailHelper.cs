namespace MCHSWebAPI.Helpers;

public static class EmailHelper
{
    public static string MaskEmail(string email)
    {
        var trimmed = email.Trim();
        var at = trimmed.IndexOf('@');
        if (at <= 1 || at == trimmed.Length - 1) return trimmed;
        var local = trimmed[..at];
        var domain = trimmed[(at + 1)..];
        if (local.Length <= 4) return $"{local[0]}***@{domain}";
        var prefix = local[..4];
        var suffix = local[^2..];
        return $"{prefix}{new string('*', Math.Max(3, local.Length - 6))}{suffix}@{domain}";
    }
}
