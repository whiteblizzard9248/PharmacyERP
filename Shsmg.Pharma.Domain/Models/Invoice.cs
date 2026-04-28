namespace Shsmg.Pharma.Domain.Models;

public class Invoice : BaseEntity, IHasRowVersion
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;

    // Customer relationship
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public List<InvoiceItem> Items { get; set; } = [];
    public List<Receipt> Receipts { get; set; } = [];

    public decimal GrossTotal { get; set; } = decimal.Zero;
    public decimal TaxTotal { get; set; } = decimal.Zero;
    public decimal NetTotal { get; set; } = decimal.Zero;
    public byte[] RowVersion { get; set; } = null!;
}
