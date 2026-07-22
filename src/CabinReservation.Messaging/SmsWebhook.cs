using System.Text.Json;
using Azure.Communication.Sms;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.Extensions.Options;

namespace CabinReservation.Messaging;

public static class SmsWebhook
{
    public static async Task<IResult> HandleAsync(HttpRequest request, SmsClient sms, IHermesConversationClient hermes,
        IOptions<MessagingOptions> options, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("SmsWebhook");
        BinaryData body = await BinaryData.FromStreamAsync(request.Body, ct);
        EventGridEvent[] events;
        try { events = EventGridEvent.ParseMany(body); }
        catch (Exception ex) { logger.LogWarning(ex, "Invalid Event Grid payload."); return Results.BadRequest(); }

        foreach (var evt in events)
        {
            if (evt.TryGetSystemEventData(out object? systemData) && systemData is SubscriptionValidationEventData validation)
                return Results.Ok(new SubscriptionValidationResponse { ValidationResponse = validation.ValidationCode });

            if (!string.Equals(evt.EventType, "Microsoft.Communication.SMSReceived", StringComparison.OrdinalIgnoreCase)) continue;
            using var doc = JsonDocument.Parse(evt.Data);
            var root = doc.RootElement;
            var from = root.GetProperty("from").GetString() ?? "";
            var message = root.GetProperty("message").GetString() ?? "";
            var providerId = root.TryGetProperty("messageId", out var id) ? id.GetString() ?? evt.Id : evt.Id;
            var reply = await hermes.ProcessAsync("Sms", from, providerId, message, ct);
            await sms.SendAsync(options.Value.SmsFromNumber, from, reply, new SmsSendOptions(enableDeliveryReport: true) { Tag = providerId }, ct);
        }
        return Results.Ok();
    }
}
