using ApiGeneral.AuthApi.Data;
using ApiGeneral.AuthApi.Entities;
using ApiGeneral.AuthApi.Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApiGeneral.AuthApi.Seed;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider services)
    {
        var db          = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        // ── Roles ──────────────────────────────────────────────────────────
        string[] roles = ["Admin", "Customer", "Scanner", "Receptionist"];
        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // ── Users ──────────────────────────────────────────────────────────
        await EnsureUser(userManager, "admin@tickets.com",        "Admin1234!",    "Admin",        "Admin",   "User");
        await EnsureUser(userManager, "customer@tickets.com",     "Customer1234!", "Customer",     "John",    "Doe");
        await EnsureUser(userManager, "scanner@tickets.com",      "Scanner1234!",  "Scanner",      "Scanner", "Device");
        await EnsureUser(userManager, "receptionist@tickets.com", "Recept1234!",   "Receptionist", "Maria",   "Lopez");

        // ── Domain seed ────────────────────────────────────────────────────
        if (!await db.Venues.AnyAsync())
        {
            var venue1 = new Venue { Name = "Cinépolis Premium", Address = "Av. El Poblado 1234", City = "Medellín", Capacity = 500 };
            var venue2 = new Venue { Name = "Teatro Metropolitano", Address = "Calle 41 # 57-30", City = "Medellín", Capacity = 1800 };
            db.Venues.AddRange(venue1, venue2);
            await db.SaveChangesAsync();

            var event1 = new Event
            {
                Name = "Inception", Description = "A mind-bending sci-fi thriller.",
                VenueId = venue1.Id, Type = EventType.Movie, DurationMinutes = 148
            };
            var event2 = new Event
            {
                Name = "Rock en Vivo 2025", Description = "El mejor festival de rock de Colombia.",
                VenueId = venue2.Id, Type = EventType.Concert, DurationMinutes = 240
            };
            db.Events.AddRange(event1, event2);
            await db.SaveChangesAsync();

            // Showtime 1: cinema
            var showtime1 = new Showtime
            {
                EventId   = event1.Id,
                StartTime = DateTime.UtcNow.Date.AddDays(1).AddHours(19),
                EndTime   = DateTime.UtcNow.Date.AddDays(1).AddHours(21).AddMinutes(28),
                BasePrice = 22000m,
                Status    = ShowtimeStatus.Active
            };
            foreach (var row in new[] { "A", "B", "C", "D", "E", "F" })
                for (int n = 1; n <= 10; n++)
                    showtime1.Seats.Add(new Seat
                    {
                        Row = row, Number = n,
                        Type = row is "E" or "F" ? SeatType.Premium : SeatType.Standard
                    });

            // Showtime 2: concert
            var showtime2 = new Showtime
            {
                EventId   = event2.Id,
                StartTime = DateTime.UtcNow.Date.AddDays(7).AddHours(18),
                EndTime   = DateTime.UtcNow.Date.AddDays(7).AddHours(22),
                BasePrice = 85000m,
                Status    = ShowtimeStatus.Active
            };
            foreach (var row in new[] { "1", "2", "3", "4", "5" })
                for (int n = 1; n <= 20; n++)
                    showtime2.Seats.Add(new Seat
                    {
                        Row = row, Number = n,
                        Type = row == "1" ? SeatType.VIP : SeatType.Standard
                    });

            db.Showtimes.AddRange(showtime1, showtime2);
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureUser(
        UserManager<ApplicationUser> mgr,
        string email, string password, string role,
        string firstName, string lastName)
    {
        if (await mgr.FindByEmailAsync(email) != null) return;

        var user = new ApplicationUser
        {
            UserName  = role,
            FullName =  firstName + " " + lastName,
            Email     = email,
            EmailConfirmed = true
        };

        var result = await mgr.CreateAsync(user, password);
        if (result.Succeeded)
            await mgr.AddToRoleAsync(user, role);
    }
}
