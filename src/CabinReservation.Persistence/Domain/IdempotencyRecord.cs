namespace CabinReservation.Persistence.Domain;

public sealed class IdempotencyRecord
{
    public required string Key { get; set; }
    public required string Operation { get; set; }
    public required string ResponseJson { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}
