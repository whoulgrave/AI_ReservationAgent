namespace CabinReservation.Integration.Contracts.Api;

public sealed record CalendarDayDto(DateOnly Date, bool IsAvailable, string Status);
