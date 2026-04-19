using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.DTOs;

public class UpdateInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;

    public List<InvoiceItemDto> Items { get; set; } = [];

    // Summary calculations for the footer
    public decimal GrossTotal => Items.Sum(x => x.TotalWithoutTax); // base
    public decimal TotalGst => Items.Sum(x => x.GstAmount);
    public decimal NetTotal => Items.Sum(x => x.LineTotal); // already inclusive
    public byte[] RowVersion { get; set; } = null!;

    public Invoice ToInvoice()
    {
        return new Invoice
        {
            Id = this.Id,
            InvoiceNumber = this.InvoiceNumber,
            PatientName = this.PatientName,
            DoctorName = this.DoctorName,
            InvoiceDate = this.Date,
            LastModified = DateTime.UtcNow,
            Items = [.. this.Items.Select(dtoItem => new InvoiceItem
            {
                Id = dtoItem.Id,
                    Description = dtoItem.Description,
                    HsnCode = dtoItem.HsnCode,
                    Quantity = dtoItem.Quantity,
                    Rate = dtoItem.Rate,
                    GstPercentage = dtoItem.GstPercentage,
                    Batch = dtoItem.Batch,
                    ExpiryDate = dtoItem.ExpiryDate,
                    Package = dtoItem.Package,
                    LastModified = DateTime.UtcNow
            })],
        };
    }
}