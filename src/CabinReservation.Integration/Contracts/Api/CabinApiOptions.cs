namespace CabinReservation.Integration.Contracts.Api;

public sealed class CabinApiOptions
{
    public const string SectionName = "CabinApi";
    public required Uri BaseAddress { get; init; }
    public required string ApiKey { get; init; }
}
