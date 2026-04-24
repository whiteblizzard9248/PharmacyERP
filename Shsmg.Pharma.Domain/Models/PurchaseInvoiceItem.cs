namespace Shsmg.Pharma.Domain.Models;

public class PurchaseInvoiceItem : BaseEntity
{
    public Guid? PurchaseInvoiceId { get; set; }
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

    public decimal Total => Quantity * Rate;
}
