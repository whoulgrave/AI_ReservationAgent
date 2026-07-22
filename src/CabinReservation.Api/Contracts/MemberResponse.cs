using CabinReservation.Persistence.Enums;

namespace CabinReservation.Api.Contracts;

public sealed record MemberResponse
(
    Guid Id,
    string ClubNumber,
    string FullName,
    string? EmailAddress,
    string? MobileNumber,
    string? PhoneNumber,
    CommunicationChannel? PreferredChannel,
    bool PreferredChannelVerified,
    bool IsActive,
    bool CanViewReports,
    bool CanViewAuditLog,
    bool CanUploadRoster
);
