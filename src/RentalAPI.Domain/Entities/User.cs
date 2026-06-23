namespace RentalAPI.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Role { get; set; } = "User"; // User | Owner | Admin
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<Property> Properties { get; set; } = [];
    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<WishlistItem> WishlistItems { get; set; } = [];
    public KycValidation? KycValidation { get; set; }
}
