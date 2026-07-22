using CabinReservation.Persistence.Enums;

namespace CabinReservation.Persistence.Domain;

public sealed class AuditEvent
{
    public long Id { get; set; }
    public DateTimeOffset OccurredUtc { get; set; }
    public Guid? ActorMemberId { get; set; }
    public ActorType ActorType { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public string? EntityId { get; set; }
    public required string SourceChannel { get; set; }
    public required string CorrelationId { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public required string Outcome { get; set; }
    public string? FailureReason { get; set; }
}
