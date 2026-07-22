namespace CabinReservation.Api.Contracts;

public sealed record RosterPreviewResponse
(
    int Added,
    int Updated,
    int Deactivated,
    int ReservationsToCancel,
    IReadOnlyList<string> AffectedClubNumbers
);
