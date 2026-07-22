using CabinReservation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CabinReservation.Api.Endpoints;
public static class CalendarEndpoints
{
    public static IEndpointRouteBuilder MapCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/calendar", async (
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to,
            [FromServices] IReportService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.GetCalendarAsync(from, to, ct));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).WithTags("Calendar");

        return app;
    }
}
