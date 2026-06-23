using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalAPI.Web.Services;
using RentalAPI.Web.ViewModels;

namespace RentalAPI.Web.Controllers;

public class HomeController(IPropertyService propertyService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var results = await propertyService.SearchAsync(null, null, null);
        var vm = new PropertySearchViewModel { Results = results.Take(6).ToList() };
        return View(vm);
    }

    public IActionResult Error() => View();
}
