using Microsoft.Extensions.Configuration;
using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.Infra.Services;

public sealed class SqliteDatabasePathProvider(
    IConfiguration configuration) : IDatabasePathProvider
{
    public string GetDatabasePath()
    {
        var fileName =
            configuration["Database:FileName"]
            ?? "pharmacy.db";
        var applicationDataPath =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
        var applicationDirectory =
            Path.Combine(applicationDataPath, "PharmacyERP");
        Directory.CreateDirectory(applicationDirectory);
        return Path.Combine(applicationDirectory, fileName);
    }
}