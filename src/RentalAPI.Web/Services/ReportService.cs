using Microsoft.AspNetCore.WebUtilities;

namespace RentalAPI.Web.Services;

public class ReportService(HttpClient http) : IReportService
{
    public async Task<(bool Success, byte[]? File, string? FileName)> ExportReservationsAsync(DateTime? from, DateTime? to)
    {
        var query = new Dictionary<string, string?>();
        if (from.HasValue) query["from"] = from.Value.ToString("yyyy-MM-dd");
        if (to.HasValue) query["to"] = to.Value.ToString("yyyy-MM-dd");

        var url = QueryHelpers.AddQueryString("api/reports/reservations/export", query);
        var resp = await http.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return (false, null, null);

        var bytes = await resp.Content.ReadAsByteArrayAsync();
        var fileName = resp.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? $"reservations_{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return (true, bytes, fileName);
    }
}
