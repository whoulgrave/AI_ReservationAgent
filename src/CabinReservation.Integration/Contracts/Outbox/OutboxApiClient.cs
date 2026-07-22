using System.Net.Http.Json;
using System.Text.Json;

namespace CabinReservation.Integration.Contracts.Outbox;

public interface IOutboxApiClient
{
    Task<IReadOnlyList<OutboxLeaseDto>> LeaseAsync(int count, CancellationToken ct);
    Task CompleteAsync(Guid id, string leaseToken, string providerMessageId, CancellationToken ct);
    Task FailAsync(Guid id, string leaseToken, string error, CancellationToken ct);
}

public sealed class OutboxApiClient(HttpClient http) : IOutboxApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public async Task<IReadOnlyList<OutboxLeaseDto>> LeaseAsync(int count, CancellationToken ct) =>
        await http.GetFromJsonAsync<List<OutboxLeaseDto>>($"api/outbox/lease?count={count}", JsonOptions, ct) ?? [];

    public async Task CompleteAsync(Guid id, string leaseToken, string providerMessageId, CancellationToken ct)
    {
        using var r = await http.PostAsJsonAsync($"api/outbox/{id}/complete", new { leaseToken, providerMessageId }, ct);
        r.EnsureSuccessStatusCode();
    }

    public async Task FailAsync(Guid id, string leaseToken, string error, CancellationToken ct)
    {
        using var r = await http.PostAsJsonAsync($"api/outbox/{id}/fail", new { leaseToken, error }, ct);
        r.EnsureSuccessStatusCode();
    }
}
