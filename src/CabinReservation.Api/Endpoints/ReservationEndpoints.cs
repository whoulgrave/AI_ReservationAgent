using CabinReservation.Api.Contracts;
using CabinReservation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CabinReservation.Api.Endpoints;

public static class ReservationEndpoints
{
    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reservations").WithTags("Reservations");

        group.MapPost("/", async (
            [FromBody] CreateReservationRequest request,
            HttpContext context,
            [FromServices] IReservationService service,
            CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, context.TraceIdentifier, ct);
            return result.Success
                ? Results.Created($"/api/reservations/{result.Data!.Id}", result)
                : result.Code == "DATE_ALREADY_RESERVED"
                    ? Results.Conflict(result)
                    : Results.BadRequest(result);
        });

        group.MapDelete("/{reservationId:guid}", async (
            [FromRoute] Guid reservationId,
            [FromBody] CancelReservationRequest request,
            HttpContext context,
            [FromServices] IReservationService service,
            CancellationToken ct) =>
        {
            var result = await service.CancelAsync(
                reservationId, request, context.TraceIdentifier, ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        group.MapGet("/member/{clubNumber}", async (
            [FromRoute] string clubNumber,
            [FromServices] IReservationService service,
            CancellationToken ct) =>
            Results.Ok(await service.GetMemberReservationsAsync(clubNumber, ct)));

        return app;
    }
}
