using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalAPI.Web.Services;
using RentalAPI.Web.ViewModels;

namespace RentalAPI.Web.Controllers;

public class PropertiesController(IPropertyService propertyService, IWishlistService wishlistService) : Controller
{
    public async Task<IActionResult> Index(string? city, DateTime? checkIn, DateTime? checkOut)
    {
        var results = await propertyService.SearchAsync(city, checkIn, checkOut);
        var vm = new PropertySearchViewModel { City = city, CheckIn = checkIn, CheckOut = checkOut, Results = results };
        return View(vm);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var property = await propertyService.GetAsync(id);
        if (property is null) return NotFound();
        return View(property);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleWishlist(Guid propertyId, bool isInWishlist)
    {
        if (isInWishlist) await wishlistService.RemoveAsync(propertyId);
        else await wishlistService.AddAsync(propertyId);
        return RedirectToAction(nameof(Details), new { id = propertyId });
    }
}
