using RentalAPI.Web.Models;

namespace RentalAPI.Web.ViewModels;

public class WishlistViewModel
{
    public List<WishlistItemDto> Items { get; set; } = [];
}

public class KycViewModel
{
    public string Status { get; set; } = "None"; // None | Pending | Approved | Rejected
    public DateTime? ProcessedAt { get; set; }
    public string? RejectionReason { get; set; }
    // La API no expone score numérico; se deriva un nivel cualitativo a partir del Status.
    public string ConfidenceLevel { get; set; } = "N/A"; // Alta | N/A
}
