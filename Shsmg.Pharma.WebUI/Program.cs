using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Shsmg.Pharma.Application;
using Shsmg.Pharma.WebUI.Components;
using Shsmg.Pharma.Infra;
using Shsmg.Pharma.Infra.Auth;
using Shsmg.Pharma.Application.Common;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var culture = new CultureInfo("en-IN");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter("LuckyPennySoftware.MediatR.License", LogLevel.None);

builder.Services.AddApplication(cfg =>
{
    cfg.LicenseKey = builder.Configuration["MediatR:LicenseKey"];
});
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.Cookie.Name = "ShsmgPharmaAuth";
    });

builder.Services.AddAuthorization(options =>
{
    // Company permissions
    options.AddPolicy("Company.View", policy => policy.RequireClaim("Permission", Permissions.CompanyView));
    options.AddPolicy("Company.Edit", policy => policy.RequireClaim("Permission", Permissions.CompanyEdit));

    // Invoice permissions
    options.AddPolicy("Invoice.View", policy => policy.RequireClaim("Permission", Permissions.InvoiceView));
    options.AddPolicy("Invoice.Create", policy => policy.RequireClaim("Permission", Permissions.InvoiceCreate));
    options.AddPolicy("Invoice.Edit", policy => policy.RequireClaim("Permission", Permissions.InvoiceEdit));
    options.AddPolicy("Invoice.Delete", policy => policy.RequireClaim("Permission", Permissions.InvoiceDelete));

    // User management
    options.AddPolicy("User.Manage", policy => policy.RequireClaim("Permission", Permissions.UserManage));
});

builder.Services.AddScoped<Shsmg.Pharma.WebUI.Services.PermissionService>();

var app = builder.Build();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = [culture],
    SupportedUICultures = [culture]
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await SeedDefaultUserAsync(app.Services);
app.Run();

static async Task SeedDefaultUserAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Seed roles
    foreach (var roleName in Roles.RolePermissions.Keys)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var role = new IdentityRole(roleName);
            await roleManager.CreateAsync(role);

            // Add claims for permissions
            var permissions = Roles.RolePermissions[roleName];
            foreach (var permission in permissions)
            {
                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", permission));
            }
        }
    }

    const string defaultEmail = "admin@pharma.local";
    const string defaultPassword = "Admin@1234";

    if (await userManager.FindByEmailAsync(defaultEmail) is null)
    {
        var user = new AppUser
        {
            UserName = defaultEmail,
            Email = defaultEmail,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, defaultPassword);
        await userManager.AddToRoleAsync(user, Roles.Admin);
    }
}
