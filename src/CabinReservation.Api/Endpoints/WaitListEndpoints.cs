using CabinReservation.Api.Contracts;
using CabinReservation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CabinReservation.Api.Endpoints;

public static class WaitListEndpoints
{
    public static IEndpointRouteBuilder MapWaitListEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/waitlist").WithTags("Waiting List");

        group.MapPost("/", async (
            [FromBody] JoinWaitListRequest request,
            HttpContext context,
            [FromServices] IWaitListService service,
            CancellationToken ct) =>
        {
            var result = await service.JoinAsync(request, context.TraceIdentifier, ct);
            return result.Success ? Results.Created($"/api/waitlist/{result.Data!.Id}", result) : Results.BadRequest(result);
        });

        group.MapPost("/{offerId:guid}/respond", async (
            [FromRoute] Guid offerId,
            [FromBody] RespondToWaitListOfferRequest request,
            HttpContext context,
            [FromServices] IWaitListService service,
            CancellationToken ct) =>
        {
            var result = await service.RespondAsync(offerId, request, context.TraceIdentifier, ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        group.MapPost("/process", async ([FromServices] IWaitListService service, CancellationToken ct) =>
        {
            await service.ProcessOffersAsync(ct);
            return Results.Accepted();
        });

        return app;
    }
}
