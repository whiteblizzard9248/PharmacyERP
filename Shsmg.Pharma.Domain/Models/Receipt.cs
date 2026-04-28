namespace Shsmg.Pharma.Domain.Models;

public class Receipt : BaseEntity, IHasRowVersion
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public decimal Amount { get; set; } = decimal.Zero;
    public string PaymentMethod { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = null!;
}
