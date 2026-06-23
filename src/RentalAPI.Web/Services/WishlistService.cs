using System.Net.Http.Json;
using RentalAPI.Web.Models;

namespace RentalAPI.Web.Services;

public class WishlistService(HttpClient http) : IWishlistService
{
    public async Task<List<WishlistItemDto>> GetAllAsync()
    {
        var resp = await http.GetAsync("api/wishlist");
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<WishlistItemDto>>() ?? [];
    }

    public async Task<bool> AddAsync(Guid propertyId)
    {
        var resp = await http.PostAsync($"api/wishlist/{propertyId}", null);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveAsync(Guid propertyId)
    {
        var resp = await http.DeleteAsync($"api/wishlist/{propertyId}");
        return resp.IsSuccessStatusCode;
    }
}
