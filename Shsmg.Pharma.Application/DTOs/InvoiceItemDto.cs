using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.Application.DTOs;

public class InvoiceItemDto
{
    // Basic Details
    public string Description { get; set; } = string.Empty;
    public PackageType Package { get; set; } = PackageType.Unit; // e.g., 10's
    public string Mfg { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;

    // Financials
    public int Quantity { get; set; }
    public decimal Rate { get; set; } // Price before GST
    public decimal GstPercentage { get; set; } // e.g., 12.00

    // Calculated fields for the UI to display
    public decimal TotalWithoutTax => Quantity * Rate;
    public decimal GstAmount => TotalWithoutTax * (GstPercentage / 100);
    public decimal LineTotal => TotalWithoutTax + GstAmount;
}