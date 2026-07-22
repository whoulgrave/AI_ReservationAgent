# Cabin Reservation API

A .NET 8 authoritative reservation API intended to sit behind Hermes, Azure Communication Services, email processors, and an administrative UI.

## Included

- Active-member validation
- One confirmed reservation per cabin date
- Maximum active-night limit
- Cancellation deadline enforcement
- Waiting-list queue and expiring offers
- Communication preferences
- Transactional reservation changes
- Idempotency records
- Outbound-message outbox
- Roster preview/application
- Automatic cancellation when members leave the roster
- Append-only business audit events
- SQLite persistence
- Swagger UI
- API-key middleware
- Docker support

## Important security note

The API-key middleware is an initial service-to-service control, not the final administrator/member authentication design. In Azure production, place the API behind HTTPS and replace or augment the API key with Microsoft Entra ID, managed identities, role checks, and restricted network access.

## Prerequisites

- .NET 8 SDK
- Visual Studio 2022/2026, Visual Studio Code, or Rider
- Docker is optional

## Run locally

```bash
dotnet restore
dotnet run
```

Use the URL printed by ASP.NET Core and open `/swagger`.

Development API key:

```text
development-only-key
```

Header:

```text
X-Api-Key: development-only-key
```

## Run with Docker

Create a `.env` file:

```text
CABIN_API_KEY=replace-with-a-long-random-secret
```

Then:

```bash
docker compose up --build
```

The container listens on localhost port 8080.

## Database

The project uses `Database.EnsureCreated()` to simplify the first runnable version. Before long-term production use, replace this with checked-in EF Core migrations:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

The initializer also creates a SQLite partial unique index so that only one `Confirmed` reservation can exist for a cabin date.

## Seed member

On first run, the application creates:

```text
Club number: 001
Name: Initial Administrator
Email: admin@example.org
```

Replace this through the roster API.

## Typical Hermes integration

Hermes should call only narrowly defined MCP tools that wrap these endpoints. It should never receive direct database access.

Suggested mapping:

| MCP tool | API operation |
|---|---|
| `check_cabin_availability` | `GET /api/calendar` |
| `create_cabin_reservation` | `POST /api/reservations` |
| `cancel_cabin_reservation` | `DELETE /api/reservations/{id}` |
| `join_waiting_list` | `POST /api/waitlist` |
| `respond_to_waiting_list_offer` | `POST /api/waitlist/{id}/respond` |
| `set_confirmation_preference` | `PUT /api/members/{clubNumber}/preference` |

## Production work still required

- Microsoft Entra ID authentication and role authorization
- Azure Communication Services adapters
- Exchange Online/inbound-email adapter
- Outbound-message dispatcher
- Roster CSV parser and Blob Storage retention
- PIN or one-time-code member verification
- Rate limiting
- EF Core migrations
- Automated test project
- Azure Key Vault configuration provider
- Application Insights/OpenTelemetry
- Database backup and restore procedure
- Admin override workflows
- Blackout dates
