namespace RentalAPI.Web.ViewModels;

public class DashboardViewModel
{
    public int TotalProperties { get; set; }
    public decimal AveragePricePerNight { get; set; }
    public int TotalReservations { get; set; }   // No expuesto por la API actual (placeholder 0)
    public decimal TotalIncome { get; set; }      // No expuesto por la API actual (placeholder 0)
    public double OccupancyRate { get; set; }     // No expuesto por la API actual (placeholder 0)
    public bool HasLimitedData { get; set; } = true;

    public List<string> PropertyLabels { get; set; } = [];
    public List<decimal> PropertyPrices { get; set; } = [];
    public Dictionary<string, int> PropertiesByCity { get; set; } = [];
}
