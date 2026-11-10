namespace CuiValidator.Api.Data;

/// <summary>One stored CUI verification.</summary>
public class CuiCheck
{
    public int Id { get; set; }
    public string Cui { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CheckedAtUtc { get; set; }
}
