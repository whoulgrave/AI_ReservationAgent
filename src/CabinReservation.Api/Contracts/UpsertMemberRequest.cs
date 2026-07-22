using System.ComponentModel.DataAnnotations;

namespace CabinReservation.Api.Contracts;

public sealed record UpsertMemberRequest
(
    [property: Required] string ClubNumber,
    [property: Required] string FullName,
    string? EmailAddress,
    string? MobileNumber,
    string? PhoneNumber,
    bool IsActive,
    bool CanViewReports,
    bool CanViewAuditLog,
    bool CanUploadRoster
);
