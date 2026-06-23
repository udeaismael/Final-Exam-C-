using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using RentalAPI.Web.Models;

namespace RentalAPI.Web.Services;

public class PropertyService(HttpClient http) : IPropertyService
{
    public async Task<List<PropertySearchItemDto>> SearchAsync(string? city, DateTime? checkIn, DateTime? checkOut)
    {
        var query = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(city)) query["city"] = city;
        if (checkIn.HasValue) query["checkIn"] = checkIn.Value.ToString("yyyy-MM-dd");
        if (checkOut.HasValue) query["checkOut"] = checkOut.Value.ToString("yyyy-MM-dd");

        var url = QueryHelpers.AddQueryString("api/properties", query);
        var resp = await http.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<PropertySearchItemDto>>() ?? [];
    }

    public async Task<PropertyDto?> GetAsync(Guid id)
    {
        var resp = await http.GetAsync($"api/properties/{id}");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<PropertyDto>();
    }

    // La API no expone un filtro "mis propiedades" (GET /api/properties no devuelve OwnerId).
    // Se obtiene el listado público y se completa cada item con el detalle para filtrar por OwnerId.
    public async Task<List<PropertyDto>> GetMyPropertiesAsync(Guid ownerId)
    {
        var all = await SearchAsync(null, null, null);
        var details = await Task.WhenAll(all.Select(p => GetAsync(p.Id)));
        return details.Where(p => p is not null && p!.OwnerId == ownerId).Select(p => p!).ToList();
    }

    public async Task<(bool Success, string? Error, PropertyDto? Created)> CreateAsync(CreatePropertyRequestDto req)
    {
        var resp = await http.PostAsJsonAsync("api/properties", req);
        if (!resp.IsSuccessStatusCode) return (false, await resp.Content.ReadAsStringAsync(), null);
        var created = await resp.Content.ReadFromJsonAsync<PropertyDto>();
        return (true, null, created);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Guid id, CreatePropertyRequestDto req)
    {
        var resp = await http.PutAsJsonAsync($"api/properties/{id}", req);
        if (!resp.IsSuccessStatusCode) return (false, await resp.Content.ReadAsStringAsync());
        return (true, null);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var resp = await http.DeleteAsync($"api/properties/{id}");
        return resp.IsSuccessStatusCode;
    }
}
