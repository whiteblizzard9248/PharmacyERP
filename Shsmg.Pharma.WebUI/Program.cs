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
using Shsmg.Pharma.Infra.Security;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Serilog;
using Serilog.Events;

var culture = new CultureInfo("en-IN");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "Shsmg.Pharma.WebUI")
    .WriteTo.Console()
    .WriteTo.Async(a => a.File(
        "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 10,
        fileSizeLimitBytes: 10_000_000,
        rollOnFileSizeLimit: true,
        shared: true
    ))
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
var isMigrationMode = args.Contains("--migrate");
builder.Host.UseSerilog();

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

    // Inventory permissions
    options.AddPolicy("Inventory.View", policy => policy.RequireClaim("Permission", Permissions.InventoryView));
    options.AddPolicy("Inventory.Create", policy => policy.RequireClaim("Permission", Permissions.InventoryCreate));
    options.AddPolicy("Inventory.Edit", policy => policy.RequireClaim("Permission", Permissions.InventoryEdit));
    options.AddPolicy("Inventory.Delete", policy => policy.RequireClaim("Permission", Permissions.InventoryDelete));

    // Supplier permissions
    options.AddPolicy("Supplier.View", policy => policy.RequireClaim("Permission", Permissions.SupplierView));
    options.AddPolicy("Supplier.Create", policy => policy.RequireClaim("Permission", Permissions.SupplierCreate));
    options.AddPolicy("Supplier.Edit", policy => policy.RequireClaim("Permission", Permissions.SupplierEdit));
    options.AddPolicy("Supplier.Delete", policy => policy.RequireClaim("Permission", Permissions.SupplierDelete));

    // Purchase permissions
    options.AddPolicy("Purchase.View", policy => policy.RequireClaim("Permission", Permissions.PurchaseView));
    options.AddPolicy("Purchase.Create", policy => policy.RequireClaim("Permission", Permissions.PurchaseCreate));
    options.AddPolicy("Purchase.Edit", policy => policy.RequireClaim("Permission", Permissions.PurchaseEdit));
    options.AddPolicy("Purchase.Delete", policy => policy.RequireClaim("Permission", Permissions.PurchaseDelete));

    // User management
    options.AddPolicy("User.Manage", policy => policy.RequireClaim("Permission", Permissions.UserManage));
});
builder.Services.AddAntiforgery();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();


builder.Services.AddScoped<Shsmg.Pharma.WebUI.Services.PermissionService>();
builder.Services.AddSingleton<LicenseStatus>();

try
{
    Log.Information("Starting Pharma ERP...");
    var app = builder.Build();
    app.UseSerilogRequestLogging();

    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        if (isMigrationMode || app.Environment.IsProduction())
        {
            Log.Information("Applying database migrations...");

            var retries = 5;
            while (retries-- > 0)
            {
                try
                {
                    await context.Database.MigrateAsync();
                    Log.Information("Database migration completed.");
                    break;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Migration failed. Retrying...");
                    await Task.Delay(3000);
                }
            }
        }

        // 👉 EXIT EARLY if installer mode
        if (isMigrationMode)
        {
            Log.Information("Migration mode completed. Exiting application.");
            return;
        }
        var status = app.Services.GetRequiredService<LicenseStatus>();

        status.IsValid = true;
        status.Message = string.Empty;

        try
        {

            var company = await context.Companies.FirstOrDefaultAsync();
            var currentHardwareId = LicenseHelper.GetHardwareId();

            Log.Information("Verifying license for hardware ID: {HardwareId}", currentHardwareId);

            if (company != null)
            {
                if (!company.IsActivated)
                {
                    status.IsValid = false;
                    status.Message = "License is not activated. Please update your store profile to activate the installation.";
                }
                else if (company.HardwareId != currentHardwareId)
                {
                    status.IsValid = false;
                    status.Message = "Unauthorized Hardware: This installation is locked to another server.";
                }
                else if (company.LicenseExpiry.HasValue && company.LicenseExpiry < DateTime.UtcNow)
                {
                    status.IsValid = false;
                    status.Message = "License Expired: Please contact Shsmg Pharma Support.";
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database unavailable during startup");
            status.IsValid = false;
            status.Message = "Database not available. Please check installation.";
        }
    }

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

    Log.Information("Seeding default user and roles...");
    await SeedDefaultUserAsync(app.Services);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static async Task SeedDefaultUserAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Seed roles
    foreach (var roleName in Roles.RolePermissions.Keys)
    {
        var role = await roleManager.FindByNameAsync(roleName);

        if (role == null)
        {
            role = new IdentityRole(roleName);
            await roleManager.CreateAsync(role);
        }

        var existingClaims = await roleManager.GetClaimsAsync(role);
        var existingPermissions = existingClaims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .ToHashSet();

        var desiredPermissions = Roles.RolePermissions[roleName].ToHashSet();

        // Add missing permissions
        foreach (var permission in desiredPermissions.Except(existingPermissions))
        {
            await roleManager.AddClaimAsync(role, new Claim("Permission", permission));
        }

        // Optional: remove stale permissions
        foreach (var claim in existingClaims.Where(c =>
            c.Type == "Permission" && !desiredPermissions.Contains(c.Value)))
        {
            await roleManager.RemoveClaimAsync(role, claim);
        }
    }

    const string adminEmail = "admin@pharma.local";
    const string adminPassword = "Admin@1234";

    const string managerEmail = "manager@pharma.local";
    const string managerPassword = "Manager@1234";

    const string employeeEmail = "employee@pharma.local";
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
