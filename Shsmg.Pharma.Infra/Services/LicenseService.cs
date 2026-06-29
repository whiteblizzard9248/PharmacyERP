using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NSec.Cryptography;
using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.Infra.Services;

public sealed class LicenseService : ILicenseService
{
    private static readonly SignatureAlgorithm Algo = SignatureAlgorithm.Ed25519;

    private readonly PublicKey _publicKey;

    private const string PublicKeyPem = @"
-----BEGIN PUBLIC KEY-----
MCowBQYDK2VwAyEAPcHRBDO6QQSvi+PAtBMRU1txq0YzOLiJt5RNvJ4oc2o=
-----END PUBLIC KEY-----
";

    public LicenseService()
    {
        var keyBytes = LoadSpkiFromPem(PublicKeyPem);

        _publicKey = PublicKey.Import(
            Algo,
            keyBytes,
            KeyBlobFormat.PkixPublicKey);
    }

    public LicenseValidationResult Validate(
        string? licenseKey,
        string currentHardwareId)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return LicenseValidationResult.Invalid("License key missing");

        if (string.IsNullOrWhiteSpace(currentHardwareId))
            return LicenseValidationResult.Invalid("Invalid hardware id");

        if (!TryValidatePayload(
                licenseKey,
                out var payload,
                out var error))
        {
            return LicenseValidationResult.Invalid(error);
        }

        var currentHw = Normalize(currentHardwareId);
        var licenseHw = Normalize(payload.HardwareId);

        if (payload.DeploymentType == DeploymentType.Onprem &&
            currentHw != licenseHw)
        {
            return LicenseValidationResult.Invalid(
                "License not valid for this machine");
        }

        return ValidateExpiry(payload);
    }

    public LicenseValidationResult ValidateCloud(
        string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return LicenseValidationResult.Invalid("License key missing");

        if (!TryValidatePayload(
                licenseKey,
                out var payload,
                out var error))
        {
            return LicenseValidationResult.Invalid(error);
        }

        return ValidateExpiry(payload);
    }

    private bool TryValidatePayload(
        string licenseKey,
        out LicensePayload payload,
        out string errorMessage)
    {
        payload = new LicensePayload();
        errorMessage = string.Empty;

        try
        {
            licenseKey = licenseKey
                .Trim()
                .Replace("\"", "");

            var parts = licenseKey.Split('.');

            if (parts.Length != 3)
            {
                errorMessage = "Invalid license format";
                return false;
            }

            //
            // Same as Go ValidateToken()
            // signingInput := headerPart + "." + payloadPart
            //
            var headerPart = parts[0];
            var payloadPart = parts[1];
            var signaturePart = parts[2];

            //
            // Optional: decode header and inspect kid/alg
            //
            var headerBytes =
                Base64UrlDecode(headerPart);

            var header =
                JsonSerializer.Deserialize<TokenHeader>(
                    headerBytes);

            if (header == null)
            {
                errorMessage = "Invalid token header";
                return false;
            }

            //
            // Verify signature against EXACT same bytes
            // signed in Go:
            //
            // signingInput :=
            //      base64url(header) + "." +
            //      base64url(payload)
            //
            var signingInput =
                $"{headerPart}.{payloadPart}";

            var signatureBytes =
                Base64UrlDecode(signaturePart);

            var valid = Algo.Verify(
                _publicKey,
                Encoding.UTF8.GetBytes(signingInput),
                signatureBytes);

            if (!valid)
            {
                errorMessage = "Invalid license signature";
                return false;
            }

            //
            // Only decode payload after verification
            //
            var payloadBytes =
                Base64UrlDecode(payloadPart);

            var json =
                Encoding.UTF8.GetString(payloadBytes);

            payload =
                JsonSerializer.Deserialize<LicensePayload>(
                    json)!;

            if (payload == null)
            {
                errorMessage = "Invalid license payload";
                return false;
            }

            payload.Expiry =
                NormalizeIncoming(payload.Expiry);

            return true;
        }
        catch (Exception ex)
        {
            errorMessage =
                $"License validation failed: {ex.Message}";
            return false;
        }
    }

    private static LicenseValidationResult ValidateExpiry(
        LicensePayload payload)
    {
        var now = DateTime.UtcNow;

        if (payload.Expiry < now.AddMinutes(-5))
        {
            return LicenseValidationResult.Invalid(
                "License expired");
        }

        if (payload.Expiry <= now.AddDays(3))
        {
            return LicenseValidationResult.Invalid(
                "License is expiring soon");
        }

        return LicenseValidationResult.Valid(payload);
    }

    private static DateTime NormalizeIncoming(
        DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc)
            return dt;

        if (dt.Kind == DateTimeKind.Unspecified)
        {
            dt = DateTime.SpecifyKind(
                dt,
                DateTimeKind.Local);
        }

        return dt.ToUniversalTime();
    }

    private static string Normalize(string value)
    {
        return (value ?? string.Empty)
            .Trim()
            .ToUpperInvariant();
    }

    private static byte[] LoadSpkiFromPem(
        string pem)
    {
        var lines = pem
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x =>
                !x.StartsWith("-----") &&
                !string.IsNullOrWhiteSpace(x));

        var base64 = string.Concat(lines);

        return Convert.FromBase64String(base64);
    }

    private static byte[] Base64UrlDecode(
        string input)
    {
        var base64 = input
            .Replace('-', '+')
            .Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }

    private sealed class TokenHeader
    {
        [JsonPropertyName("alg")]
        public string Alg { get; set; } = string.Empty;
        [JsonPropertyName("kid")]
        public string Kid { get; set; } = string.Empty;
    }
}