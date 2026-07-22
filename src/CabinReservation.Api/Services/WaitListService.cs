using CabinReservation.Api.Contracts;
using CabinReservation.Persistence.Context;
using CabinReservation.Persistence.Domain;
using CabinReservation.Persistence.Enums;
using CabinReservation.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CabinReservation.Api.Services;

public interface IWaitListService
{
    Task<ApiResult<WaitListResponse>> JoinAsync(JoinWaitListRequest request, string correlationId, CancellationToken ct);
    Task<ApiResult<object>> RespondAsync(Guid offerId, RespondToWaitListOfferRequest request, string correlationId, CancellationToken ct);
    Task ProcessOffersAsync(CancellationToken ct);
}

public sealed class WaitListService(
    CabinDbContext db,
    ISystemClock clock,
    IAuditWriter audit,
    IOptions<ReservationPolicyOptions> options) : IWaitListService
{
    private readonly ReservationPolicyOptions _policy = options.Value;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ApiResult<WaitListResponse>> JoinAsync(
        JoinWaitListRequest request,
        string correlationId,
        CancellationToken ct)
    {
        var member = await db.Members.SingleOrDefaultAsync(
            x => x.ClubNumber == request.ClubNumber && x.IsActive, ct);

        if (member is null)
            return new(false, "MEMBER_NOT_ACTIVE", "The member is not active.", null);

        var isReserved = await db.Reservations.AnyAsync(
            x => x.CabinDate == request.CabinDate && x.Status == ReservationStatus.Confirmed, ct);

        if (!isReserved)
            return new(false, "DATE_AVAILABLE", "The date is currently available and may be reserved directly.", null);

        var duplicate = await db.WaitListEntries.AnyAsync(
            x => x.CabinDate == request.CabinDate &&
                 x.MemberId == member.Id &&
                 (x.Status == WaitListStatus.Waiting || x.Status == WaitListStatus.Offered), ct);

        if (duplicate)
            return new(false, "ALREADY_WAITING", "The member is already on the waiting list for this date.", null);

        var entry = new WaitListEntry
        {
            CabinDate = request.CabinDate,
            MemberId = member.Id,
            RequestedUtc = clock.UtcNow,
            SourceChannel = request.SourceChannel,
            CorrelationId = correlationId
        };

        db.WaitListEntries.Add(entry);
        audit.Add(db, ActorType.Member, member.Id, "WaitListJoined", nameof(WaitListEntry),
            entry.Id.ToString(), request.SourceChannel, correlationId, "Success", after: entry);
        await db.SaveChangesAsync(ct);

        return new(true, "WAIT_LIST_JOINED", "The member was added to the waiting list.",
            new(entry.Id, entry.CabinDate, member.ClubNumber, entry.Status, entry.RequestedUtc, entry.OfferExpiresUtc));
    }

    public async Task<ApiResult<object>> RespondAsync(
        Guid offerId,
        RespondToWaitListOfferRequest request,
        string correlationId,
        CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var entry = await db.WaitListEntries.Include(x => x.Member)
            .SingleOrDefaultAsync(x => x.Id == offerId, ct);

        if (entry is null)
            return new(false, "OFFER_NOT_FOUND", "Waiting-list offer not found.", null);

        if (!string.Equals(entry.Member.ClubNumber, request.ClubNumber, StringComparison.OrdinalIgnoreCase))
            return new(false, "OFFER_NOT_OWNED", "This offer does not belong to the member.", null);

        if (entry.Status != WaitListStatus.Offered || entry.OfferExpiresUtc <= clock.UtcNow)
            return new(false, "OFFER_NOT_ACTIVE", "The waiting-list offer is no longer active.", null);

        if (!request.Accept)
        {
            entry.Status = WaitListStatus.Declined;
            entry.RespondedUtc = clock.UtcNow;
            audit.Add(db, ActorType.Member, entry.MemberId, "WaitListOfferDeclined",
                nameof(WaitListEntry), entry.Id.ToString(), request.SourceChannel,
                correlationId, "Success");
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new(true, "OFFER_DECLINED", "The offer was declined.", new { entry.Id });
        }

        if (!entry.Member.IsActive)
            return new(false, "MEMBER_NOT_ACTIVE", "The member is no longer active.", null);

        var localToday = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeCount = await db.Reservations.CountAsync(
            x => x.MemberId == entry.MemberId &&
                 x.Status == ReservationStatus.Confirmed &&
                 x.CabinDate >= localToday, ct);

        if (activeCount >= _policy.MaximumActiveNightsPerMember)
            return new(false, "MEMBER_NIGHT_LIMIT_REACHED", "The member now holds the maximum active nights.", null);

        var occupied = await db.Reservations.AnyAsync(
            x => x.CabinDate == entry.CabinDate && x.Status == ReservationStatus.Confirmed, ct);

        if (occupied)
            return new(false, "DATE_ALREADY_RESERVED", "The date is no longer available.", null);

        var reservation = new Reservation
        {
            CabinDate = entry.CabinDate,
            MemberId = entry.MemberId,
            Status = ReservationStatus.Confirmed,
            SourceChannel = request.SourceChannel,
            CreatedUtc = clock.UtcNow,
            CorrelationId = correlationId
        };

        entry.Status = WaitListStatus.Accepted;
        entry.RespondedUtc = clock.UtcNow;
        db.Reservations.Add(reservation);

        audit.Add(db, ActorType.Member, entry.MemberId, "WaitListOfferAccepted",
            nameof(WaitListEntry), entry.Id.ToString(), request.SourceChannel,
            correlationId, "Success", after: new { reservation.Id, reservation.CabinDate });

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(true, "OFFER_ACCEPTED", "The waiting-list offer was accepted and the reservation was created.",
            new { reservation.Id, reservation.CabinDate });
    }

    public async Task ProcessOffersAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;

        var expired = await db.WaitListEntries
            .Where(x => x.Status == WaitListStatus.Offered && x.OfferExpiresUtc <= now)
            .ToListAsync(ct);

        foreach (var entry in expired)
        {
            entry.Status = WaitListStatus.Expired;
            entry.RespondedUtc = now;
            audit.Add(db, ActorType.System, null, "WaitListOfferExpired",
                nameof(WaitListEntry), entry.Id.ToString(), "System",
                entry.CorrelationId, "Success");
        }

        var availableWaitingDates = await db.WaitListEntries
            .Where(x => x.Status == WaitListStatus.Waiting)
            .Select(x => x.CabinDate)
            .Distinct()
            .ToListAsync(ct);

        foreach (var date in availableWaitingDates)
        {
            var occupied = await db.Reservations.AnyAsync(
                x => x.CabinDate == date && x.Status == ReservationStatus.Confirmed, ct);
            var activeOffer = await db.WaitListEntries.AnyAsync(
                x => x.CabinDate == date && x.Status == WaitListStatus.Offered && x.OfferExpiresUtc > now, ct);

            if (occupied || activeOffer)
                continue;

            var next = await db.WaitListEntries.Include(x => x.Member)
                .Where(x => x.CabinDate == date &&
                            x.Status == WaitListStatus.Waiting &&
                            x.Member.IsActive)
                .OrderBy(x => x.RequestedUtc)
                .FirstOrDefaultAsync(ct);

            if (next is null)
                continue;

            next.Status = WaitListStatus.Offered;
            next.OfferedUtc = now;
            next.OfferExpiresUtc = now.AddMinutes(_policy.WaitListOfferMinutes);

            if (next.Member.PreferredChannel is not null)
            {
                db.OutboundMessages.Add(new OutboundMessage
                {
                    MemberId = next.MemberId,
                    Channel = next.Member.PreferredChannel.Value,
                    MessageType = "WaitListOffer",
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        next.Id,
                        next.CabinDate,
                        next.OfferExpiresUtc
                    }, JsonOptions),
                    CreatedUtc = now,
                    NextAttemptUtc = now
                });
            }

            audit.Add(db, ActorType.System, null, "WaitListOfferCreated",
                nameof(WaitListEntry), next.Id.ToString(), "System",
                next.CorrelationId, "Success", after: next);
        }

        await db.SaveChangesAsync(ct);
    }
}

public sealed class WaitListOfferWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReservationPolicyOptions> options,
    ILogger<WaitListOfferWorker> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(options.Value.WorkerIntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IWaitListService>();
                await service.ProcessOffersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Waiting-list worker failed.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
