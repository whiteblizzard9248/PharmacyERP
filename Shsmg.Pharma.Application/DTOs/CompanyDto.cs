namespace Shsmg.Pharma.Application.DTOs;

public class CompanyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty; // DL No.
    public string ContactNumber { get; set; } = string.Empty;

    // Configurable invoice print format settings
    public string InvoiceHeaderText { get; set; } = string.Empty;
    public string InvoiceFooterText { get; set; } = string.Empty;
    public bool PrintShowGst { get; set; } = true;
    public bool PrintShowExpiry { get; set; } = true;
}