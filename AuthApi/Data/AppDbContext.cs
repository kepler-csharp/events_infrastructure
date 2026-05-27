using ApiGeneral.AuthApi.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApiGeneral.AuthApi.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Venue>            Venues            => Set<Venue>();
    public DbSet<Event>            Events            => Set<Event>();
    public DbSet<Showtime>         Showtimes         => Set<Showtime>();
    public DbSet<Seat>             Seats             => Set<Seat>();
    public DbSet<Order>            Orders            => Set<Order>();
    public DbSet<OrderItem>        OrderItems        => Set<OrderItem>();
    public DbSet<Payment>          Payments          => Set<Payment>();
    public DbSet<Ticket>           Tickets           => Set<Ticket>();
    public DbSet<TicketValidation> TicketValidations => Set<TicketValidation>();
    public DbSet<AuditLog>         AuditLogs         => Set<AuditLog>();
    public DbSet<RefundRequest>    RefundRequests     => Set<RefundRequest>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Venue ──────────────────────────────────────────────────────────
        builder.Entity<Venue>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        // ── Event ──────────────────────────────────────────────────────────
        builder.Entity<Event>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Venue)
             .WithMany(x => x.Events)
             .HasForeignKey(x => x.VenueId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Showtime ───────────────────────────────────────────────────────
        builder.Entity<Showtime>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Event)
             .WithMany(x => x.Showtimes)
             .HasForeignKey(x => x.EventId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Seat ───────────────────────────────────────────────────────────
        builder.Entity<Seat>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.Label);
            e.HasIndex(x => new { x.ShowtimeId, x.Row, x.Number }).IsUnique();
            e.HasOne(x => x.Showtime)
             .WithMany(x => x.Seats)
             .HasForeignKey(x => x.ShowtimeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Order ──────────────────────────────────────────────────────────
        builder.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);
            // No FK to AspNetUsers — just store UserId as string
        });

        // ── OrderItem ──────────────────────────────────────────────────────
        builder.Entity<OrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Order)
             .WithMany(x => x.Items)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Seat)
             .WithOne(x => x.OrderItem)
             .HasForeignKey<OrderItem>(x => x.SeatId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Payment ────────────────────────────────────────────────────────
        builder.Entity<Payment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Order)
             .WithOne(x => x.Payment)
             .HasForeignKey<Payment>(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Ticket ─────────────────────────────────────────────────────────
        builder.Entity<Ticket>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.QRCode).IsUnique();
            e.HasOne(x => x.OrderItem)
             .WithOne(x => x.Ticket)
             .HasForeignKey<Ticket>(x => x.OrderItemId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── TicketValidation ───────────────────────────────────────────────
        builder.Entity<TicketValidation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Ticket)
             .WithMany(x => x.Validations)
             .HasForeignKey(x => x.TicketId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── AuditLog ───────────────────────────────────────────────────────
        builder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        });

        // ── RefundRequest ──────────────────────────────────────────────────
        builder.Entity<RefundRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Order)
             .WithMany(x => x.RefundRequests)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
