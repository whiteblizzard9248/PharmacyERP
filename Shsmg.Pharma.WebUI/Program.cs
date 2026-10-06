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
using Shsmg.Pharma.Infra.Services;
using Shsmg.Pharma.WebUI;
using Shsmg.Pharma.WebUI.Services;

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

    // Receipt permissions
    options.AddPolicy("Receipt.View", policy => policy.RequireClaim("Permission", Permissions.ReceiptView));
    options.AddPolicy("Receipt.Create", policy => policy.RequireClaim("Permission", Permissions.ReceiptCreate));
    options.AddPolicy("Receipt.Edit", policy => policy.RequireClaim("Permission", Permissions.ReceiptEdit));
    options.AddPolicy("Receipt.Delete", policy => policy.RequireClaim("Permission", Permissions.ReceiptDelete));

    // Payment permissions
    options.AddPolicy("Payment.View", policy => policy.RequireClaim("Permission", Permissions.PaymentView));
    options.AddPolicy("Payment.Create", policy => policy.RequireClaim("Permission", Permissions.PaymentCreate));
    options.AddPolicy("Payment.Edit", policy => policy.RequireClaim("Permission", Permissions.PaymentEdit));
    options.AddPolicy("Payment.Delete", policy => policy.RequireClaim("Permission", Permissions.PaymentDelete));

    // User management
    options.AddPolicy("User.Manage", policy => policy.RequireClaim("Permission", Permissions.UserManage));
});
builder.Services.AddAntiforgery();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();


builder.Services.AddScoped<PermissionService>();
builder.Services.AddSingleton<LicenseStatus>();
builder.Services.AddSingleton<ILicenseService, LicenseService>();

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
                    Log.Information("Database migration completed");
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
            Log.Information("Migration mode completed. Exiting application");
            return;
        }
        var status = app.Services.GetRequiredService<LicenseStatus>();

        status.IsValid = true;
        status.Message = string.Empty;

        try
        {
            var company = await context.Companies.FirstOrDefaultAsync();
            if (company != null)
            {
                var licenseService = app.Services.GetRequiredService<ILicenseService>();
                if (company.LicenseKey == null) throw new Exception("License Key Cannot be null");
                var currentHardwareId = LicenseHelper.GetHardwareId();
                Log.Information("Verifying license for hardware ID: {HardwareId}", currentHardwareId);
                var result = licenseService.Validate(company.LicenseKey, currentHardwareId);
                status.IsValid = result.IsValid;
                status.Message = result.Message;
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
    var isDev = app.Environment.IsDevelopment();
    Log.Information("Is Development: {IsDev}", isDev);
    if (isDev)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<PharmacyDbContext>();
        await PharmacyTestDataSeeder.SeedAsync(context);
    }
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

return;

static async Task SeedDefaultUserAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();

    var userManager =
        scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    var roleManager =
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // ---------------------------------------------------------
    // Roles
    // ---------------------------------------------------------

    var roleNames = Roles.RolePermissions.Keys.ToList();

    var existingRoleNames = await roleManager.Roles
        .Where(r => r.Name != null && roleNames.Contains(r.Name))
        .Select(r => r.Name!)
        .ToListAsync();

    if (existingRoleNames.Count == roleNames.Count)
    {
        Log.Information("All roles exist");
    }
    else
    {
        foreach (var roleName in roleNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);

            if (role == null)
            {
                role = new IdentityRole(roleName);

                var result = await roleManager.CreateAsync(role);

                if (!result.Succeeded)
                {
                    Log.Error(
                        "Failed to create role {RoleName}: {Errors}",
                        roleName,
                        string.Join(", ", result.Errors.Select(e => e.Description)));

                    continue;
                }
            }

            var existingClaims =
                await roleManager.GetClaimsAsync(role);

            var existingPermissions = existingClaims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value)
                .ToHashSet();

            var desiredPermissions =
                Roles.RolePermissions[roleName].ToHashSet();

            foreach (var permission in
                     desiredPermissions.Except(existingPermissions))
            {
                await roleManager.AddClaimAsync(
                    role,
                    new Claim("Permission", permission));
            }

            foreach (var claim in existingClaims.Where(c =>
                c.Type == "Permission" &&
                !desiredPermissions.Contains(c.Value)))
            {
                await roleManager.RemoveClaimAsync(role, claim);
            }
        }
    }

    // ---------------------------------------------------------
    // Default users
    // ---------------------------------------------------------

    const string adminEmail = "admin@pharma.local";
    const string adminPassword = "Admin@1234";

    const string managerEmail = "manager@pharma.local";
    const string managerPassword = "Manager@1234";

    const string employeeEmail = "employee@pharma.local";
    const string employeePassword = "Employee@1234";

    var defaultUsers = new[]
    {
        (Email: adminEmail, Password: adminPassword, Role: Roles.Admin),
        (Email: managerEmail, Password: managerPassword, Role: Roles.Manager),
        (Email: employeeEmail, Password: employeePassword, Role: Roles.Employee)
    };

    var userEmails = defaultUsers
        .Select(x => x.Email)
        .ToList();

    var existingUserEmails = await userManager.Users
        .Where(u => u.Email != null && userEmails.Contains(u.Email))
        .Select(u => u.Email!)
        .ToListAsync();

    if (existingUserEmails.Count == userEmails.Count)
    {
        Log.Information(
            "All default users exist: {UsersCount}",
            existingUserEmails.Count);

        return;
    }

    foreach (var user in defaultUsers)
    {
        if (existingUserEmails.Contains(user.Email))
            continue;

        await CreateDefaultUser(
            userManager,
            user.Email,
            user.Password,
            user.Role);
    }
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
