using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalAPI.Web.Services;
using RentalAPI.Web.ViewModels;

namespace RentalAPI.Web.Controllers;

[Authorize]
public class WishlistController(IWishlistService wishlistService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await wishlistService.GetAllAsync();
        return View(new WishlistViewModel { Items = items });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid propertyId)
    {
        await wishlistService.RemoveAsync(propertyId);
        return RedirectToAction(nameof(Index));
    }
}
