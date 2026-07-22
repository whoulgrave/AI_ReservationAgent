namespace CabinReservation.Api.Contracts;

public sealed record CalendarDayResponse
(
    DateOnly Date,
    bool IsAvailable,
    string Status
);
