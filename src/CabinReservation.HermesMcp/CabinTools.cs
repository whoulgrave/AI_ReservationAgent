using System.ComponentModel;
using CabinReservation.Integration.Contracts.Api;
using ModelContextProtocol.Server;

namespace CabinReservation.HermesMcp;

[McpServerToolType]
public sealed class CabinTools(ICabinApiClient api)
{
    [McpServerTool, Description("Gets a club member by club-assigned identification number. Use before reservation-changing operations.")]
    public Task<MemberDto?> IdentifyMember([Description("Club member number")] string clubNumber, CancellationToken ct) => api.GetMemberAsync(clubNumber, ct);

    [McpServerTool, Description("Lists cabin availability for an inclusive date range. Never infer availability without calling this tool.")]
    public Task<IReadOnlyList<CalendarDayDto>> GetAvailability(DateOnly from, DateOnly to, CancellationToken ct) => api.GetCalendarAsync(from, to, ct);

    [McpServerTool, Description("Creates one cabin-night reservation after the member explicitly confirms the exact date.")]
    public Task<ApiResult<ReservationDto>> CreateReservation(string clubNumber, DateOnly cabinDate, string sourceChannel, string providerMessageId, CancellationToken ct) =>
        api.CreateReservationAsync(clubNumber, cabinDate, sourceChannel, providerMessageId, ct);

    [McpServerTool, Description("Cancels an existing reservation owned by the member. The API enforces the cancellation deadline.")]
    public Task<ApiResult<ReservationDto>> CancelReservation(Guid reservationId, string clubNumber, string sourceChannel, string providerMessageId, string? reason, CancellationToken ct) =>
        api.CancelReservationAsync(reservationId, clubNumber, sourceChannel, providerMessageId, reason, ct);

    [McpServerTool, Description("Lists all reservations belonging to a member.")]
    public Task<IReadOnlyList<ReservationDto>> GetMemberReservations(string clubNumber, CancellationToken ct) => api.GetReservationsAsync(clubNumber, ct);

    [McpServerTool, Description("Adds a member to the waiting list for an already-reserved date.")]
    public Task<System.Text.Json.JsonElement> JoinWaitingList(string clubNumber, DateOnly cabinDate, string sourceChannel, string providerMessageId, CancellationToken ct) =>
        api.JoinWaitListAsync(clubNumber, cabinDate, sourceChannel, providerMessageId, ct);

    [McpServerTool, Description("Accepts or declines an active waiting-list offer.")]
    public Task<System.Text.Json.JsonElement> RespondToWaitingListOffer(Guid offerId, string clubNumber, bool accept, string sourceChannel, string providerMessageId, CancellationToken ct) =>
        api.RespondToWaitListAsync(offerId, clubNumber, accept, sourceChannel, providerMessageId, ct);
}
