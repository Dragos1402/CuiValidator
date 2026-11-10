namespace CuiValidator.Api.Services;

/// <summary>
/// Offline CUI format check: optional "RO" prefix, 2-10 digits, valid control digit
/// (key 753217532).
/// </summary>
public static class CuiFormat
{
    private const string ControlKey = "753217532";

    /// <summary>Strips whitespace and an optional "RO" prefix. Returns null if nothing usable remains.</summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        string value = input.Trim().Replace(" ", string.Empty);
        if (value.StartsWith("RO", StringComparison.OrdinalIgnoreCase))
            value = value[2..];

        return value.Length == 0 ? null : value;
    }

    public static bool IsValid(string? input)
    {
        string? cui = Normalize(input);
        if (cui is null || cui.Length is < 2 or > 10 || !cui.All(char.IsAsciiDigit))
            return false;

        int control = cui[^1] - '0';
        string body = cui[..^1].PadLeft(9, '0');

        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += (body[i] - '0') * (ControlKey[i] - '0');

        int expected = sum * 10 % 11;
        if (expected == 10) expected = 0;

        return expected == control;
    }
}
