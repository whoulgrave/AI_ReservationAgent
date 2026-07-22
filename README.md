# Cabin Reservation Azure Solution

This archive extends the original API with the remaining service projects:

- `CabinReservation.HermesMcp`: streamable HTTP MCP endpoint at `/mcp`.
- `CabinReservation.Messaging`: Azure Communication Services SMS webhook and messaging host.
- `CabinReservation.Voice`: Azure Communication Services Call Automation IVR skeleton.
- `CabinReservation.EmailIntake`: Microsoft Graph shared-mailbox worker.
- `CabinReservation.Admin`: Entra ID protected Razor Pages administration shell.
- `CabinReservation.AzureInfrastructure`: Key Vault and Blob Storage helpers.
- `CabinReservation.Integration`: typed API client shared by adapters.
- `CabinReservation.Api.Tests`: initial API smoke tests.

## Important status

The projects are substantial implementation starters, not a claim of production completion. Before deployment, complete the following:

1. Review and harden the included outbox lease/complete/fail endpoints for your chosen deployment topology.
2. Complete all voice menu branches, PIN validation, call-session persistence, retry prompts, and hang-up handling.
3. Add roster CSV parsing and API calls to the admin portal.
4. Apply an Exchange Application Access Policy or equivalent mailbox scoping so Graph application permission is restricted to the cabin mailbox.
5. Replace client secrets with managed identity or certificates where supported.
6. Add webhook validation, rate limiting, replay protection, provider event persistence, and dead-letter handling.
7. Confirm package versions with NuGet and run `dotnet restore`, `dotnet build`, and `dotnet test` in an environment with the .NET 8 SDK.
8. Use EF Core migrations rather than `EnsureCreated` for production.

## Hermes configuration

Point Hermes at:

```text
http://127.0.0.1:8081/mcp
```

Only expose the MCP service to Hermes or an authenticated private network. Do not publish it openly without authentication.

## Microsoft Graph permission

The email intake worker needs application `Mail.Read` to read the shared mailbox. Grant admin consent and scope the application to the one mailbox using Exchange controls.

## Build

```bash
dotnet restore CabinReservation.sln
dotnet build CabinReservation.sln -c Release
dotnet test CabinReservation.sln -c Release
```

No project enables TreatWarningsAsErrors.
