namespace RentalAPI.Domain.Entities;

public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid PropertyId { get; set; }
    public DateTime CheckIn { get; set; }   // stored as date; time forced to 14:00 UTC
    public DateTime CheckOut { get; set; }  // stored as date; time forced to 12:00 UTC
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Pending"; // Pending | Confirmed | Cancelled
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Property Property { get; set; } = null!;
}
