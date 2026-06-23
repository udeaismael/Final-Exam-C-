using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalAPI.Web.Services;

namespace RentalAPI.Web.Controllers;

[Authorize(Roles = "Admin")]
public class ReportsController(IReportService reportService) : Controller
{
    public IActionResult Index() => View();

    public async Task<IActionResult> ExportReservations(DateTime? from, DateTime? to)
    {
        var (success, file, fileName) = await reportService.ExportReservationsAsync(from, to);
        if (!success || file is null)
        {
            TempData["Error"] = "No se pudo generar el reporte.";
            return RedirectToAction(nameof(Index));
        }

        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
