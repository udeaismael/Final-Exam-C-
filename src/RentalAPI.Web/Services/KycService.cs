using System.Net.Http.Json;
using RentalAPI.Web.Models;

namespace RentalAPI.Web.Services;

public class KycService(HttpClient http) : IKycService
{
    public async Task<(bool Success, string? Error)> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var resp = await http.PostAsync("api/kyc/upload", content);
        if (resp.StatusCode is not (System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Accepted))
            return (false, await resp.Content.ReadAsStringAsync());
        return (true, null);
    }

    public async Task<KycStatusDto?> GetStatusAsync()
    {
        var resp = await http.GetAsync("api/kyc/status");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<KycStatusDto>();
    }
}
