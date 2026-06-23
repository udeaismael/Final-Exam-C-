using RentalAPI.Web.Models;

namespace RentalAPI.Web.ViewModels;

public class CreateReservationViewModel
{
    public Guid PropertyId { get; set; }
    public string PropertyTitle { get; set; } = "";
    public decimal PricePerNight { get; set; }
    public DateTime CheckIn { get; set; } = DateTime.Today.AddDays(1);
    public DateTime CheckOut { get; set; } = DateTime.Today.AddDays(2);
}

public class MyReservationsViewModel
{
    public List<ReservationDto> Reservations { get; set; } = [];
}
