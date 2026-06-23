using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalAPI.Web.Models;
using RentalAPI.Web.Services;
using RentalAPI.Web.ViewModels;

namespace RentalAPI.Web.Controllers;

public class AccountController(IAuthService authService) : Controller
{
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (success, token, error) = await authService.LoginAsync(new LoginRequestDto(vm.Email, vm.Password));
        if (!success || token is null)
        {
            ModelState.AddModelError("", error ?? "Credenciales inválidas.");
            return View(vm);
        }

        await SignInWithTokenAsync(token);

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return Redirect(vm.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (success, error) = await authService.RegisterAsync(
            new RegisterRequestDto(vm.Email, vm.Password, vm.FirstName, vm.LastName, vm.Role));

        if (!success)
        {
            ModelState.AddModelError("", error ?? "No se pudo registrar el usuario.");
            return View(vm);
        }

        TempData["Success"] = "Cuenta creada. Ahora inicia sesión.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    private async Task SignInWithTokenAsync(TokenResponseDto token)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);
        var identity = new ClaimsIdentity(jwt.Claims, CookieAuthenticationDefaults.AuthenticationScheme,
            nameType: ClaimTypes.Email, roleType: ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);

        var props = new AuthenticationProperties { IsPersistent = true };
        props.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token", Value = token.AccessToken },
            new AuthenticationToken { Name = "refresh_token", Value = token.RefreshToken }
        });

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
    }
}
