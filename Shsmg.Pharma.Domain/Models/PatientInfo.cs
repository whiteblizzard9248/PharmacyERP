namespace Shsmg.Pharma.Domain.Models;

/// <summary>
/// Value object representing patient/individual information for regulatory compliance.
/// Used by Registered customers to track medical and identification details.
/// Immutable for audit trail integrity.
/// </summary>
public class PatientInfo
{
    /// <summary>Age of the patient (optional, can be null for walk-ins)</summary>
    public bool Exists { get; init; } = true;
    public int? Age { get; init; }

    /// <summary>Gender: M (Male), F (Female), O (Other), null for unknown</summary>
    public char? Gender { get; init; }

    /// <summary>GSTIN (Goods and Services Tax Identification Number) for billing</summary>
    public string? GSTIN { get; init; }

    /// <summary>Aadhar number (Indian unique ID) - optional, for identity verification</summary>
    public string? AadharNumber { get; init; }

    /// <summary>Pan number (Permanent Account Number) - optional, for corporate customers</summary>
    public string? PanNumber { get; init; }

    /// <summary>Doctor name for prescription reference (optional)</summary>
    public string? DoctorName { get; init; }

    /// <summary>Prescription/medical condition notes (optional, for context)</summary>
    public string? MedicalNotes { get; init; }

    /// <summary>Date when patient info was last updated</summary>
    public DateTime? LastUpdatedAt { get; init; }

    /// <summary>
    /// Private constructor for EF Core. Use factory methods for object creation.
    /// </summary>
    private PatientInfo() { }

    /// <summary>
    /// Creates a new PatientInfo value object for a registered customer.
    /// All fields are optional to support incremental data collection.
    /// </summary>
    public static PatientInfo Create(
        int? age = null,
        char? gender = null,
        string? gstin = null,
        string? aadharNumber = null,
        string? panNumber = null,
        string? doctorName = null,
        string? medicalNotes = null)
    {
        // Validate gender if provided
        if (gender.HasValue && !IsValidGender(gender.Value))
            throw new ArgumentException("Gender must be M, F, or O.", nameof(gender));

        // Validate age if provided
        if (age.HasValue && (age < 0 || age > 150))
            throw new ArgumentException("Age must be between 0 and 150.", nameof(age));

        // Validate GSTIN format if provided (15 alphanumeric characters for India)
        if (!string.IsNullOrWhiteSpace(gstin) && !IsValidGSTIN(gstin))
            throw new ArgumentException("GSTIN must be 15 alphanumeric characters.", nameof(gstin));

        // Validate Aadhar if provided (12 digits)
        if (!string.IsNullOrWhiteSpace(aadharNumber) && !IsValidAadhar(aadharNumber))
            throw new ArgumentException("Aadhar number must be exactly 12 digits.", nameof(aadharNumber));

        // Validate PAN if provided (10 alphanumeric characters following specific format)
        if (!string.IsNullOrWhiteSpace(panNumber) && !IsValidPAN(panNumber))
            throw new ArgumentException("PAN format is invalid.", nameof(panNumber));

        return new PatientInfo
        {
            Age = age,
            Gender = gender,
            GSTIN = string.IsNullOrWhiteSpace(gstin) ? null : gstin.Trim().ToUpperInvariant(),
            AadharNumber = string.IsNullOrWhiteSpace(aadharNumber) ? null : aadharNumber.Trim(),
            PanNumber = string.IsNullOrWhiteSpace(panNumber) ? null : panNumber.Trim().ToUpperInvariant(),
            DoctorName = string.IsNullOrWhiteSpace(doctorName) ? null : doctorName.Trim(),
            MedicalNotes = string.IsNullOrWhiteSpace(medicalNotes) ? null : medicalNotes.Trim(),
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an empty PatientInfo for walk-in customers (no data collected).
    /// </summary>
    public static PatientInfo CreateEmpty()
    {
        return new PatientInfo { LastUpdatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Validates gender input: M (Male), F (Female), O (Other).
    /// </summary>
    private static bool IsValidGender(char gender)
    {
        return gender is 'M' or 'F' or 'O';
    }

    /// <summary>
    /// Validates GSTIN format: 15 alphanumeric characters.
    /// India format: 2-digit state code + 10-digit PAN + 1-digit entity number + 1-digit check digit + 1-digit.
    /// </summary>
    private static bool IsValidGSTIN(string gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin))
            return false;

        gstin = gstin.Trim();
        return gstin.Length == 15 && System.Text.RegularExpressions.Regex.IsMatch(gstin, @"^[0-9A-Z]{15}$");
    }

    /// <summary>
    /// Validates Aadhar number: exactly 12 digits.
    /// </summary>
    private static bool IsValidAadhar(string aadhar)
    {
        if (string.IsNullOrWhiteSpace(aadhar))
            return false;

        aadhar = aadhar.Trim();
        return aadhar.Length == 12 && System.Text.RegularExpressions.Regex.IsMatch(aadhar, @"^\d{12}$");
    }

    /// <summary>
    /// Validates PAN number: 10 character format (AAAAA1234A)
    /// </summary>
    private static bool IsValidPAN(string pan)
    {
        if (string.IsNullOrWhiteSpace(pan))
            return false;

        pan = pan.Trim().ToUpperInvariant();
        // Format: AAAAA1234A (5 letters, 4 digits, 1 letter)
        return pan.Length == 10 && System.Text.RegularExpressions.Regex.IsMatch(pan, @"^[A-Z]{5}\d{4}[A-Z]$");
    }

    /// <summary>
    /// Compares two PatientInfo value objects for equality.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not PatientInfo other)
            return false;

        return Age == other.Age
            && Gender == other.Gender
            && GSTIN == other.GSTIN
            && AadharNumber == other.AadharNumber
            && PanNumber == other.PanNumber
            && DoctorName == other.DoctorName;
    }

    /// <summary>
    /// Generates hash code based on patient info components.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Age, Gender, GSTIN, AadharNumber, PanNumber, DoctorName);
    }
}
