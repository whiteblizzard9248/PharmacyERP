namespace Shsmg.Pharma.Application.DTOs;

public class PurchaseInvoiceItemDto
{
    public Guid Id { get; set; }
    public Guid? InventoryItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string HsnCode { get; set; } = string.Empty;
    public string Package { get; set; } = string.Empty;
    public string Mfg { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal GstPercentage { get; set; }

    public decimal TotalWithTax => Quantity * Rate;
    public decimal TotalWithoutTax => TotalWithTax / (1 + GstPercentage / 100);
    public decimal GstAmount => TotalWithTax - TotalWithoutTax;
    public decimal LineTotal => TotalWithTax;
}
