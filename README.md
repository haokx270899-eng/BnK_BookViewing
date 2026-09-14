# Property Viewing Management

## Overview & Executive Summary

Property Viewing Management is a multi-timezone property-viewing booking system built with **.NET 8**, ASP.NET Core Web API, Entity Framework Core, and PostgreSQL. A small React/Vite client is included for booking and availability workflows.

The system applies property-local business rules consistently across time zones:

- Viewings are fixed **30-minute** intervals.
- Valid starts are on `:00` or `:30` between **09:00 (inclusive)** and **20:00 (exclusive)** in the property's local time zone.
- Availability is isolated per property; an overlapping viewing makes that property interval unavailable.
- Availability searches are limited to 31 days.

The API keeps HTTP concerns in controllers, business rules in the application service, and EF Core/PostgreSQL access in infrastructure. This separation keeps scheduling logic independently unit-testable and preserves a clear path for production hardening.

## Architecture & Technical Design Decisions

### Layered design

```text
HTTP request
    │
    ▼
API Controllers ──► Application / ViewingService ──► Repository interface
                                                        │
                                                        ▼
                                              EF Core / PostgreSQL
```

- **Domain** contains the `Property`, `User`, and `Viewing` entities.
- **Application** owns validation, slot generation, timezone conversion, and business exceptions behind interfaces.
- **Infrastructure** implements persistence, entity configuration, migrations, and database-level conflict translation.
- **API** exposes controller endpoints, dependency composition, exception-to-HTTP mapping, Swagger, and development CORS.

### Timezone and DST awareness

Properties store an IANA timezone ID (for example, `Europe/London`, `America/New_York`, or `Asia/Ho_Chi_Minh`). Booking input is deliberately treated as a **local wall-clock value**: `ViewingService` applies `DateTime.SpecifyKind(..., DateTimeKind.Unspecified)` before validating business hours and alignment. This prevents the caller or host machine's local timezone from silently changing the intended property-local time.

After local validation, `ConvertToUtcSafe` converts slot boundaries to UTC. Bookings are persisted as PostgreSQL `timestamp with time zone` values, and conflict/availability queries operate strictly on UTC timestamps. The availability DTO returns both local and UTC boundaries:

```json
{
  "localStartTime": "2026-09-15T10:30:00",
  "localEndTime": "2026-09-15T11:00:00",
  "utcStartTime": "2026-09-15T09:30:00Z",
  "utcEndTime": "2026-09-15T10:00:00Z"
}
```

### DST edge cases

`ConvertToUtcSafe` and availability generation explicitly account for daylight-saving transitions:

- **Spring Forward:** `TimeZoneInfo.IsInvalidTime` identifies non-existent local wall-clock values. The availability generator skips those slots. For conversion, the active timezone adjustment rule is resolved for the target date and its dynamic `DaylightDelta` is applied (with a one-hour fallback) before conversion.
- **Fall Back:** repeated local hours can map to different UTC instants. Returning paired Local/UTC fields allows API consumers to distinguish the otherwise identical local times. UTC persistence also makes collision detection deterministic.

### Database query range expansion

`GetAvailableAsync` expands a requested date range to complete **local-day** boundaries—`00:00` on `from` through `00:00` on `to + 1`—before converting those bounds to UTC. This is important because a local calendar day is not always 24 hours at timezone/DST boundaries. Querying with the expanded UTC range prevents bookings near a shift from being omitted before local availability is calculated.

### Conflict checking and performance

The service materializes booked UTC intervals into a `HashSet<(StartTime, EndTime)>`, giving O(1) expected lookup for an exact candidate interval and avoiding repeated database calls while slots are generated. It uses the standard interval predicate `booked.Start < candidate.End && booked.End > candidate.Start` for overlap semantics. Because the current overlap predicate enumerates the set, its general interval-overlap check remains O(n); for the fixed 30-minute grid, it can be reduced to O(1) by testing `HashSet.Contains((slotStartUtc, slotEndUtc))` once interval flexibility is no longer required.

The repository also checks conflicts in the database and PostgreSQL enforces a unique `(PropertyId, StartTime)` index. The unique-violation path is translated to a domain booking-conflict error and surfaced as HTTP `409 Conflict`.

## Project Structure

```text
.
├── PropertyViewing.sln
├── Directory.Build.props                 # net8.0, nullable, warnings as errors
├── src
│   ├── PropertyViewing.Domain
│   │   └── Entities
│   │       ├── Property.cs
│   │       ├── User.cs
│   │       └── Viewing.cs
│   ├── PropertyViewing.Application
│   │   ├── DTOs/ViewingDtos.cs
│   │   ├── Exceptions/DomainExceptions.cs
│   │   ├── Interfaces
│   │   │   ├── IViewingRepository.cs
│   │   │   └── IViewingService.cs
│   │   └── Services/ViewingService.cs
│   ├── PropertyViewing.Infrastructure
│   │   ├── Migrations
│   │   ├── Persistence
│   │   │   ├── AppDbContext.cs
│   │   │   └── Configurations/EntityConfigurations.cs
│   │   └── Repositories/ViewingRepository.cs
│   ├── PropertyViewing.Api
│   │   ├── Controllers
│   │   │   ├── ViewingsController.cs
│   │   │   ├── AdminViewingsController.cs
│   │   │   └── LookupsController.cs
│   │   ├── DTOs
│   │   ├── appsettings.json
│   │   └── Program.cs
│   └── PropertyViewing.Web                # React + TypeScript + Vite client
│       └── src
│           ├── api
│           ├── components
│           ├── hooks
│           └── pages
└── tests
    └── PropertyViewing.UnitTests
        └── ViewingServiceTests.cs
```

## Prerequisites and Local Development

Required:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL (for API/database execution)
- Node.js 20+ and npm (optional, for the web client)

Build and run the unit tests from the repository root:

```powershell
dotnet restore
dotnet build
dotnet test
```

To run the API, configure `ConnectionStrings__DefaultConnection` with local PostgreSQL credentials, apply the included migrations, and start the API:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=property_viewing;Username=postgres;Password=12345678"
dotnet ef database update --project src/PropertyViewing.Infrastructure --startup-project src/PropertyViewing.Api
dotnet run --project src/PropertyViewing.Api
```

In Development, Swagger UI is served at the API root. To start the web client in a separate shell:

```powershell
Set-Location src/PropertyViewing.Web
npm install
npm run dev
```

The Vite development server uses `http://localhost:5173`; configure `VITE_API_URL` if the API uses a different address.

## API Surface

| Endpoint | Purpose |
| --- | --- |
| `POST /api/viewings` | Creates a property-local, 30-minute viewing request. |
| `GET /api/viewings/available?propertyId={id}&from={date}&to={date}` | Returns available slots with local and UTC boundaries. |
| `GET /api/properties` | Returns property lookup data, including timezone context. |
| `GET /api/users` | Returns user lookup data. |
| `GET /api/admin/viewings` | Returns administrative viewing data. |

Invalid business input returns `400`, unknown properties/users return `404`, and collisions return `409`.

## AI Collaboration & Disclosure

AI tools were used as a development aid for code refactoring, review of DST edge cases, and generation of documentation and test material. The resulting implementation and this document should be reviewed as normal engineering artifacts; timezone behavior, data contracts, and production policies remain explicit human-owned decisions.

## Production Considerations & Next Steps

- **Concurrency control:** the unique index protects fixed aligned starts, but high-contention or variable-duration booking flows should use a transaction with pessimistic locking, an appropriate PostgreSQL exclusion constraint, or a distributed lock to prevent double-booking across API instances.
- **Caching:** cache bounded availability responses in Redis, keyed by property, local date range, and timezone; invalidate or version entries whenever a booking changes the property's schedule.
- **Observability:** add structured logging with correlation IDs, metrics for search/booking latency and conflicts, distributed tracing, health checks, and alerting for database/timezone conversion failures.
- **Security and operations:** introduce OIDC/OAuth2 authentication, derive user identity from claims rather than request bodies, use secret storage, enforce production CORS/HTTPS policy, and add rate limits.
- **Verification:** add PostgreSQL integration tests for concurrent requests and DST transitions, especially invalid times and repeated local hours in DST-observing property zones.
