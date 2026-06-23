using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalAPI.Web.Models;
using RentalAPI.Web.Services;
using RentalAPI.Web.ViewModels;

namespace RentalAPI.Web.Controllers;

[Authorize]
public class ReservationsController(
    IReservationService reservationService,
    IPropertyService propertyService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var list = await reservationService.GetMineAsync();
        return View(new MyReservationsViewModel { Reservations = list });
    }

    public async Task<IActionResult> Create(Guid propertyId)
    {
        var property = await propertyService.GetAsync(propertyId);
        if (property is null) return NotFound();

        return View(new CreateReservationViewModel
        {
            PropertyId = property.Id,
            PropertyTitle = property.Title,
            PricePerNight = property.PricePerNight
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReservationViewModel vm)
    {
        if (vm.CheckOut.Date <= vm.CheckIn.Date)
        {
            ModelState.AddModelError("", "La fecha de salida debe ser posterior a la de llegada.");
            return View(vm);
        }

        var (success, error) = await reservationService.CreateAsync(
            new CreateReservationRequestDto(vm.PropertyId, vm.CheckIn, vm.CheckOut));

        if (!success)
        {
            ModelState.AddModelError("", error ?? "No se pudo crear la reserva.");
            return View(vm);
        }

        TempData["Success"] = "Reserva confirmada. Check-in 14:00 · Check-out 12:00.";
        return RedirectToAction(nameof(Index));
    }
}
