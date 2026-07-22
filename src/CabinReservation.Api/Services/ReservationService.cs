using CabinReservation.Api.Contracts;
using CabinReservation.Persistence.Context;
using CabinReservation.Persistence.Domain;
using CabinReservation.Persistence.Enums;
using CabinReservation.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CabinReservation.Api.Services;

public interface IReservationService
{
    Task<ApiResult<ReservationResponse>> CreateAsync(CreateReservationRequest request, string correlationId, CancellationToken ct);
    Task<ApiResult<ReservationResponse>> CancelAsync(Guid reservationId, CancelReservationRequest request, string correlationId, CancellationToken ct);
    Task<IReadOnlyList<ReservationResponse>> GetMemberReservationsAsync(string clubNumber, CancellationToken ct);
}

public sealed class ReservationService(
    CabinDbContext db,
    ISystemClock clock,
    IAuditWriter audit,
    IOptions<ReservationPolicyOptions> policyOptions) : IReservationService
{
    private readonly ReservationPolicyOptions _policy = policyOptions.Value;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ApiResult<ReservationResponse>> CreateAsync(
        CreateReservationRequest request,
        string correlationId,
        CancellationToken ct)
    {
        var prior = await db.IdempotencyRecords.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Key == request.IdempotencyKey, ct);

        if (prior is not null)
            return JsonSerializer.Deserialize<ApiResult<ReservationResponse>>(prior.ResponseJson, JsonOptions)!;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var member = await db.Members.SingleOrDefaultAsync(x => x.ClubNumber == request.ClubNumber, ct);
        if (member is null || !member.IsActive)
            return await FailAsync("MEMBER_NOT_ACTIVE", "The member is not active.", request, correlationId, null, ct);

        var localToday = GetCabinLocalDate(clock.UtcNow);
        if (request.CabinDate < localToday)
            return await FailAsync("DATE_IN_PAST", "A reservation cannot be made for a past date.", request, correlationId, member, ct);

        var activeNightCount = await db.Reservations.CountAsync(
            x => x.MemberId == member.Id &&
                 x.Status == ReservationStatus.Confirmed &&
                 x.CabinDate >= localToday, ct);

        if (activeNightCount >= _policy.MaximumActiveNightsPerMember)
            return await FailAsync(
                "MEMBER_NIGHT_LIMIT_REACHED",
                $"The member already holds the maximum of {_policy.MaximumActiveNightsPerMember} active nights.",
                request, correlationId, member, ct);

        var alreadyReserved = await db.Reservations.AnyAsync(
            x => x.CabinDate == request.CabinDate &&
                 x.Status == ReservationStatus.Confirmed, ct);

        if (alreadyReserved)
            return await FailAsync("DATE_ALREADY_RESERVED", "The requested date is already reserved.", request, correlationId, member, ct);

        var reservation = new Reservation
        {
            CabinDate = request.CabinDate,
            MemberId = member.Id,
            Status = ReservationStatus.Confirmed,
            SourceChannel = request.SourceChannel,
            CreatedUtc = clock.UtcNow,
            CorrelationId = correlationId
        };

        db.Reservations.Add(reservation);
        audit.Add(db, ActorType.Member, member.Id, "ReservationCreated", nameof(Reservation),
            reservation.Id.ToString(), request.SourceChannel, correlationId, "Success", after: reservation);

        QueueConfirmation(member, "ReservationConfirmed", new
        {
            reservation.Id,
            reservation.CabinDate,
            cancellationDeadline = GetCancellationDeadlineUtc(reservation.CabinDate)
        });

        try
        {
            await db.SaveChangesAsync(ct);
            var response = ToResponse(reservation, member);
            db.IdempotencyRecords.Add(new IdempotencyRecord
            {
                Key = request.IdempotencyKey,
                Operation = "CreateReservation",
                ResponseJson = JsonSerializer.Serialize(
                    new ApiResult<ReservationResponse>(true, "RESERVATION_CREATED", "Reservation created.", response), JsonOptions),
                CreatedUtc = clock.UtcNow
            });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return new(true, "RESERVATION_CREATED", "Reservation created.", response);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            return new(false, "DATE_ALREADY_RESERVED", "The requested date was reserved by another request.", null);
        }
    }

    public async Task<ApiResult<ReservationResponse>> CancelAsync(
        Guid reservationId,
        CancelReservationRequest request,
        string correlationId,
        CancellationToken ct)
    {
        var prior = await db.IdempotencyRecords.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Key == request.IdempotencyKey, ct);

        if (prior is not null)
            return JsonSerializer.Deserialize<ApiResult<ReservationResponse>>(prior.ResponseJson, JsonOptions)!;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var reservation = await db.Reservations
            .Include(x => x.Member)
            .SingleOrDefaultAsync(x => x.Id == reservationId, ct);

        if (reservation is null)
            return new(false, "RESERVATION_NOT_FOUND", "Reservation not found.", null);

        if (!string.Equals(reservation.Member.ClubNumber, request.ClubNumber, StringComparison.OrdinalIgnoreCase))
            return new(false, "RESERVATION_NOT_OWNED", "The reservation is not owned by this member.", null);

        if (reservation.Status != ReservationStatus.Confirmed)
            return new(false, "RESERVATION_NOT_ACTIVE", "The reservation is not active.", ToResponse(reservation, reservation.Member));

        var deadlineUtc = GetCancellationDeadlineUtc(reservation.CabinDate);
        if (clock.UtcNow > deadlineUtc)
            return new(false, "CANCELLATION_DEADLINE_PASSED",
                $"The cancellation deadline was {deadlineUtc:u}.", ToResponse(reservation, reservation.Member));

        var before = new { reservation.Status, reservation.CancelledUtc, reservation.CancellationReason };
        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledUtc = clock.UtcNow;
        reservation.CancellationReason = request.Reason;

        audit.Add(db, ActorType.Member, reservation.MemberId, "ReservationCancelled", nameof(Reservation),
            reservation.Id.ToString(), request.SourceChannel, correlationId, "Success", before, reservation);

        QueueConfirmation(reservation.Member, "ReservationCancelled", new
        {
            reservation.Id,
            reservation.CabinDate,
            reservation.CancelledUtc
        });

        await db.SaveChangesAsync(ct);

        var response = ToResponse(reservation, reservation.Member);
        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = request.IdempotencyKey,
            Operation = "CancelReservation",
            ResponseJson = JsonSerializer.Serialize(
                new ApiResult<ReservationResponse>(true, "RESERVATION_CANCELLED", "Reservation cancelled.", response), JsonOptions),
            CreatedUtc = clock.UtcNow
        });

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new(true, "RESERVATION_CANCELLED", "Reservation cancelled.", response);
    }

    public async Task<IReadOnlyList<ReservationResponse>> GetMemberReservationsAsync(
        string clubNumber,
        CancellationToken ct)
    {
        return await db.Reservations
            .AsNoTracking()
            .Where(x => x.Member.ClubNumber == clubNumber)
            .OrderBy(x => x.CabinDate)
            .Select(x => new ReservationResponse(
                x.Id, x.CabinDate, x.Member.ClubNumber, x.Member.FullName,
                x.Status, x.SourceChannel, x.CreatedUtc, x.CancelledUtc))
            .ToListAsync(ct);
    }

    private async Task<ApiResult<ReservationResponse>> FailAsync(
        string code,
        string message,
        CreateReservationRequest request,
        string correlationId,
        Member? member,
        CancellationToken ct)
    {
        audit.Add(db, member is null ? ActorType.Agent : ActorType.Member, member?.Id,
            "ReservationCreateRejected", nameof(Reservation), null,
            request.SourceChannel, correlationId, "Rejected",
            after: request, failureReason: code);
        await db.SaveChangesAsync(ct);
        return new(false, code, message, null);
    }

    private void QueueConfirmation(Member member, string messageType, object payload)
    {
        if (member.PreferredChannel is null)
            return;

        db.OutboundMessages.Add(new OutboundMessage
        {
            MemberId = member.Id,
            Channel = member.PreferredChannel.Value,
            MessageType = messageType,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            CreatedUtc = clock.UtcNow,
            NextAttemptUtc = clock.UtcNow
        });
    }

    private DateOnly GetCabinLocalDate(DateTimeOffset utc)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(_policy.CabinTimeZoneId);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, zone).DateTime);
    }

    private DateTimeOffset GetCancellationDeadlineUtc(DateOnly cabinDate)
    {
        var deadlineDate = cabinDate.AddDays(-_policy.CancellationDaysBeforeStay);
        var local = deadlineDate.ToDateTime(_policy.CancellationCutoffLocalTime, DateTimeKind.Unspecified);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(_policy.CabinTimeZoneId);
        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }

    private static ReservationResponse ToResponse(Reservation reservation, Member member) =>
        new(reservation.Id, reservation.CabinDate, member.ClubNumber, member.FullName,
            reservation.Status, reservation.SourceChannel, reservation.CreatedUtc, reservation.CancelledUtc);
}
