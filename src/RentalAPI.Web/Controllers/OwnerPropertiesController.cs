using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalAPI.Web.Models;
using RentalAPI.Web.Services;
using RentalAPI.Web.ViewModels;

namespace RentalAPI.Web.Controllers;

[Authorize(Roles = "Owner,Admin")]
public class OwnerPropertiesController(IPropertyService propertyService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var properties = await propertyService.GetMyPropertiesAsync(User.GetUserId());
        return View(properties);
    }

    public IActionResult Create() => View(new PropertyFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (success, error, _) = await propertyService.CreateAsync(new CreatePropertyRequestDto(
            vm.Title, vm.Description, vm.City, vm.Address, vm.PricePerNight, vm.MaxGuests, vm.Bedrooms, vm.Bathrooms));

        if (!success)
        {
            ModelState.AddModelError("", error ?? "No se pudo crear la propiedad.");
            return View(vm);
        }

        TempData["Success"] = "Propiedad creada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var property = await propertyService.GetAsync(id);
        if (property is null || property.OwnerId != User.GetUserId()) return NotFound();

        return View(new PropertyFormViewModel
        {
            Id = property.Id,
            Title = property.Title,
            Description = property.Description,
            City = property.City,
            Address = property.Address,
            PricePerNight = property.PricePerNight,
            MaxGuests = property.MaxGuests,
            Bedrooms = property.Bedrooms,
            Bathrooms = property.Bathrooms
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PropertyFormViewModel vm)
    {
        if (vm.Id is null || !ModelState.IsValid) return View(vm);

        var (success, error) = await propertyService.UpdateAsync(vm.Id.Value, new CreatePropertyRequestDto(
            vm.Title, vm.Description, vm.City, vm.Address, vm.PricePerNight, vm.MaxGuests, vm.Bedrooms, vm.Bathrooms));

        if (!success)
        {
            ModelState.AddModelError("", error ?? "No se pudo actualizar la propiedad.");
            return View(vm);
        }

        TempData["Success"] = "Propiedad actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await propertyService.DeleteAsync(id);
        TempData["Success"] = "Propiedad eliminada.";
        return RedirectToAction(nameof(Index));
    }
}
