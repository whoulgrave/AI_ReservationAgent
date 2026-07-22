using System.ComponentModel.DataAnnotations;

namespace CabinReservation.Api.Contracts;

public sealed record ApplyRosterRequest
(
    [property: Required] string FileName,
    [property: Required] string UploadedBy,
    [property: Required] IReadOnlyList<RosterMemberRow> Members,
    bool ConfirmCancellationOfExistingReservations
);
