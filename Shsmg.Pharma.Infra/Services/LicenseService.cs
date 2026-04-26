using Shsmg.Pharma.Application.Common;
using System.Text;
using System.Text.Json;
using NSec.Cryptography;

namespace Shsmg.Pharma.Infra.Services;

public sealed class LicenseService : ILicenseService
{
    private static readonly SignatureAlgorithm Algo = SignatureAlgorithm.Ed25519;

    private readonly PublicKey _publicKey;

    // 🔐 Paste your PEM public key here
    private const string PublicKeyPem = @"
-----BEGIN PUBLIC KEY-----
MCowBQYDK2VwAyEAPcHRBDO6QQSvi+PAtBMRU1txq0YzOLiJt5RNvJ4oc2o=
-----END PUBLIC KEY-----
";

    public LicenseService()
    {
        var keyBytes = LoadSpkiFromPem(PublicKeyPem);
        _publicKey = PublicKey.Import(Algo, keyBytes, KeyBlobFormat.PkixPublicKey);
    }

    public LicenseValidationResult Validate(string? licenseKey, string currentHardwareId)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return LicenseValidationResult.Invalid("License key missing");

        if (string.IsNullOrWhiteSpace(currentHardwareId))
            return LicenseValidationResult.Invalid("Invalid license payload");

        licenseKey = licenseKey.Trim().Replace("\"", "");

        if (!TrySplit(licenseKey, out var payloadBytes, out var signatureBytes))
        { return LicenseValidationResult.Invalid("Invalid license format"); }

        // 🔐 Verify signature (Ed25519)
        if (!Algo.Verify(_publicKey, payloadBytes, signatureBytes))
        { return LicenseValidationResult.Invalid("Invalid license signature"); }

        LicensePayload payload;

        try
        {
            var json = Encoding.UTF8.GetString(payloadBytes);
            payload = JsonSerializer.Deserialize<LicensePayload>(json)!;
        }
        catch
        {
            return LicenseValidationResult.Invalid("Invalid license payload");
        }

        // Normalize hardware IDs
        var currentHw = Normalize(currentHardwareId);
        var licenseHw = Normalize(payload.HardwareId);

        if (!string.Equals(currentHw, licenseHw, StringComparison.Ordinal))
            return LicenseValidationResult.Invalid("Hardware mismatch");

        // Expiry check (with small tolerance)
        if (payload.Expiry < DateTime.UtcNow.AddMinutes(-5))
            return LicenseValidationResult.Invalid("License expired");

        return LicenseValidationResult.Valid(payload);
    }

    // -------- helpers --------

    private static string Normalize(string s)
        => (s ?? string.Empty).Trim().ToUpperInvariant();

    /// <summary>
    /// Extracts SubjectPublicKeyInfo (SPKI) DER bytes from a PEM public key.
    /// Works with:
    /// -----BEGIN PUBLIC KEY----- (recommended)
    /// </summary>
    private static byte[] LoadSpkiFromPem(string pem)
    {
        var lines = pem
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => !l.StartsWith("-----") && !string.IsNullOrWhiteSpace(l));

        var base64 = string.Concat(lines);

        return Convert.FromBase64String(base64);
    }

    private static bool TrySplit(string licenseKey, out byte[] payload, out byte[] signature)
    {
        payload = Array.Empty<byte>();
        signature = Array.Empty<byte>();

        if (string.IsNullOrEmpty(licenseKey))
        {
            Console.WriteLine("DEBUG: licenseKey is null or empty");
            return false;
        }

        var parts = licenseKey.Split('.');
        if (parts.Length != 2)
        {
            // This is likely your issue if you see "Invalid license format"
            Console.WriteLine($"DEBUG: Split failed. Found {parts.Length} parts. String: {licenseKey}");
            return false;
        }

        try
        {
            payload = Base64UrlDecode(parts[0]);
            signature = Base64UrlDecode(parts[1]);
            return true;
        }
        catch (Exception ex)
        {
            // This will tell you if the Base64 conversion is the culprit
            Console.WriteLine($"DEBUG: Decode failed: {ex.Message}");
            return false;
        }
    }
    private static byte[] Base64UrlDecode(string input)
    {
        string base64 = input.Replace('-', '+').Replace('_', '/');
        int mod = base64.Length % 4;
        if (mod > 0) base64 += new string('=', 4 - mod);
        return Convert.FromBase64String(base64);
    }
}