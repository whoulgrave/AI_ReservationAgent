using CabinReservation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CabinReservation.Api.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audit", async (
            [FromQuery] DateTimeOffset fromUtc,
            [FromQuery] DateTimeOffset toUtc,
            [FromServices] IReportService service,
            CancellationToken ct) =>
            Results.Ok(await service.GetAuditAsync(fromUtc, toUtc, ct)))
            .WithTags("Audit");

        return app;
    }
}
