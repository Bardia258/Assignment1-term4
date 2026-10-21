using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MunicipalElections.Web.Areas.Admin.Models;
using MunicipalElections.Web.Authorization;
using MunicipalElections.Web.Models;

namespace MunicipalElections.Web.Areas.Admin.Controllers;

[Area("Admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountController(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    AdminAccessService accessService) : Controller
{
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (await accessService.GetAccessAsync(User) is not null)
            return LocalRedirect(SafeReturnUrl(returnUrl));
        return View(new AdminLoginViewModel { ReturnUrl = SafeReturnUrl(returnUrl) });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await users.FindByNameAsync(model.Username);
            if (user is not null && await accessService.GetAccessForUserAsync(user.Id) is not null)
            {
                var result = await signIn.PasswordSignInAsync(user, model.Password,
                    isPersistent: false, lockoutOnFailure: true);
                if (result.Succeeded) return LocalRedirect(SafeReturnUrl(model.ReturnUrl));
                if (result.RequiresTwoFactor)
                    return RedirectToPage("/Account/LoginWith2fa", new
                    {
                        area = "Identity", returnUrl = SafeReturnUrl(model.ReturnUrl), rememberMe = false
                    });
                if (result.IsLockedOut)
                    ModelState.AddModelError(string.Empty, "Sign-in is temporarily locked. Please try again later.");
                else ModelState.AddModelError(string.Empty, "Invalid administrator credentials.");
            }
            else ModelState.AddModelError(string.Empty, "Invalid administrator credentials.");
        }

        model.Password = string.Empty;
        ModelState.SetModelValue(nameof(model.Password), null, string.Empty);
        model.ReturnUrl = SafeReturnUrl(model.ReturnUrl);
        ModelState.Remove(nameof(model.ReturnUrl));
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction(nameof(Login), "Account", new { area = "Admin" });
    }

    private string SafeReturnUrl(string? returnUrl) => Url.IsLocalUrl(returnUrl)
        ? returnUrl! : Url.Action("Index", "Home", new { area = "Admin" })!;
}
