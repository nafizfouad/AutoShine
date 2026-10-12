using System.Net.Mail;

namespace AutoShine.Service.Common;

/// <summary>Input validation shared by services. Failures throw ArgumentException (→ HTTP 400).</summary>
public static class Validate
{
    public const int MinPasswordLength = 8;

    public static string Name(string? value, string field)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0) throw new ArgumentException($"{field} is required.");
        if (trimmed.Length > 100) throw new ArgumentException($"{field} must be at most 100 characters.");
        return trimmed;
    }

    public static string Email(string? value)
    {
        var trimmed = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (trimmed.Length == 0) throw new ArgumentException("Email is required.");
        if (trimmed.Length > 256 || !MailAddress.TryCreate(trimmed, out var parsed) || parsed.Address != trimmed)
            throw new ArgumentException("Email address is not valid.");
        return trimmed;
    }

    public static string Phone(string? value, bool required)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (required && trimmed.Length == 0) throw new ArgumentException("Phone is required.");
        if (trimmed.Length > 20) throw new ArgumentException("Phone must be at most 20 characters.");
        return trimmed;
    }

    public static void Password(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < MinPasswordLength)
            throw new ArgumentException($"Password must be at least {MinPasswordLength} characters.");
    }
}
