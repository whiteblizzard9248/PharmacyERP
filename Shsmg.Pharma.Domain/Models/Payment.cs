namespace Shsmg.Pharma.Domain.Models;

public class Payment : BaseEntity, IHasRowVersion
{
    public string PaymentNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public Guid? PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }
    public decimal Amount { get; set; } = decimal.Zero;
    public string PaymentMethod { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = null!;
}
