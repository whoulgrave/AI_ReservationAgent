using System.Net.Http.Json;
using System.Text.Json;

namespace CabinReservation.Integration.Contracts.Api;

public interface ICabinApiClient
{
    Task<MemberDto?> GetMemberAsync(string clubNumber, CancellationToken ct);
    Task<IReadOnlyList<CalendarDayDto>> GetCalendarAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<ApiResult<ReservationDto>> CreateReservationAsync(string clubNumber, DateOnly date, string channel, string idempotencyKey, CancellationToken ct);
    Task<ApiResult<ReservationDto>> CancelReservationAsync(Guid reservationId, string clubNumber, string channel, string idempotencyKey, string? reason, CancellationToken ct);
    Task<JsonElement> JoinWaitListAsync(string clubNumber, DateOnly date, string channel, string idempotencyKey, CancellationToken ct);
    Task<JsonElement> RespondToWaitListAsync(Guid offerId, string clubNumber, bool accept, string channel, string idempotencyKey, CancellationToken ct);
    Task<IReadOnlyList<ReservationDto>> GetReservationsAsync(string clubNumber, CancellationToken ct);
}
public sealed class CabinApiClient(HttpClient http) : ICabinApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<MemberDto?> GetMemberAsync(string clubNumber, CancellationToken ct)
    {
        using var response = await http.GetAsync($"api/members/{Uri.EscapeDataString(clubNumber)}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MemberDto>(JsonOptions, ct);
    }

    public async Task<IReadOnlyList<CalendarDayDto>> GetCalendarAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
        await http.GetFromJsonAsync<List<CalendarDayDto>>($"api/calendar?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", JsonOptions, ct) ?? [];

    public Task<ApiResult<ReservationDto>> CreateReservationAsync(string clubNumber, DateOnly date, string channel, string idempotencyKey, CancellationToken ct) =>
        SendAsync<ApiResult<ReservationDto>>(HttpMethod.Post, "api/reservations", new { clubNumber, cabinDate = date, sourceChannel = channel, idempotencyKey }, ct);

    public Task<ApiResult<ReservationDto>> CancelReservationAsync(Guid reservationId, string clubNumber, string channel, string idempotencyKey, string? reason, CancellationToken ct) =>
        SendAsync<ApiResult<ReservationDto>>(HttpMethod.Delete, $"api/reservations/{reservationId}", new { clubNumber, sourceChannel = channel, idempotencyKey, reason }, ct);

    public Task<JsonElement> JoinWaitListAsync(string clubNumber, DateOnly date, string channel, string idempotencyKey, CancellationToken ct) =>
        SendAsync<JsonElement>(HttpMethod.Post, "api/waitlist", new { clubNumber, cabinDate = date, sourceChannel = channel, idempotencyKey }, ct);

    public Task<JsonElement> RespondToWaitListAsync(Guid offerId, string clubNumber, bool accept, string channel, string idempotencyKey, CancellationToken ct) =>
        SendAsync<JsonElement>(HttpMethod.Post, $"api/waitlist/{offerId}/respond", new { clubNumber, accept, sourceChannel = channel, idempotencyKey }, ct);

    public async Task<IReadOnlyList<ReservationDto>> GetReservationsAsync(string clubNumber, CancellationToken ct) =>
        await http.GetFromJsonAsync<List<ReservationDto>>($"api/reservations/member/{Uri.EscapeDataString(clubNumber)}", JsonOptions, ct) ?? [];

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body, options: JsonOptions) };
        using var response = await http.SendAsync(request, ct);
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        if (result is null) throw new InvalidOperationException($"The Cabin API returned an empty response ({(int)response.StatusCode}).");
        return result;
    }
}
