using System.ComponentModel.DataAnnotations;

namespace CabinReservation.Api.Contracts;

public sealed record JoinWaitListRequest
(
    [property: Required] string ClubNumber,
    DateOnly CabinDate,
    [property: Required] string SourceChannel,
    [property: Required] string IdempotencyKey
);
