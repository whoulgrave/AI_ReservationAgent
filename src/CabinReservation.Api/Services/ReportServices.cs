using CabinReservation.Api.Contracts;
using CabinReservation.Persistence.Context;
using CabinReservation.Persistence.Domain;
using CabinReservation.Persistence.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CabinReservation.Api.Services;

public interface IReportService
{
    Task<IReadOnlyList<CalendarDayResponse>> GetCalendarAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<IReadOnlyList<AuditEvent>> GetAuditAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
}

//public sealed class ReportService(CabinDbContext db) : IReportService
public sealed class ReportService : IReportService
{
    private readonly IDbContextFactory<CabinDbContext> _contextFactory;

    public ReportService(IDbContextFactory<CabinDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<CalendarDayResponse>> GetCalendarAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        if (to < from)
            throw new ArgumentException("'to' must be on or after 'from'.");

        if (to.DayNumber - from.DayNumber > 366)
            throw new ArgumentException("Calendar reports are limited to 367 days.");

        var reservedDates = (await db.Reservations.AsNoTracking()
            .Where(x => x.CabinDate >= from &&
                        x.CabinDate <= to &&
                        x.Status == ReservationStatus.Confirmed)
            .Select(x => x.CabinDate)
            .ToListAsync(ct))
            .ToHashSet();

        var result = new List<CalendarDayResponse>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var available = !reservedDates.Contains(date);
            result.Add(new(date, available, available ? "Available" : "Reserved"));
        }

        return result;
    }

    public async Task<IReadOnlyList<AuditEvent>> GetAuditAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        return await db.AuditEvents.AsNoTracking()
            .Where(x => x.OccurredUtc >= fromUtc && x.OccurredUtc <= toUtc)
            .OrderBy(x => x.OccurredUtc)
            .Take(10000)
            .ToListAsync(ct);
    }
}

