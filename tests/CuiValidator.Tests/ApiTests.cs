using System.Net;
using System.Net.Http.Json;
using CuiValidator.Api.Models;
using CuiValidator.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CuiValidator.Tests;

/// <summary>End-to-end tests with a fake ANAF client, so no network is needed.</summary>
public class ApiTests
{
    private sealed class FakeAnaf(AnafLookupResult result) : IAnafClient
    {
        public int Calls { get; private set; }

        public Task<AnafLookupResult> LookupAsync(string cui, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(result);
        }
    }

    private static (HttpClient client, FakeAnaf anaf) CreateClient(AnafLookupResult anafResult)
    {
        var anaf = new FakeAnaf(anafResult);
        string dbPath = Path.Combine(Path.GetTempPath(), $"cui-test-{Guid.NewGuid():N}.db");

        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:Default", $"Data Source={dbPath}");
            b.ConfigureServices(s =>
            {
                s.RemoveAll<IAnafClient>();
                s.AddSingleton<IAnafClient>(anaf);
            });
        });

        return (factory.CreateClient(), anaf);
    }

    [Fact]
    public async Task Found_at_anaf_returns_200_valid()
    {
        var (client, _) = CreateClient(AnafLookupResult.Found);

        var response = await client.GetAsync("/api/cui/RO14080700");
        var body = await response.Content.ReadFromJsonAsync<CuiCheckResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new CuiCheckResponse("CUI-ul este valid", true, 200), body);
    }

    [Fact]
    public async Task Not_found_at_anaf_returns_404()
    {
        var (client, _) = CreateClient(AnafLookupResult.NotFound);

        var response = await client.GetAsync("/api/cui/14080700");
        var body = await response.Content.ReadFromJsonAsync<CuiCheckResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(body!.IsValid);
        Assert.Equal(404, body.StatusCode);
    }

    [Fact]
    public async Task Anaf_down_returns_503()
    {
        var (client, _) = CreateClient(AnafLookupResult.Unavailable);

        var response = await client.GetAsync("/api/cui/14080700");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Bad_format_returns_400_without_calling_anaf()
    {
        var (client, anaf) = CreateClient(AnafLookupResult.Found);

        var response = await client.GetAsync("/api/cui/14080701");
        var body = await response.Content.ReadFromJsonAsync<CuiCheckResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(body!.IsValid);
        Assert.Equal(0, anaf.Calls);
    }

    [Fact]
    public async Task Checks_are_saved_to_sqlite()
    {
        var (client, _) = CreateClient(AnafLookupResult.Found);

        await client.GetAsync("/api/cui/14080700");
        var checks = await client.GetFromJsonAsync<List<Dictionary<string, object>>>("/api/cui/checks");

        Assert.Single(checks!);
    }
}
