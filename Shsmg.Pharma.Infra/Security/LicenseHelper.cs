using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Shsmg.Pharma.Infra.Security;

public static class LicenseHelper
{
    public static string GetHardwareId()
    {
        var identifier = string.Empty;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            identifier = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? string.Empty;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Common location for unique machine ID on Linux
            if (File.Exists("/etc/machine-id"))
                identifier = File.ReadAllText("/etc/machine-id");
            else if (File.Exists("/var/lib/dbus/machine-id"))
                identifier = File.ReadAllText("/var/lib/dbus/machine-id");
        }

        if (string.IsNullOrWhiteSpace(identifier))
        {
            identifier = Environment.MachineName;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(identifier.Trim()));
        return Convert.ToBase64String(bytes)[..24];
    }
}