using System.Net.Http.Json;
using RentalAPI.Web.Models;

namespace RentalAPI.Web.Services;

public class AuthService(HttpClient http) : IAuthService
{
    public async Task<(bool Success, TokenResponseDto? Token, string? Error)> LoginAsync(LoginRequestDto req)
    {
        var resp = await http.PostAsJsonAsync("api/auth/login", req);
        if (!resp.IsSuccessStatusCode)
            return (false, null, await SafeError(resp));

        var token = await resp.Content.ReadFromJsonAsync<TokenResponseDto>();
        return (true, token, null);
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequestDto req)
    {
        var resp = await http.PostAsJsonAsync("api/auth/register", req);
        if (!resp.IsSuccessStatusCode)
            return (false, await SafeError(resp));
        return (true, null);
    }

    private static async Task<string> SafeError(HttpResponseMessage resp)
    {
        var text = await resp.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(text) ? resp.ReasonPhrase ?? "Error inesperado." : text;
    }
}
