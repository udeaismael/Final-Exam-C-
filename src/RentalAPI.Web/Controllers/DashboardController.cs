using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalAPI.Web.Services;

namespace RentalAPI.Web.Controllers;

[Authorize(Roles = "Owner,Admin")]
public class DashboardController(IDashboardService dashboardService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vm = await dashboardService.GetOwnerDashboardAsync(User.GetUserId());
        return View(vm);
    }
}
