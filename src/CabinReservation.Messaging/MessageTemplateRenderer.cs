using System.Text.Json;

namespace CabinReservation.Messaging;

public interface IMessageTemplateRenderer { string Render(string messageType, string payloadJson); }

public sealed class MessageTemplateRenderer : IMessageTemplateRenderer
{
    public string Render(string messageType, string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;
        return messageType switch
        {
            "ReservationConfirmed" => $"Cabin reservation confirmed for {root.GetProperty("cabinDate").GetString()}. Confirmation {root.GetProperty("id").GetString()}.",
            "ReservationCancelled" => $"Cabin reservation for {root.GetProperty("cabinDate").GetString()} was cancelled.",
            "WaitListOffer" => $"The cabin is available for {root.GetProperty("cabinDate").GetString()}. Reply ACCEPT before {root.GetProperty("offerExpiresUtc").GetString()}.",
            _ => $"Cabin service notification: {payloadJson}"
        };
    }
}
