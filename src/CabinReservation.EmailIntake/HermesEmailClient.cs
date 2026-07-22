using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace CabinReservation.EmailIntake;

public interface IHermesEmailClient { Task ProcessAsync(string sender, string providerMessageId, string subject, string body, CancellationToken ct); }

public sealed class HermesEmailClient(HttpClient http, IOptions<EmailIntakeOptions> options) : IHermesEmailClient
{
    public async Task ProcessAsync(string sender, string providerMessageId, string subject, string body, CancellationToken ct)
    {
        var uri = new Uri(options.Value.HermesConversationEndpoint, "api/conversations/process");
        using var response = await http.PostAsJsonAsync(uri, new { channel = "Email", sender, providerMessageId, message = $"Subject: {subject}{body}" }, ct);
        response.EnsureSuccessStatusCode();
    }
}
