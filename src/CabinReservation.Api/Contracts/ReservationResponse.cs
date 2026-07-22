using CabinReservation.Persistence.Enums;

namespace CabinReservation.Api.Contracts;

public sealed record ReservationResponse
(
    Guid Id,
    DateOnly CabinDate,
    string ClubNumber,
    string MemberName,
    ReservationStatus Status,
    string SourceChannel,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? CancelledUtc
);
