using CabinReservation.Persistence.Enums;

namespace CabinReservation.Api.Contracts;

public sealed record WaitListResponse
(
    Guid Id,
    DateOnly CabinDate,
    string ClubNumber,
    WaitListStatus Status,
    DateTimeOffset RequestedUtc,
    DateTimeOffset? OfferExpiresUtc
);
