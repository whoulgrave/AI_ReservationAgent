namespace CabinReservation.Api.Contracts;

public sealed record RosterMemberRow
(
    string ClubNumber,
    string FullName,
    string? EmailAddress,
    string? MobileNumber,
    string? PhoneNumber,
    bool IsActive,
    bool CanViewReports,
    bool CanViewAuditLog,
    bool CanUploadRoster
);
