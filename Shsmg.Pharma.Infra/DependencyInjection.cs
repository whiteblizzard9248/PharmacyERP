using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shsmg.Pharma.Infra.Auth;
using Shsmg.Pharma.Infra.Persistence;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.Services;
using Shsmg.Pharma.Infra.Services;
using QuestPDF.Infrastructure;

namespace Shsmg.Pharma.Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDatabasePathProvider, SqliteDatabasePathProvider>();
        services.AddScoped<RowVersionInterceptor>();
        services.AddDbContext<PharmacyDbContext>((provider, options) =>
        {
            var databasePathProvider =
                provider.GetRequiredService<IDatabasePathProvider>();

            var databasePath =
                databasePathProvider.GetDatabasePath();

            options.UseSqlite($"Data Source={databasePath}")
                .EnableSensitiveDataLogging(false);
        });

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

        // Register repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        QuestPDF.Settings.License = LicenseType.Community;

        return services;
    }
}