namespace CabinReservation.Integration.Contracts.Api;

public sealed record MemberDto(Guid Id, string ClubNumber, string FullName, string? EmailAddress,
    string? MobileNumber, string? PhoneNumber, int? PreferredChannel, bool PreferredChannelVerified,
    bool IsActive, bool CanViewReports, bool CanViewAuditLog, bool CanUploadRoster);
