using System.Text;
using System.Text.Json;
using CabinReservation.Api.Contracts;
using CabinReservation.Persistence.Context;
using CabinReservation.Persistence.Domain;
using CabinReservation.Persistence.Enums;
using CabinReservation.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

public interface IRosterService
{
    Task<ApiResult<RosterPreviewResponse>> PreviewAsync(ApplyRosterRequest request, CancellationToken ct);
    Task<ApiResult<RosterPreviewResponse>> ApplyAsync(ApplyRosterRequest request, string correlationId, CancellationToken ct);
}

public sealed class RosterService(CabinDbContext db, ISystemClock clock, IAuditWriter audit) : IRosterService
{
    public async Task<ApiResult<RosterPreviewResponse>> PreviewAsync(
        ApplyRosterRequest request,
        CancellationToken ct)
    {
        var validationError = ValidateRows(request.Members);
        if (validationError is not null)
            return new(false, "INVALID_ROSTER", validationError, null);

        var existing = await db.Members.AsNoTracking().ToListAsync(ct);
        var incomingByNumber = request.Members.ToDictionary(x => x.ClubNumber, StringComparer.OrdinalIgnoreCase);
        var added = request.Members.Count(x => existing.All(e =>
            !string.Equals(e.ClubNumber, x.ClubNumber, StringComparison.OrdinalIgnoreCase)));
        var updated = request.Members.Count - added;
        var deactivatedMembers = existing.Where(x => x.IsActive && !incomingByNumber.ContainsKey(x.ClubNumber)).ToList();
        var deactivatedIds = deactivatedMembers.Select(x => x.Id).ToList();
        var reservationsToCancel = await db.Reservations.CountAsync(
            x => deactivatedIds.Contains(x.MemberId) && x.Status == ReservationStatus.Confirmed, ct);

        return new(true, "ROSTER_PREVIEW", "Roster preview created.",
            new(added, updated, deactivatedMembers.Count, reservationsToCancel,
                deactivatedMembers.Select(x => x.ClubNumber).OrderBy(x => x).ToList()));
    }

    public async Task<ApiResult<RosterPreviewResponse>> ApplyAsync(
        ApplyRosterRequest request,
        string correlationId,
        CancellationToken ct)
    {
        var previewResult = await PreviewAsync(request, ct);
        if (!previewResult.Success || previewResult.Data is null)
            return previewResult;

        if (previewResult.Data.ReservationsToCancel > 0 && !request.ConfirmCancellationOfExistingReservations)
            return new(false, "CANCELLATION_CONFIRMATION_REQUIRED",
                "This roster would cancel existing reservations. Explicit confirmation is required.",
                previewResult.Data);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var now = clock.UtcNow;
        var existing = await db.Members.ToListAsync(ct);
        var existingByNumber = existing.ToDictionary(x => x.ClubNumber, StringComparer.OrdinalIgnoreCase);
        var incomingNumbers = request.Members.Select(x => x.ClubNumber)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in request.Members)
        {
            if (!existingByNumber.TryGetValue(row.ClubNumber, out var member))
            {
                member = new Member
                {
                    ClubNumber = row.ClubNumber,
                    FullName = row.FullName,
                    CreatedUtc = now
                };
                db.Members.Add(member);
            }

            member.FullName = row.FullName;
            member.EmailAddress = row.EmailAddress;
            member.MobileNumber = row.MobileNumber;
            member.PhoneNumber = row.PhoneNumber;
            member.IsActive = row.IsActive;
            member.CanViewReports = row.CanViewReports;
            member.CanViewAuditLog = row.CanViewAuditLog;
            member.CanUploadRoster = row.CanUploadRoster;
            member.UpdatedUtc = now;
        }

        var deactivated = existing.Where(x => x.IsActive && !incomingNumbers.Contains(x.ClubNumber)).ToList();
        foreach (var member in deactivated)
        {
            member.IsActive = false;
            member.UpdatedUtc = now;

            var reservations = await db.Reservations.Where(
                x => x.MemberId == member.Id && x.Status == ReservationStatus.Confirmed).ToListAsync(ct);

            foreach (var reservation in reservations)
            {
                reservation.Status = ReservationStatus.Cancelled;
                reservation.CancelledUtc = now;
                reservation.CancellationReason = "Member removed from active roster.";
            }
        }

        var rosterJson = JsonSerializer.Serialize(request.Members);
        var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rosterJson)));

        var import = new RosterImport
        {
            FileName = request.FileName,
            UploadedBy = request.UploadedBy,
            UploadedUtc = now,
            AppliedUtc = now,
            Sha256 = sha,
            SummaryJson = JsonSerializer.Serialize(previewResult.Data)
        };
        db.RosterImports.Add(import);

        audit.Add(db, ActorType.Administrator, null, "RosterApplied", nameof(RosterImport),
            import.Id.ToString(), "AdminApi", correlationId, "Success",
            after: previewResult.Data);

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new(true, "ROSTER_APPLIED", "Roster applied.", previewResult.Data);
    }

    private static string? ValidateRows(IReadOnlyList<RosterMemberRow> rows)
    {
        if (rows.Count == 0)
            return "The roster is empty.";

        var duplicate = rows.GroupBy(x => x.ClubNumber, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            return $"Duplicate club number: {duplicate.Key}.";

        if (rows.Any(x => string.IsNullOrWhiteSpace(x.ClubNumber) || string.IsNullOrWhiteSpace(x.FullName)))
            return "Every roster row requires a club number and full name.";

        return null;
    }
}

