namespace RentalAPI.Domain.Entities;

public class KycValidation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending | Approved | Rejected
    public string? ExtractedFirstName { get; set; }
    public string? ExtractedLastName { get; set; }
    public string? ExtractedDocumentNumber { get; set; }
    public DateTime? ExtractedBirthDate { get; set; }
    public string? RejectionReason { get; set; }
    public string? TempImagePath { get; set; } // deleted after processing
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    public User User { get; set; } = null!;
}
