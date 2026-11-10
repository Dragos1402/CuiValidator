# CuiValidator

Small .NET 8 Web API that checks a Romanian CUI (fiscal code) against the public ANAF service
and stores each check in SQLite via EF Core.

## Run

```bash
dotnet run --project src/CuiValidator.Api
curl http://localhost:5000/api/cui/RO14080700
```

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/cui/{cui}` | Validates the CUI (with or without `RO` prefix) |
| GET | `/api/cui/checks` | Last 20 stored checks |

Response body:

```json
{ "message": "CUI-ul este valid", "isValid": true, "statusCode": 200 }
```

| Case | statusCode | isValid |
|------|-----------|---------|
| Found at ANAF | 200 | true |
| Bad format / wrong control digit (ANAF not called) | 400 | false |
| Valid format, not found at ANAF | 404 | false |
| ANAF unreachable after retries | 503 | false |

## How it works

1. `CuiFormat` checks the format offline: 2-10 digits, control digit with key `753217532`.
2. `AnafClient` POSTs to `Anaf:BaseUrl` (default ANAF `PlatitorTvaRest/v9/tva`), retrying on 5xx/429/timeouts
   (`MaxRetries`, `RetryDelayMs` in `appsettings.json`).
3. `CuiValidationService` maps the result to the response and saves it in `cui.db` (created on startup with `EnsureCreated`).

## Constraints

`src/CuiValidator.Api/Services/CuiFormat.cs` is frozen. The control-digit algorithm was validated
by our accounting team. Do not modify this file. If a change seems necessary, raise it instead.

## Tests

```bash
dotnet test
```

Tests run fully offline: the ANAF client is tested against a stub `HttpMessageHandler`,
and the API tests replace `IAnafClient` with a fake.

## License

MIT
