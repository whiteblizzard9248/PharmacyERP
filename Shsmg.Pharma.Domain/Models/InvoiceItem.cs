namespace Shsmg.Pharma.Domain.Models;

public class InvoiceItem : BaseEntity
{
    public string? Description { get; set; }
    public string? Package { get; set; } // e.g., "10's"
    public string? Mfg { get; set; }
    public string? Batch { get; set; }
    public string? ExpiryDate { get; set; } // Manual text for now

    public int Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal GstPercentage { get; set; } // e.g., 12 or 18

    public decimal Total => Quantity * Rate;
}