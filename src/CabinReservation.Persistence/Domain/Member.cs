using CabinReservation.Persistence.Enums;

namespace CabinReservation.Persistence.Domain;

public sealed class Member
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ClubNumber { get; set; }
    public required string FullName { get; set; }
    public string? EmailAddress { get; set; }
    public string? MobileNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public CommunicationChannel? PreferredChannel { get; set; }
    public bool PreferredChannelVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public bool CanViewReports { get; set; }
    public bool CanViewAuditLog { get; set; }
    public bool CanUploadRoster { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<WaitListEntry> WaitListEntries { get; set; } = [];
}
