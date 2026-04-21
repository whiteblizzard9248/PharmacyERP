using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shsmg.Pharma.Infra.Auth;
using Shsmg.Pharma.Infra.Persistence;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Infra.Services;
using QuestPDF.Infrastructure;

namespace Shsmg.Pharma.Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext with PostgreSQL provider
        services.AddDbContext<PharmacyDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")).EnableSensitiveDataLogging(false));

        services.AddScoped<RowVersionInterceptor>();
        // Register IPharmacyDbContext for DI
        services.AddScoped<IPharmacyDbContext>(provider => provider.GetRequiredService<PharmacyDbContext>());
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

        // Register Identity services
        services.AddIdentityCore<AppUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<PharmacyDbContext>()
                .AddSignInManager();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();
        QuestPDF.Settings.License = LicenseType.Community;

        return services;
    }
}
