using System.Net;
using System.Text;
using CuiValidator.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CuiValidator.Tests;

/// <summary>Tests the HTTP client against canned responses (no real ANAF call).</summary>
public class AnafClientTests
{
    private sealed class StubHandler(params Func<HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var next = responses[Math.Min(Calls, responses.Length - 1)];
            Calls++;
            return Task.FromResult(next());
        }
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static AnafClient Create(StubHandler handler) =>
        new(new HttpClient(handler),
            Options.Create(new AnafOptions { BaseUrl = "http://anaf.test/tva", MaxRetries = 2, RetryDelayMs = 0 }),
            NullLogger<AnafClient>.Instance);

    [Fact]
    public async Task Found_array_with_item_means_found()
    {
        var handler = new StubHandler(() => Json("""{"cod":200,"found":[{"date_generale":{"cui":14080700}}],"notFound":[]}"""));

        Assert.Equal(AnafLookupResult.Found, await Create(handler).LookupAsync("14080700", default));
    }

    [Fact]
    public async Task Empty_found_means_not_found()
    {
        var handler = new StubHandler(() => Json("""{"cod":200,"found":[],"notFound":[14080700]}"""));

        Assert.Equal(AnafLookupResult.NotFound, await Create(handler).LookupAsync("14080700", default));
    }

    [Fact]
    public async Task Server_error_is_retried_then_succeeds()
    {
        var handler = new StubHandler(
            () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            () => Json("""{"found":[{}]}"""));

        Assert.Equal(AnafLookupResult.Found, await Create(handler).LookupAsync("14080700", default));
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task Persistent_server_error_gives_unavailable_after_retries()
    {
        var handler = new StubHandler(() => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Assert.Equal(AnafLookupResult.Unavailable, await Create(handler).LookupAsync("14080700", default));
        Assert.Equal(3, handler.Calls); // 1 attempt + 2 retries
    }
}
