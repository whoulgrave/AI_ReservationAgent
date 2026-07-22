using System.ComponentModel.DataAnnotations;

namespace CabinReservation.Api.Contracts;

public sealed record RespondToWaitListOfferRequest
(
    [property: Required] string ClubNumber,
    bool Accept,
    [property: Required] string SourceChannel,
    [property: Required] string IdempotencyKey
);
