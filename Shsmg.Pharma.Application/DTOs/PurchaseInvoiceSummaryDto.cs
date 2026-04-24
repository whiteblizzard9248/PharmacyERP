namespace Shsmg.Pharma.Application.DTOs;

public class PurchaseInvoiceSummaryDto
{
    public Guid Id { get; set; }
    public string PurchaseInvoiceNumber { get; set; } = string.Empty;
    public string SupplierInvoiceNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal NetTotal { get; set; }
}
