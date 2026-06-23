using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalAPI.Web.Services;
using RentalAPI.Web.ViewModels;

namespace RentalAPI.Web.Controllers;

[Authorize]
public class KycController(IKycService kycService) : Controller
{
    public async Task<IActionResult> Upload()
    {
        var vm = await BuildViewModelAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError("", "Selecciona una imagen de tu cédula.");
            return View(await BuildViewModelAsync());
        }

        await using var stream = file.OpenReadStream();
        var (success, error) = await kycService.UploadAsync(stream, file.FileName, file.ContentType);

        if (!success)
        {
            ModelState.AddModelError("", error ?? "No se pudo procesar el documento.");
            return View(await BuildViewModelAsync());
        }

        TempData["Success"] = "Documento enviado. Procesando verificación...";
        return RedirectToAction(nameof(Upload));
    }

    // Polled vía JS (fetch) para refrescar el estado sin recargar la página.
    [HttpGet]
    public async Task<IActionResult> StatusJson()
    {
        var status = await kycService.GetStatusAsync();
        return Json(new
        {
            status = status?.Status ?? "None",
            processedAt = status?.ProcessedAt,
            rejectionReason = status?.RejectionReason
        });
    }

    private async Task<KycViewModel> BuildViewModelAsync()
    {
        var status = await kycService.GetStatusAsync();
        return new KycViewModel
        {
            Status = status?.Status ?? "None",
            ProcessedAt = status?.ProcessedAt,
            RejectionReason = status?.RejectionReason,
            ConfidenceLevel = status?.Status == "Approved" ? "Alta" : "N/A"
        };
    }
}
