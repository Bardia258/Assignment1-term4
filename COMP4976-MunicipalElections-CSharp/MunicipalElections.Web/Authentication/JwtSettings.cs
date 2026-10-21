using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MunicipalElections.Web.Models;

namespace MunicipalElections.Web.Authentication;

public sealed class JwtSettings
{
    public string Issuer { get; set; } = "MunicipalElections";
    public string Audience { get; set; } = "MunicipalElections.iOS";
    public string SigningKey { get; set; } = string.Empty;
    public int LifetimeMinutes { get; set; } = 60;

    public static JwtSettings Load(IConfiguration configuration, IHostEnvironment environment)
    {
        var settings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new();
        // Development tokens intentionally expire on restart unless a local secret is configured.
        if (string.IsNullOrWhiteSpace(settings.SigningKey) && environment.IsDevelopment())
            settings.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        if (Encoding.UTF8.GetByteCount(settings.SigningKey) < 32
            || string.IsNullOrWhiteSpace(settings.Issuer) || string.IsNullOrWhiteSpace(settings.Audience)
            || settings.LifetimeMinutes is < 1 or > 1440)
            throw new InvalidOperationException("Configure Jwt:SigningKey (at least 32 UTF-8 bytes), Issuer, Audience and LifetimeMinutes (1-1440). Use user secrets or environment settings for the signing key.");
        return settings;
    }

    public SymmetricSecurityKey Key => new(Encoding.UTF8.GetBytes(SigningKey));
}

public sealed class JwtTokenService(JwtSettings settings, UserManager<ApplicationUser> users)
{
    public async Task<string> CreateAsync(ApplicationUser user)
    {
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim("security_stamp", await users.GetSecurityStampAsync(user))
        };
        // API tokens have no administrative role claims and do not authenticate MVC requests.
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
            notBefore: now, expires: now.AddMinutes(settings.LifetimeMinutes),
            signingCredentials: new SigningCredentials(settings.Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
