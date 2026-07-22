using CabinReservation.Api.Contracts;
using CabinReservation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CabinReservation.Api.Endpoints;

public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/members").WithTags("Members");

        group.MapGet("/", async (
            [FromServices] IMemberService service, 
            CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)));

        group.MapGet("/{clubNumber}", async (
            [FromRoute] string clubNumber,
            [FromServices] IMemberService service,
            CancellationToken ct) =>
        {
            var result = await service.GetAsync(clubNumber, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPut("/{clubNumber}/preference", async (
            [FromRoute] string clubNumber,
            [FromBody] UpdatePreferenceRequest request,
            HttpContext context,
            [FromServices] IMemberService service,
            CancellationToken ct) =>
        {
            var result = await service.UpdatePreferenceAsync(
                clubNumber, request, context.TraceIdentifier, ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        return app;
    }
}
