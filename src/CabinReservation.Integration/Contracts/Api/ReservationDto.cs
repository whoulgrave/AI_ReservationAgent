namespace CabinReservation.Integration.Contracts.Api;

public sealed record ReservationDto(Guid Id, DateOnly CabinDate, string ClubNumber, string MemberName,
    string Status, string SourceChannel, DateTimeOffset CreatedUtc, DateTimeOffset? CancelledUtc);
