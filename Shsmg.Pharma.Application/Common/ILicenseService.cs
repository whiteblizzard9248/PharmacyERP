using System.ComponentModel;
using System.Text.Json.Serialization;

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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeploymentType
{
    Onprem,
    Cloud
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubscriptionType
{
    Trial,
    Silver,
    Gold,
    Platinum
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Features
{
    Accounting,
    UploadGDrive,
    UploadAws,
    UploadOther,
    Erp,
    Pharma,
}

public class LicensePayload
{
    [JsonPropertyName("Company")]
    public string Company { get; set; } = string.Empty;
    [JsonPropertyName("LicenseId")]
    public string LicenseId { get; set; } = string.Empty;
    [JsonPropertyName("HardwareId")]
    public string HardwareId { get; set; } = string.Empty;
    [JsonPropertyName("Expiry")]
    public DateTime Expiry { get; set; }
    [JsonPropertyName("KeyId")]
    public string KeyId { get; set; } = string.Empty;
    [JsonPropertyName("DeploymentType")]
    public DeploymentType DeploymentType { get; set; }
    [JsonPropertyName("SubscriptionType")]
    public SubscriptionType SubscriptionType { get; set; }
    [JsonPropertyName("Features")]
    public Features[] Features { get; set; } = [];
}

public class LicenseEnvelope
{
    public string Payload { get; set; } = string.Empty;     // base64 JSON
    public string Signature { get; set; } = string.Empty;   // base64 signature
}

public interface ILicenseService
{
    LicenseValidationResult Validate(string licenseKey, string currentHardwareId);
    LicenseValidationResult ValidateCloud(string licenseKey);
}