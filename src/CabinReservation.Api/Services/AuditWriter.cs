using CabinReservation.Persistence.Context;
using CabinReservation.Persistence.Enums;
using CabinReservation.Persistence.Domain;
using System.Text.Json;

namespace CabinReservation.Api.Services;

public interface IAuditWriter
{
    void Add(
        CabinDbContext db,
        ActorType actorType,
        Guid? actorMemberId,
        string action,
        string entityType,
        string? entityId,
        string sourceChannel,
        string correlationId,
        string outcome,
        object? before = null,
        object? after = null,
        string? failureReason = null);
}


public sealed class AuditWriter(ISystemClock clock) : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Add(
        CabinDbContext db,
        ActorType actorType,
        Guid? actorMemberId,
        string action,
        string entityType,
        string? entityId,
        string sourceChannel,
        string correlationId,
        string outcome,
        object? before = null,
        object? after = null,
        string? failureReason = null)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            OccurredUtc = clock.UtcNow,
            ActorType = actorType,
            ActorMemberId = actorMemberId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            SourceChannel = sourceChannel,
            CorrelationId = correlationId,
            Outcome = outcome,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            FailureReason = failureReason
        });
    }
}
