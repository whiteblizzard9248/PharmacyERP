using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Shsmg.Pharma.Application;
using Shsmg.Pharma.WebUI.Components;
using Shsmg.Pharma.Infra;
using Shsmg.Pharma.Infra.Auth;
using Shsmg.Pharma.Application.Common;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Shsmg.Pharma.Infra.Persistence;
using Microsoft.AspNetCore.Components;

var culture = new CultureInfo("en-IN");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();

builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<PharmacyDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped(sp =>
{
    var navigation = sp.GetRequiredService<NavigationManager>();
    return new HttpClient
    {
        BaseAddress = new Uri(navigation.BaseUri)
    };
});
builder.Services.ConfigureApplicationCookie(options =>
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
builder.Services.AddAntiforgery();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();


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

    const string adminEmail = "admin@pharma.local";
    const string adminPassword = "Admin@1234";

    const string managerEmail = "manager.pharma.local";
    const string managerPassword = "Manager@1234";

    const string employeeEmail = "employee.pharma.local";
    const string employeePassword = "Employee@1234";

    await CreateDefaultUser(userManager, adminEmail, adminPassword, Roles.Admin);
    await CreateDefaultUser(userManager, managerEmail, managerPassword, Roles.Manager);
    await CreateDefaultUser(userManager, employeeEmail, employeePassword, Roles.Employee);
}

static async Task CreateDefaultUser(UserManager<AppUser> userManager, string userName, string password, string role)
{
    if (await userManager.FindByEmailAsync(userName) is null)
    {
        var user = new AppUser
        {
            UserName = userName,
            Email = userName,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role);
    }
}