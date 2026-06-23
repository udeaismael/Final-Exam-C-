namespace RentalAPI.Web.Models;

// ── Auth ──────────────────────────────────────────────────────────────────
public record RegisterRequestDto(string Email, string Password, string FirstName, string LastName, string? Role);
public record LoginRequestDto(string Email, string Password);
public record TokenResponseDto(string AccessToken, string RefreshToken);

// ── Properties ────────────────────────────────────────────────────────────
public record PropertySearchItemDto(Guid Id, string Title, string City, decimal PricePerNight, int Bedrooms, int Bathrooms, int MaxGuests);

public class PropertyDto
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string City { get; set; } = "";
    public string Address { get; set; } = "";
    public decimal PricePerNight { get; set; }
    public int MaxGuests { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record CreatePropertyRequestDto(
    string Title, string Description, string City, string Address,
    decimal PricePerNight, int MaxGuests, int Bedrooms, int Bathrooms);

// ── Reservations ──────────────────────────────────────────────────────────
public record CreateReservationRequestDto(Guid PropertyId, DateTime CheckIn, DateTime CheckOut);

public class ReservationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PropertyId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public PropertyDto? Property { get; set; }
}

// ── Wishlist ──────────────────────────────────────────────────────────────
public class WishlistItemDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Title { get; set; } = "";
    public string City { get; set; } = "";
    public decimal PricePerNight { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── KYC ───────────────────────────────────────────────────────────────────
public record KycUploadResponseDto(Guid Id, string Status, string Message);
public class KycStatusDto
{
    public string Status { get; set; } = "Pending"; // Pending | Approved | Rejected
    public DateTime? ProcessedAt { get; set; }
    public string? RejectionReason { get; set; }
}
