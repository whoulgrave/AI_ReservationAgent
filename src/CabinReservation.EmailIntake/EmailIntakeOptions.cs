namespace CabinReservation.EmailIntake;

public sealed class EmailIntakeOptions
{
    public string TenantId { get; init; } = "";
    public string ClientId { get; init; } = "";
    public string ClientSecret { get; init; } = "";
    public string MailboxAddress { get; init; } = "";
    public Uri HermesConversationEndpoint { get; init; } = new("http://127.0.0.1:9000/");
    public int PollSeconds { get; init; } = 30;
}
