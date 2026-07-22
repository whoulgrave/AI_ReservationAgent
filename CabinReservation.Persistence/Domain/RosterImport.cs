namespace CabinReservation.Persistence.Domain;

public sealed class RosterImport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string FileName { get; set; }
    public required string Sha256 { get; set; }
    public required string UploadedBy { get; set; }
    public DateTimeOffset UploadedUtc { get; set; }
    public DateTimeOffset? AppliedUtc { get; set; }
    public string? SummaryJson { get; set; }
}
