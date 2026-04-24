namespace Shsmg.Pharma.Application.DTOs;

public class PurchaseInvoiceDetailDto
{
    public Guid Id { get; set; }
    public string PurchaseInvoiceNumber { get; set; } = string.Empty;
    public string SupplierInvoiceNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public string Notes { get; set; } = string.Empty;
    public List<PurchaseInvoiceItemDto> Items { get; set; } = [];
    public byte[] RowVersion { get; set; } = [];

    public decimal GrossTotal => Items.Sum(x => x.TotalWithoutTax);
    public decimal TotalGst => Items.Sum(x => x.GstAmount);
    public decimal NetTotal => Items.Sum(x => x.LineTotal);

    public static CreatePurchaseInvoiceDto ToCreateDto(PurchaseInvoiceDetailDto source)
    {
        return new CreatePurchaseInvoiceDto
        {
            PurchaseInvoiceNumber = source.PurchaseInvoiceNumber,
            SupplierInvoiceNumber = source.SupplierInvoiceNumber,
            SupplierId = source.SupplierId,
            Date = source.Date,
            Notes = source.Notes,
            RowVersion = source.RowVersion,
            Items = [.. source.Items.Select(item => new PurchaseInvoiceItemDto
            {
                Id = item.Id,
                InventoryItemId = item.InventoryItemId,
                Description = item.Description,
                HsnCode = item.HsnCode,
                Package = item.Package,
                Mfg = item.Mfg,
                Batch = item.Batch,
                ExpiryDate = item.ExpiryDate,
                Quantity = item.Quantity,
                Rate = item.Rate,
                GstPercentage = item.GstPercentage
            })]
        };
    }
}
