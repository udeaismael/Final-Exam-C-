namespace RentalAPI.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Type { get; set; } = null!;   // Email | Push | SMS
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public bool IsSent { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}
