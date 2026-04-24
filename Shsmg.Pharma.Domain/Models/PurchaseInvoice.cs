namespace Shsmg.Pharma.Domain.Models;

public class PurchaseInvoice : BaseEntity, IHasRowVersion
{
    public string PurchaseInvoiceNumber { get; set; } = string.Empty;
    public string SupplierInvoiceNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string Notes { get; set; } = string.Empty;

    public List<PurchaseInvoiceItem> Items { get; set; } = [];

    public decimal GrossTotal { get; set; } = decimal.Zero;
    public decimal TaxTotal { get; set; } = decimal.Zero;
    public decimal NetTotal { get; set; } = decimal.Zero;
    public byte[] RowVersion { get; set; } = null!;
}
