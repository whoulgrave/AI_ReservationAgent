namespace CabinReservation.Messaging;

public sealed class MessagingOptions
{
    public string AcsConnectionString { get; init; } = "";
    public string? AcsEmailConnectionString { get; init; }
    public string SmsFromNumber { get; init; } = "";
    public string EmailSender { get; init; } = "";
    public Uri HermesConversationEndpoint { get; init; } = new("http://127.0.0.1:9000/");
    public int OutboxPollSeconds { get; init; } = 15;
}
