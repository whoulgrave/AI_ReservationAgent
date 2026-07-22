using CabinReservation.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CabinReservation.Api.Endpoints;

public static class RosterEndpoints
{
    public static IEndpointRouteBuilder MapRosterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roster").WithTags("Roster");

        group.MapPost("/preview", async (
            [FromBody] ApplyRosterRequest request,
            [FromServices] IRosterService service,
            CancellationToken ct) =>
        {
            var result = await service.PreviewAsync(request, ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        group.MapPost("/apply", async (
            [FromBody] ApplyRosterRequest request,
            HttpContext context,
            [FromServices] IRosterService service,
            CancellationToken ct) =>
        {
            var result = await service.ApplyAsync(request, context.TraceIdentifier, ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        return app;
    }
}
