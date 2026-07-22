using Microsoft.Extensions.Options;

namespace CabinReservation.Messaging;

public interface IHermesConversationClient
{
    Task<string> ProcessAsync(string channel, string sender, string providerMessageId, string message, CancellationToken ct);
}

public sealed class HermesConversationClient(HttpClient http, IOptions<MessagingOptions> options) : IHermesConversationClient
{
    public async Task<string> ProcessAsync(string channel, string sender, string providerMessageId, string message, CancellationToken ct)
    {
        var endpoint = new Uri(options.Value.HermesConversationEndpoint, "api/conversations/process");
        using var response = await http.PostAsJsonAsync(endpoint, new { channel, sender, providerMessageId, message }, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<HermesReply>(cancellationToken: ct);
        return result?.Reply ?? "I could not process that request. Please contact the cabin administrator.";
    }

    private sealed record HermesReply(string Reply);
}
