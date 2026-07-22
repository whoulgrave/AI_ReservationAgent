using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite; // <-- Add this if using SQLite
using CabinReservation.Persistence.Domain;
using CabinReservation.Persistence.Enums;
using CabinReservation.Persistence.Context;

namespace CabinReservation.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CabinDbContext>();

        Directory.CreateDirectory("data");
        await ((DbContext)db).Database.EnsureCreatedAsync();

        // Use the correct extension method for executing raw SQL
        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS UX_Reservations_ConfirmedCabinDate
            ON Reservations (CabinDate)
            WHERE Status = 1;
        """);

        if (!await db.Members.AnyAsync())
        {
            var now = DateTimeOffset.UtcNow;
            db.Members.Add(new Member
            {
                ClubNumber = "001",
                FullName = "Initial Administrator",
                EmailAddress = "admin@example.org",
                PreferredChannel = CommunicationChannel.Email,
                PreferredChannelVerified = false,
                IsActive = true,
                CanViewReports = true,
                CanViewAuditLog = true,
                CanUploadRoster = true,
                CreatedUtc = now,
                UpdatedUtc = now
            });
            await db.SaveChangesAsync();
        }
    }
}
