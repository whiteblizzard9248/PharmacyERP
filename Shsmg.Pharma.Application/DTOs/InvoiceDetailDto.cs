using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.DTOs;

public class InvoiceDetailDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;

    public List<InvoiceItemDto> Items { get; set; } = [];

    // Summary calculations for the footer
    public decimal GrossTotal => Items.Sum(x => x.TotalWithoutTax); // base
    public decimal TotalGst => Items.Sum(x => x.GstAmount);
    public decimal NetTotal => Items.Sum(x => x.LineTotal); // already inclusive
    public byte[] RowVersion { get; set; } = null!;

    public static CreateInvoiceDto ToCreateInvoiceDto(InvoiceDetailDto source)
    {
        return new CreateInvoiceDto
        {
            RowVersion = source.RowVersion,
            PatientName = source.PatientName,
            DoctorName = source.DoctorName,
            CustomerId = source.CustomerId,
            Date = source.Date,
            InvoiceNumber = source.InvoiceNumber,

            Items = [.. source.Items.Select(item => new InvoiceItemDto
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

    public static UpdateInvoiceDto ToUpdateInvoiceDto(InvoiceDetailDto source)
    {
        return new UpdateInvoiceDto
        {
            Id = source.Id,
            RowVersion = source.RowVersion,
            PatientName = source.PatientName,
            DoctorName = source.DoctorName,
            Date = source.Date,
            InvoiceNumber = source.InvoiceNumber,

            Items = [.. source.Items.Select(item => new InvoiceItemDto
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
