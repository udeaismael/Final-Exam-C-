using RentalAPI.Web.ViewModels;

namespace RentalAPI.Web.Services;

public class DashboardService(IPropertyService properties) : IDashboardService
{
    public async Task<DashboardViewModel> GetOwnerDashboardAsync(Guid ownerId)
    {
        var myProperties = await properties.GetMyPropertiesAsync(ownerId);

        var vm = new DashboardViewModel
        {
            TotalProperties = myProperties.Count,
            AveragePricePerNight = myProperties.Count > 0 ? myProperties.Average(p => p.PricePerNight) : 0,
            // Reservas/Ingresos/Ocupación requieren un endpoint agregado por propietario que la API
            // actual no expone (ReservationsController solo retorna reservas del usuario autenticado
            // como huésped). Se deja en 0 y queda listo para conectar cuando exista.
            TotalReservations = 0,
            TotalIncome = 0,
            OccupancyRate = 0,
            PropertyLabels = myProperties.Select(p => p.Title).ToList(),
            PropertyPrices = myProperties.Select(p => p.PricePerNight).ToList(),
            PropertiesByCity = myProperties.GroupBy(p => p.City).ToDictionary(g => g.Key, g => g.Count())
        };

        return vm;
    }
}
