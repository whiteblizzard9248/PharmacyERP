using System.ComponentModel;

namespace Shsmg.Pharma.Application.Common;

public class LicenseValidationResult
{
    public bool IsValid { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public LicensePayload? LicensePayload { get; private set; }

    public static LicenseValidationResult Valid(LicensePayload? payload)
        => new() { IsValid = true, LicensePayload = payload };

    public static LicenseValidationResult Invalid(string message)
        => new() { IsValid = false, Message = message };
}

public class LicensePayload
{
    public string Company { get; set; } = string.Empty;
    public string LicenseId { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
    public DateTime Expiry { get; set; }
}

public class LicenseEnvelope
{
    public string Payload { get; set; } = string.Empty;     // base64 JSON
    public string Signature { get; set; } = string.Empty;   // base64 signature
}

public interface ILicenseService
{
    LicenseValidationResult Validate(string licenseKey, string currentHardwareId);
}