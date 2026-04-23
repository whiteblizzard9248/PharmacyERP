namespace Shsmg.Pharma.Application.DTOs;

public class InvoiceSummaryDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal NetTotal { get; set; }
    public bool IsDeleted { get; set; } // Useful for the Admin view
}