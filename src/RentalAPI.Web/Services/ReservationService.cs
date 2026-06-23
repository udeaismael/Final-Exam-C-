using System.Net.Http.Json;
using RentalAPI.Web.Models;

namespace RentalAPI.Web.Services;

public class ReservationService(HttpClient http) : IReservationService
{
    public async Task<(bool Success, string? Error)> CreateAsync(CreateReservationRequestDto req)
    {
        var resp = await http.PostAsJsonAsync("api/reservations", req);
        if (!resp.IsSuccessStatusCode) return (false, await resp.Content.ReadAsStringAsync());
        return (true, null);
    }

    public async Task<List<ReservationDto>> GetMineAsync()
    {
        var resp = await http.GetAsync("api/reservations");
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<ReservationDto>>() ?? [];
    }
}
