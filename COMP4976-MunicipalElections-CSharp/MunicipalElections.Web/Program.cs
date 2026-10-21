using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Data;
using MunicipalElections.Web.Models;
using MunicipalElections.Web.Authorization;
using MunicipalElections.Web.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MunicipalElections.Web.Authentication;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
// Add services to the container.
var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection in appsettings.Development.json locally or through secure environment configuration in Production.");
var connectionString = DatabaseConfiguration.ResolveConnectionString(configuredConnection, builder.Environment.ContentRootPath);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        // Registration is immediate for the assignment; no email delivery service is configured.
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddAdminAuthorization();
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection("Uploads"));
builder.Services.AddScoped<IImageStorage, ImageStorage>();
builder.Services.AddControllersWithViews(options => options.Conventions.Add(new AdminAreaAuthorizationConvention()));
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/Admin"))
        {
            var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
            context.Response.Redirect(context.Request.PathBase + "/Admin/Account/Login"
                + QueryString.Create("returnUrl", returnUrl));
        }
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/Admin"))
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

var jwt = JwtSettings.Load(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(jwt);
builder.Services.AddScoped<JwtTokenService>();
// Keep Identity's cookie defaults; API authorization explicitly selects Bearer.
builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = jwt.Key,
        ValidateLifetime = true, RequireExpirationTime = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ClockSkew = TimeSpan.FromSeconds(30), NameClaimType = JwtRegisteredClaimNames.Sub
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var id = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var user = id is null ? null : await users.FindByIdAsync(id);
            if (user is null || await users.IsLockedOutAsync(user)
                || context.Principal?.FindFirst("security_stamp")?.Value != await users.GetSecurityStampAsync(user))
                context.Fail("The account or token is no longer valid.");
        }
    };
});

builder.Services.AddScoped<IMunicipalityService, MunicipalityService>();
builder.Services.AddScoped<ICandidateService, CandidateService>();

var app = builder.Build();

// Explicit offline migration command for deployment. Never run automatically in Production.
if (args.Contains("--migrate-database"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await DatabaseMigration.MigrateAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DatabaseMigration.MigrateAsync(db);
    await DbSeeder.SeedAsync(db);
    await IdentitySeeder.SeedAsync(scope.ServiceProvider, app.Environment);
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
var uploadRoot = UploadOptions.ResolveRoot(builder.Configuration["Uploads:RootPath"] ?? "App_Data/uploads", app.Environment.ContentRootPath);
Directory.CreateDirectory(uploadRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadRoot), RequestPath = "/uploads",
    OnPrepareResponse = context => context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff"
});
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program { }
