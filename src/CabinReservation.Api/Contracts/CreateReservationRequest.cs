using System.ComponentModel.DataAnnotations;

namespace CabinReservation.Api.Contracts;

public sealed record CreateReservationRequest
(
    [property: Required] string ClubNumber,
    DateOnly CabinDate,
    [property: Required] string SourceChannel,
    [property: Required] string IdempotencyKey
);
