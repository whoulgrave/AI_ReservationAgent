using System.ComponentModel.DataAnnotations;

namespace CabinReservation.Api.Options;

public sealed class ReservationPolicyOptions
{
    public const string SectionName = "ReservationPolicy";

    [Required]
    public string CabinTimeZoneId { get; init; } = "America/New_York";

    [Range(1, 30)]
    public int MaximumActiveNightsPerMember { get; init; } = 2;

    [Required]
    public TimeOnly CancellationCutoffLocalTime { get; init; } = new(12, 0);

    [Range(0, 30)]
    public int CancellationDaysBeforeStay { get; init; } = 1;

    [Range(5, 10080)]
    public int WaitListOfferMinutes { get; init; } = 240;

    [Range(10, 3600)]
    public int WorkerIntervalSeconds { get; init; } = 60;
}
