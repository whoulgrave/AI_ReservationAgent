using CabinReservation.Api.Contracts;
using CabinReservation.Persistence.Context;
using CabinReservation.Persistence.Domain;
using CabinReservation.Persistence.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace CabinReservation.Api.Services;

public interface IMemberService
{
    Task<MemberResponse?> GetAsync(string clubNumber, CancellationToken ct);
    Task<ApiResult<MemberResponse>> UpdatePreferenceAsync(string clubNumber, UpdatePreferenceRequest request, string correlationId, CancellationToken ct);
    Task<IReadOnlyList<MemberResponse>> GetAllAsync(CancellationToken ct);
}

public sealed class MemberService : IMemberService
{
    private readonly IDbContextFactory<CabinDbContext> _contextFactory;
    private readonly ISystemClock clock;
    private readonly IAuditWriter audit;

    public MemberService(IDbContextFactory<CabinDbContext> contextFactory, ISystemClock clock, IAuditWriter audit)
    {
        _contextFactory = contextFactory;
        this.clock = clock;
        this.audit = audit;
    }

    public async Task<MemberResponse?> GetAsync(string clubNumber, CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var member = await db.Members.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClubNumber == clubNumber, ct);
        return member is null ? null : ToResponse(member);
    }

    public async Task<IReadOnlyList<MemberResponse>> GetAllAsync(CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.Members.AsNoTracking().OrderBy(x => x.ClubNumber)
            .Select(x => new MemberResponse(
                x.Id, x.ClubNumber, x.FullName, x.EmailAddress, x.MobileNumber, x.PhoneNumber,
                x.PreferredChannel, x.PreferredChannelVerified, x.IsActive,
                x.CanViewReports, x.CanViewAuditLog, x.CanUploadRoster))
            .ToListAsync(ct);
    }

    public async Task<ApiResult<MemberResponse>> UpdatePreferenceAsync(
        string clubNumber,
        UpdatePreferenceRequest request,
        string correlationId,
        CancellationToken ct)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var member = await db.Members.SingleOrDefaultAsync(x => x.ClubNumber == clubNumber, ct);
        if (member is null)
            return new(false, "MEMBER_NOT_FOUND", "Member not found.", null);

        if (!ChannelAvailable(member, request.PreferredChannel))
            return new(false, "CHANNEL_NOT_AVAILABLE",
                "The selected communication channel has no configured contact address or number.", null);

        var before = new { member.PreferredChannel, member.PreferredChannelVerified };
        member.PreferredChannel = request.PreferredChannel;
        member.PreferredChannelVerified = request.Verified;
        member.UpdatedUtc = clock.UtcNow;

        audit.Add(db, ActorType.Member, member.Id, "CommunicationPreferenceChanged",
            nameof(Member), member.Id.ToString(), request.SourceChannel, correlationId,
            "Success", before, new { member.PreferredChannel, member.PreferredChannelVerified });

        await db.SaveChangesAsync(ct);
        return new(true, "PREFERENCE_UPDATED", "Communication preference updated.", ToResponse(member));
    }

    private static bool ChannelAvailable(Member member, CommunicationChannel channel) => channel switch
    {
        CommunicationChannel.Email => !string.IsNullOrWhiteSpace(member.EmailAddress),
        CommunicationChannel.Sms => !string.IsNullOrWhiteSpace(member.MobileNumber),
        CommunicationChannel.Phone => !string.IsNullOrWhiteSpace(member.PhoneNumber) ||
                                      !string.IsNullOrWhiteSpace(member.MobileNumber),
        _ => false
    };

    private static MemberResponse ToResponse(Member x) =>
        new(x.Id, x.ClubNumber, x.FullName, x.EmailAddress, x.MobileNumber, x.PhoneNumber,
            x.PreferredChannel, x.PreferredChannelVerified, x.IsActive,
            x.CanViewReports, x.CanViewAuditLog, x.CanUploadRoster);
}
