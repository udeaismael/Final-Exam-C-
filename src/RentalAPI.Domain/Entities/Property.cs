using System.Text.Json.Serialization;

namespace RentalAPI.Domain.Entities;

public class Property
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Address { get; set; } = null!;

    public decimal PricePerNight { get; set; }
    public int MaxGuests { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Owner { get; set; } = null!;

    [JsonIgnore]
    public ICollection<Reservation> Reservations { get; set; } = [];

    [JsonIgnore]
    public ICollection<WishlistItem> WishlistItems { get; set; } = [];
}