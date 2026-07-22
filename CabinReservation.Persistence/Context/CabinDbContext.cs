using CabinReservation.Persistence.Domain;
using Microsoft.EntityFrameworkCore;

namespace CabinReservation.Persistence.Context;

public sealed class CabinDbContext(DbContextOptions<CabinDbContext> options) : DbContext(options)
{
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<WaitListEntry> WaitListEntries => Set<WaitListEntry>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<OutboundMessage> OutboundMessages => Set<OutboundMessage>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<RosterImport> RosterImports => Set<RosterImport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ClubNumber).HasMaxLength(32);
            entity.Property(x => x.FullName).HasMaxLength(200);
            entity.Property(x => x.EmailAddress).HasMaxLength(320);
            entity.Property(x => x.MobileNumber).HasMaxLength(32);
            entity.Property(x => x.PhoneNumber).HasMaxLength(32);
            entity.HasIndex(x => x.ClubNumber).IsUnique();
            entity.HasIndex(x => x.EmailAddress);
            entity.HasIndex(x => x.MobileNumber);
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CabinDate).HasConversion<string>();
            entity.Property(x => x.SourceChannel).HasMaxLength(32);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.HasIndex(x => new { x.CabinDate, x.Status });
            entity.HasIndex(x => new { x.MemberId, x.CabinDate });
            entity.HasOne(x => x.Member)
                .WithMany(x => x.Reservations)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WaitListEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CabinDate).HasConversion<string>();
            entity.Property(x => x.SourceChannel).HasMaxLength(32);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.HasIndex(x => new { x.CabinDate, x.Status, x.RequestedUtc });
            entity.HasOne(x => x.Member)
                .WithMany(x => x.WaitListEntries)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(100);
            entity.Property(x => x.EntityType).HasMaxLength(100);
            entity.Property(x => x.SourceChannel).HasMaxLength(32);
            entity.Property(x => x.CorrelationId).HasMaxLength(100);
            entity.Property(x => x.Outcome).HasMaxLength(32);
            entity.HasIndex(x => x.OccurredUtc);
            entity.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<OutboundMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MessageType).HasMaxLength(100);
            entity.HasIndex(x => new { x.Status, x.NextAttemptUtc });
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasMaxLength(200);
            entity.Property(x => x.Operation).HasMaxLength(100);
        });

        modelBuilder.Entity<RosterImport>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).HasMaxLength(255);
            entity.Property(x => x.Sha256).HasMaxLength(64);
            entity.Property(x => x.UploadedBy).HasMaxLength(200);
        });
    }
}
