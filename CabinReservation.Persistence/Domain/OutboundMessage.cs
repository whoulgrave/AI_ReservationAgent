
using CabinReservation.Persistence.Enums;

namespace CabinReservation.Persistence.Domain;

public sealed class OutboundMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public CommunicationChannel Channel { get; set; }
    public required string MessageType { get; set; }
    public required string PayloadJson { get; set; }
    public OutboundMessageStatus Status { get; set; } = OutboundMessageStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTimeOffset NextAttemptUtc { get; set; }
    public string? ProviderMessageId { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? SentUtc { get; set; }
    public string? LeaseToken { get; set; }
    public DateTimeOffset? LeaseExpiresUtc { get; set; }
    public string? LastError { get; set; }
}
