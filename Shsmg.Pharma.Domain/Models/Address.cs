namespace Shsmg.Pharma.Domain.Models;

/// <summary>
/// Value object representing a physical or billing address.
/// Immutable and suitable for comparison by value.
/// </summary>
public class Address
{
    /// <summary>Street address line 1 (required)</summary>
    public string Street { get; init; }

    /// <summary>Street address line 2 (optional, for apartment/suite number)</summary>
    public string? Street2 { get; init; }

    /// <summary>City or town (required)</summary>
    public string City { get; init; }

    /// <summary>State or province (required for Indian addresses)</summary>
    public string State { get; init; }

    /// <summary>Postal code (required for Indian addresses - PIN code)</summary>
    public string PostalCode { get; init; }

    /// <summary>Country (default: India for pharmacy context)</summary>
    public string Country { get; init; } = "India";

    /// <summary>Latitude for mapping (optional, for future logistics)</summary>
    public double? Latitude { get; init; }

    /// <summary>Longitude for mapping (optional, for future logistics)</summary>
    public double? Longitude { get; init; }

    /// <summary>
    /// Private constructor for EF Core. Use factory methods for object creation.
    /// </summary>
    private Address()
    {
        Street = string.Empty;
        City = string.Empty;
        State = string.Empty;
        PostalCode = string.Empty;
    }

    /// <summary>
    /// Creates a new Address value object.
    /// Validates that required fields are non-empty.
    /// </summary>
    public static Address Create(string street, string city, string state, string postalCode,
        string? street2 = null, string country = "India", double? latitude = null, double? longitude = null)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street address is required.", nameof(street));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.", nameof(city));
        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State is required.", nameof(state));
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code is required.", nameof(postalCode));
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required.", nameof(country));

        // Validate PIN code format for India (6 digits)
        if (country == "India" && !System.Text.RegularExpressions.Regex.IsMatch(postalCode, @"^\d{6}$"))
            throw new ArgumentException("Indian PIN code must be exactly 6 digits.", nameof(postalCode));

        return new Address
        {
            Street = street.Trim(),
            Street2 = string.IsNullOrWhiteSpace(street2) ? null : street2.Trim(),
            City = city.Trim(),
            State = state.Trim(),
            PostalCode = postalCode.Trim(),
            Country = country,
            Latitude = latitude,
            Longitude = longitude
        };
    }

    /// <summary>
    /// Returns a formatted address string suitable for display or printing.
    /// </summary>
    public string GetFormattedAddress()
    {
        var lines = new List<string> { Street };
        if (!string.IsNullOrWhiteSpace(Street2))
            lines.Add(Street2);
        lines.Add($"{City}, {State} {PostalCode}");
        if (!string.IsNullOrWhiteSpace(Country))
            lines.Add(Country);

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Returns a single-line address suitable for CSV export or compact display.
    /// </summary>
    public string GetSingleLineAddress()
    {
        var parts = new List<string> { Street };
        if (!string.IsNullOrWhiteSpace(Street2))
            parts.Add(Street2);
        parts.AddRange(new[] { City, State, PostalCode });
        return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    /// <summary>
    /// Compares two Address value objects for equality.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not Address other)
            return false;

        return Street == other.Street
            && Street2 == other.Street2
            && City == other.City
            && State == other.State
            && PostalCode == other.PostalCode
            && Country == other.Country;
    }

    /// <summary>
    /// Generates hash code based on address components.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Street, Street2, City, State, PostalCode, Country);
    }
}
