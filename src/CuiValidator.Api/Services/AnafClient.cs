using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CuiValidator.Api.Services;

public enum AnafLookupResult
{
    Found,
    NotFound,
    Unavailable
}

public interface IAnafClient
{
    Task<AnafLookupResult> LookupAsync(string cui, CancellationToken ct);
}

public sealed class AnafOptions
{
    public const string SectionName = "Anaf";

    public string BaseUrl { get; set; } = "https://webservicesp.anaf.ro/api/PlatitorTvaRest/v9/tva";
    public int TimeoutSeconds { get; set; } = 10;
    public int MaxRetries { get; set; } = 2;
    public int RetryDelayMs { get; set; } = 500;
}

/// <summary>Calls the public ANAF VAT-payer service and retries on transient failures.</summary>
public sealed class AnafClient : IAnafClient
{
    private readonly HttpClient _http;
    private readonly AnafOptions _options;
    private readonly ILogger<AnafClient> _logger;

    public AnafClient(HttpClient http, IOptions<AnafOptions> options, ILogger<AnafClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<AnafLookupResult> LookupAsync(string cui, CancellationToken ct)
    {
        var payload = new[]
        {
            new { cui = long.Parse(cui), data = DateTime.UtcNow.ToString("yyyy-MM-dd") }
        };

        for (int attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            if (attempt > 0 && _options.RetryDelayMs > 0)
                await Task.Delay(_options.RetryDelayMs * attempt, ct);

            try
            {
                using HttpResponseMessage response = await _http.PostAsJsonAsync(_options.BaseUrl, payload, ct);

                if ((int)response.StatusCode >= 500 || (int)response.StatusCode == 429)
                {
                    _logger.LogWarning("ANAF returned {Status} (attempt {Attempt})", (int)response.StatusCode, attempt + 1);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ANAF returned {Status}, not retrying", (int)response.StatusCode);
                    return AnafLookupResult.Unavailable;
                }

                using JsonDocument doc = await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

                return doc.RootElement.TryGetProperty("found", out JsonElement found)
                       && found.ValueKind == JsonValueKind.Array
                       && found.GetArrayLength() > 0
                    ? AnafLookupResult.Found
                    : AnafLookupResult.NotFound;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "ANAF call failed (attempt {Attempt})", attempt + 1);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "ANAF returned an unreadable response");
                return AnafLookupResult.Unavailable;
            }
        }

        return AnafLookupResult.Unavailable;
    }
}
