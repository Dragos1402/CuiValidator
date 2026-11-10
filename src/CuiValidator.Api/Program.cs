using CuiValidator.Api.Data;
using CuiValidator.Api.Models;
using CuiValidator.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AnafOptions>(builder.Configuration.GetSection(AnafOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=cui.db"));

builder.Services.AddHttpClient<IAnafClient, AnafClient>();
builder.Services.AddScoped<CuiValidationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

app.MapGet("/api/cui/{cui}", async (string cui, CuiValidationService service, CancellationToken ct) =>
{
    CuiCheckResponse result = await service.ValidateAsync(cui, ct);
    return Results.Json(result, statusCode: result.StatusCode);
});

app.MapGet("/api/cui/checks", async (AppDbContext db, CancellationToken ct) =>
    await db.CuiChecks
        .OrderByDescending(c => c.Id)
        .Take(20)
        .ToListAsync(ct));

app.Run();

// Exposed for WebApplicationFactory in tests.
public partial class Program { }
