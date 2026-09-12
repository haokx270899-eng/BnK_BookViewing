# Property Viewing Slots

## Overview

A deliberately small .NET 8 Web API for booking and finding available 30-minute property-viewing slots. It uses a service layer for business rules, EF Core Code First for persistence, and PostgreSQL as the database.

## Technology stack and architecture

- .NET 8 / ASP.NET Core Web API
- EF Core with the Npgsql PostgreSQL provider (Code First)
- Swagger/OpenAPI
- xUnit

Request flow: **Controller → ViewingService → IViewingRepository → EF Core → PostgreSQL**. Controllers stay HTTP-focused; the service is independently unit-testable and owns slot rules. The repository is intentionally small because it encapsulates the couple of database queries and the concurrency-specific persistence behavior.

## Database setup and running

Install PostgreSQL, create a database user if needed, and set the connection string. Never commit real credentials; the checked-in value is a placeholder. An environment variable overrides it:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=property_viewing;Username=postgres;Password=your-password"
dotnet restore
dotnet ef database update --project src/PropertyViewing.Infrastructure --startup-project src/PropertyViewing.Api
dotnet run --project src/PropertyViewing.Api
```

If EF CLI is missing: `dotnet tool install --global dotnet-ef --version 8.*`. The included initial migration creates the tables and seeds three properties and three users. Swagger is available at the URL printed by `dotnet run`, under `/swagger` in Development.

Run the tests with:

```powershell
dotnet test
```

## API examples

`POST /api/viewings`

```json
{ "propertyId": 1, "userId": 1, "startTime": "2026-09-15T10:30:00" }
```

Successful responses are `201 Created` and include the calculated `endTime`. A taken slot is `409`:

```json
{ "status": 409, "message": "The viewing slot is already booked." }
```

`GET /api/viewings/available?propertyId=1&from=2026-09-15&to=2026-09-17` returns available `{ startTime, endTime }` slots. Searches are limited to 31 days.

## Business rules and assumptions

- Slots are exactly 30 minutes, start at `:00` or `:30`, and run from 09:00 through 20:00. Thus 19:30 is valid while 20:00 and 10:15 are rejected.
- A property's overlapping slot is unavailable; bookings on a different property are independent.
- Property and user must exist. Invalid rules return 400, missing records return 404, and booking collisions return 409.
- Weekends are treated like weekdays. A user may book different properties at the same time because the challenge only prohibits property conflicts; this would be clarified with the product owner.
- Viewing times are wall-clock local business times stored as PostgreSQL `timestamp without time zone`; `CreatedAt` is UTC. A production version should establish a property time zone and return offset-aware values.

## Concurrency

The service checks for conflict to produce a friendly result, but that check alone cannot prevent two simultaneous requests. PostgreSQL has a unique `(PropertyId, StartTime)` index as the final guard; the repository translates its unique-violation error to a 409 response. This is safe across multiple API instances for fixed aligned slots. More flexible intervals would benefit from a PostgreSQL exclusion constraint and a transaction.

## Testing

The xUnit tests cover successful aligned bookings, conflicts, out-of-hours and misaligned input, missing property/user behavior, booked-slot filtering, and multi-day searches. They exercise business behavior with a small in-memory fake rather than EF internals. A useful next integration test would run migrations and concurrent API calls against PostgreSQL.

## Security, performance, and next steps

Production should add OAuth2/OIDC authentication, authorization, HTTPS enforcement, rate limits, secrets management, and derive `UserId` from claims rather than the body. Database filtering and indexes keep normal searches bounded; for higher scale, add monitoring/tracing, query analysis, stricter range policies/pagination, caching only after measurement, and horizontal API scaling.

With more time: clarify time zones and user-conflict policy, strengthen interval concurrency, add PostgreSQL integration tests and health checks, improve observability, and refine production migration/deployment strategy.

## AI usage

AI tools were used to clarify requirements, identify edge cases, review the architecture, and assist development. The implementation decisions were reviewed and are intended to be explainable in a technical interview.
