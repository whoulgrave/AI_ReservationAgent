using System.ComponentModel.DataAnnotations;

namespace CabinReservation.Api.Contracts;

public sealed record CancelReservationRequest
(
    [property: Required] string ClubNumber,
    [property: Required] string SourceChannel,
    [property: Required] string IdempotencyKey,
    string? Reason
);