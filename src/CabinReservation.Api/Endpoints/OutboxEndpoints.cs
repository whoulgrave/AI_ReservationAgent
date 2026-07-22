using CabinReservation.Persistence.Context;
using CabinReservation.Persistence.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace CabinReservation.Api.Endpoints;

public static class OutboxEndpoints
{
    public static IEndpointRouteBuilder MapOutboxEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/outbox").WithTags("Outbox");

        group.MapGet("/lease", async ([FromQuery] int count, [FromServices] CabinDbContext db, CancellationToken ct) =>
        {
            count = Math.Clamp(count, 1, 50);
            var now = DateTimeOffset.UtcNow;
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var messages = await db.OutboundMessages
                .Where(x => x.Status == OutboundMessageStatus.Pending && x.NextAttemptUtc <= now &&
                    (x.LeaseExpiresUtc == null || x.LeaseExpiresUtc <= now))
                .OrderBy(x => x.CreatedUtc).Take(count).ToListAsync(ct);
            var memberIds = messages.Select(x => x.MemberId).Distinct().ToList();
            var members = await db.Members.Where(x => memberIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var result = new List<object>();
            foreach (var message in messages)
            {
                var token = Guid.NewGuid().ToString("N");
                message.LeaseToken = token;
                message.LeaseExpiresUtc = now.AddMinutes(2);
                var member = members[message.MemberId];
                result.Add(new { message.Id, message.MemberId, Channel = (int)message.Channel, message.MessageType,
                    message.PayloadJson, member.EmailAddress, member.MobileNumber, member.PhoneNumber, LeaseToken = token });
            }
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Results.Ok(result);
        });

        group.MapPost("/{id:guid}/complete", async (
            [FromRoute] Guid id,
            [FromBody] CompleteOutboxRequest request,
            [FromServices] CabinDbContext db,
            CancellationToken ct) =>
        {
            var message = await db.OutboundMessages.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (message is null) return Results.NotFound();
            if (!string.Equals(message.LeaseToken, request.LeaseToken, StringComparison.Ordinal)) return Results.Conflict(new { error = "Lease mismatch." });
            message.Status = OutboundMessageStatus.Sent;
            message.ProviderMessageId = request.ProviderMessageId;
            message.SentUtc = DateTimeOffset.UtcNow;
            message.LeaseToken = null;
            message.LeaseExpiresUtc = null;
            message.LastError = null;
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        });

        group.MapPost("/{id:guid}/fail", async (
            [FromRoute] Guid id,
            [FromBody] FailOutboxRequest request,
            [FromServices] CabinDbContext db,
            CancellationToken ct) =>
        {
            var message = await db.OutboundMessages.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (message is null) return Results.NotFound();
            if (!string.Equals(message.LeaseToken, request.LeaseToken, StringComparison.Ordinal)) return Results.Conflict(new { error = "Lease mismatch." });
            message.AttemptCount++;
            message.LastError = request.Error.Length > 2000 ? request.Error[..2000] : request.Error;
            message.LeaseToken = null;
            message.LeaseExpiresUtc = null;
            if (message.AttemptCount >= 8) message.Status = OutboundMessageStatus.Failed;
            else message.NextAttemptUtc = DateTimeOffset.UtcNow.AddMinutes(Math.Min(60, Math.Pow(2, message.AttemptCount)));
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        });
        return app;
    }

    public sealed record CompleteOutboxRequest(string LeaseToken, string ProviderMessageId);
    public sealed record FailOutboxRequest(string LeaseToken, string Error);
}
