using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.Application.DTOs;

public class InvoiceItemDto
{
    // Basic Details
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string HsnCode { get; set; } = string.Empty;
    public string Package { get; set; } = string.Empty;
    public string Mfg { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;

    // Financials
    public int Quantity { get; set; }
    public decimal Rate { get; set; } // Price before GST
    public decimal GstPercentage { get; set; } // e.g., 12.00

    // Calculated fields for the UI to display
    public decimal TotalWithTax => Quantity * Rate;
    public decimal TotalWithoutTax => TotalWithTax / (1 + GstPercentage / 100);
    public decimal GstAmount => TotalWithTax - TotalWithoutTax;
    public decimal LineTotal => TotalWithTax;
}