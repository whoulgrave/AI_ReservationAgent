using CabinReservation.Persistence.Enums;

namespace CabinReservation.Persistence.Domain;

public sealed class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly CabinDate { get; set; }
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;
    public required string SourceChannel { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? CancelledUtc { get; set; }
    public string? CancellationReason { get; set; }
    public required string CorrelationId { get; set; }
}
