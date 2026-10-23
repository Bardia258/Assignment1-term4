using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MunicipalElections.Web.Authentication;
using MunicipalElections.Web.Models;

namespace MunicipalElections.Web.Controllers.Api;

public sealed class RegisterRequest
{
    [Required, StringLength(100)] public string Username { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 6)] public string Password { get; set; } = string.Empty;
    [StringLength(200)] public string? FullName { get; set; }
}

public sealed class LoginRequest
{
    [Required, StringLength(254)] public string Username { get; set; } = string.Empty;
    [Required, StringLength(128)] public string Password { get; set; } = string.Empty;
}

[ApiController, AllowAnonymous, Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn, JwtTokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Username.Trim(), Email = request.Email.Trim(),
            FullName = request.FullName?.Trim(), LockoutEnabled = true
        };
       
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem(ModelState);
        }
        return StatusCode(StatusCodes.Status201Created, new { id = user.Id, username = user.UserName });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var identifier = request.Username.Trim();
        var user = await users.FindByNameAsync(identifier) ?? await users.FindByEmailAsync(identifier);
        if (user is null) return Unauthorized(new { message = "Invalid credentials." });
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded || await users.GetTwoFactorEnabledAsync(user))
            return Unauthorized(new { message = "Invalid credentials or account unavailable." });
        return Ok(new { token = await tokens.CreateAsync(user) });
    }
}
