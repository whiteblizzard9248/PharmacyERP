namespace Shsmg.Pharma.Application.DTOs;

public class UpdatePurchaseInvoiceDto
{
    public Guid Id { get; set; }
    public string PurchaseInvoiceNumber { get; set; } = string.Empty;
    public string SupplierInvoiceNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;
    public List<PurchaseInvoiceItemDto> Items { get; set; } = [];

    public decimal GrossTotal => Items.Sum(x => x.TotalWithoutTax);
    public decimal TotalGst => Items.Sum(x => x.GstAmount);
    public decimal NetTotal => Items.Sum(x => x.LineTotal);
    public byte[] RowVersion { get; set; } = [];
}
