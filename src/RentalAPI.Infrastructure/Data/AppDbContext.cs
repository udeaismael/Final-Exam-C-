using Microsoft.EntityFrameworkCore;
using RentalAPI.Domain.Entities;

namespace RentalAPI.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<KycValidation> KycValidations => Set<KycValidation>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        // ── User ──────────────────────────────────────────────────────────────
        m.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
        });

        // ── Property ──────────────────────────────────────────────────────────
        m.Entity<Property>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.City).HasMaxLength(100).IsRequired();
            e.Property(x => x.PricePerNight).HasPrecision(12, 2);
            e.HasOne(x => x.Owner).WithMany(u => u.Properties)
             .HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);

            // indexes for search
            e.HasIndex(x => x.City);
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => new { x.City, x.IsActive });
        });

        // ── Reservation ───────────────────────────────────────────────────────
        m.Entity<Reservation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TotalPrice).HasPrecision(12, 2);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.User).WithMany(u => u.Reservations)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Property).WithMany(p => p.Reservations)
             .HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);

            // overlap check + listing
            e.HasIndex(x => x.PropertyId);
            e.HasIndex(x => new { x.PropertyId, x.CheckIn, x.CheckOut });
            e.HasIndex(x => x.UserId);
        });

        // ── WishlistItem ──────────────────────────────────────────────────────
        m.Entity<WishlistItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User).WithMany(u => u.WishlistItems)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Property).WithMany(p => p.WishlistItems)
             .HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.UserId, x.PropertyId }).IsUnique();
        });

        // ── KycValidation ─────────────────────────────────────────────────────
        m.Entity<KycValidation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.User).WithOne(u => u.KycValidation)
             .HasForeignKey<KycValidation>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.UserId).IsUnique();
            e.HasIndex(x => x.Status);
        });

        // ── Notification ──────────────────────────────────────────────────────
        m.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(20);
            e.Property(x => x.Subject).HasMaxLength(300);
            e.HasIndex(x => new { x.UserId, x.IsSent });
        });
    }
}
