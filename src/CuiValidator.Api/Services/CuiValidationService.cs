using CuiValidator.Api.Data;
using CuiValidator.Api.Models;

namespace CuiValidator.Api.Services;

public sealed class CuiValidationService(IAnafClient anaf, AppDbContext db)
{
    public const string MsgValid = "CUI-ul este valid";
    public const string MsgInvalidFormat = "CUI-ul nu are un format valid";
    public const string MsgNotFound = "CUI-ul nu a fost găsit la ANAF";
    public const string MsgUnavailable = "Serviciul ANAF nu este disponibil momentan";

    public async Task<CuiCheckResponse> ValidateAsync(string input, CancellationToken ct)
    {
        string? cui = CuiFormat.Normalize(input);

        CuiCheckResponse response;
        if (cui is null || !CuiFormat.IsValid(cui))
        {
            response = new CuiCheckResponse(MsgInvalidFormat, false, StatusCodes.Status400BadRequest);
        }
        else
        {
            response = await anaf.LookupAsync(cui, ct) switch
            {
                AnafLookupResult.Found => new CuiCheckResponse(MsgValid, true, StatusCodes.Status200OK),
                AnafLookupResult.NotFound => new CuiCheckResponse(MsgNotFound, false, StatusCodes.Status404NotFound),
                _ => new CuiCheckResponse(MsgUnavailable, false, StatusCodes.Status503ServiceUnavailable)
            };
        }

        db.CuiChecks.Add(new CuiCheck
        {
            Cui = cui ?? input,
            IsValid = response.IsValid,
            StatusCode = response.StatusCode,
            Message = response.Message,
            CheckedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);

        return response;
    }
}
