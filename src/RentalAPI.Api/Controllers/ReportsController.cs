using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalAPI.Infrastructure.Data;

namespace RentalAPI.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController(AppDbContext db) : ControllerBase
{
    // ── GET /api/reports/reservations/export ─────────────────────────────────
    [HttpGet("reservations/export")]
    public async Task<IActionResult> ExportReservations(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var q = db.Reservations
            .Include(r => r.User)
            .Include(r => r.Property)
            .AsQueryable();

        if (from.HasValue) q = q.Where(r => r.CreatedAt >= from.Value);
        if (to.HasValue)   q = q.Where(r => r.CreatedAt <= to.Value);

        var data = await q.OrderByDescending(r => r.CreatedAt).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Reservations");

        // Header row
        string[] headers = ["Id", "User", "Property", "City", "CheckIn", "CheckOut", "TotalPrice", "Status", "CreatedAt"];
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        // Data rows
        for (int row = 0; row < data.Count; row++)
        {
            var r = data[row];
            ws.Cell(row + 2, 1).Value = r.Id.ToString();
            ws.Cell(row + 2, 2).Value = $"{r.User.FirstName} {r.User.LastName}";
            ws.Cell(row + 2, 3).Value = r.Property.Title;
            ws.Cell(row + 2, 4).Value = r.Property.City;
            ws.Cell(row + 2, 5).Value = r.CheckIn.ToString("yyyy-MM-dd HH:mm");
            ws.Cell(row + 2, 6).Value = r.CheckOut.ToString("yyyy-MM-dd HH:mm");
            ws.Cell(row + 2, 7).Value = (double)r.TotalPrice;
            ws.Cell(row + 2, 8).Value = r.Status;
            ws.Cell(row + 2, 9).Value = r.CreatedAt.ToString("yyyy-MM-dd");
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"reservations_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }
}
