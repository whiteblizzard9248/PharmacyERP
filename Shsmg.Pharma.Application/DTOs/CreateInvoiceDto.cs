namespace Shsmg.Pharma.Application.DTOs;

public class CreateInvoiceDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; } // Optional for walk-in customers
    public DateTime Date { get; set; } = DateTime.Now;

    public List<InvoiceItemDto> Items { get; set; } = [];

    // Summary calculations for the footer
    public decimal GrossTotal => Items.Sum(x => x.TotalWithoutTax); // base
    public decimal TotalGst => Items.Sum(x => x.GstAmount);
    public decimal NetTotal => Items.Sum(x => x.LineTotal); // already inclusive
    public byte[] RowVersion { get; set; } = [];
}