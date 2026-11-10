namespace CuiValidator.Api.Models;

/// <summary>Response returned to the frontend.</summary>
public sealed record CuiCheckResponse(string Message, bool IsValid, int StatusCode);
