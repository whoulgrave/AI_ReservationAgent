using CabinReservation.Persistence.Enums;

namespace CabinReservation.Persistence.Domain;
public sealed class WaitListEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly CabinDate { get; set; }
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public WaitListStatus Status { get; set; } = WaitListStatus.Waiting;
    public DateTimeOffset RequestedUtc { get; set; }
    public DateTimeOffset? OfferedUtc { get; set; }
    public DateTimeOffset? OfferExpiresUtc { get; set; }
    public DateTimeOffset? RespondedUtc { get; set; }
    public required string SourceChannel { get; set; }
    public required string CorrelationId { get; set; }
}
