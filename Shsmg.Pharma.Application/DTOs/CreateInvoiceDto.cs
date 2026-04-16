namespace Shsmg.Pharma.Application.DTOs;

public class CreateInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;

    public List<InvoiceItemDto> Items { get; set; } = [];

    // Summary calculations for the footer
    public decimal GrossTotal => Items.Sum(x => x.TotalWithoutTax);
    public decimal TotalGst => Items.Sum(x => x.GstAmount);
    public decimal NetTotal => GrossTotal + TotalGst;
}