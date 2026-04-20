namespace Shsmg.Pharma.Application.DTOs;

public class CompanyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty; // DL No.
    public string ContactNumber { get; set; } = string.Empty;

    public string? LicenseKey { get; set; }
    public DateTime? LicenseExpiry { get; set; }
    public string? HardwareId { get; set; }
    public bool IsActivated { get; set; }
}