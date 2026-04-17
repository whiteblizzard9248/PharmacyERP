using Microsoft.VisualBasic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Infra.Services;

public class PdfGeneratorService : IPdfGeneratorService
{
    public byte[] GeneratePdf(CreateInvoiceDto invoiceDto, CompanyDto companyDto)
    {
        return Document.Create(container =>
    {
        container.Page(page =>
        {
            // Set to A6 for Quarter-A4 size
            page.Size(PageSizes.A6);
            page.Margin(0.5f, Unit.Centimetre);
            page.PageColor(Colors.White);

            // Primary blue color matching the image ink
            var primaryBlue = "#002D62";
            page.DefaultTextStyle(x => x.FontSize(8).FontColor(primaryBlue));

            page.Header().Column(column =>
            {
                // Company Name and Invoice Number
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(t => t.Span(companyDto.Name).FontSize(11).Bold());
                        col.Item().Text(t => t.Span(companyDto.Address).FontSize(7));
                    });

                    // Red color for the Invoice Number as seen in image
                    row.ConstantItem(50).AlignRight()
                        .Text(t => t.Span($"No. {invoiceDto.InvoiceNumber}").FontColor(Colors.Red.Medium).Bold().FontSize(12));
                });

                column.Item().AlignCenter()
                    .Text(t => t.Span($"DL No. {companyDto.LicenseNumber} | M. {companyDto.ContactNumber}").FontSize(7));

                column.Item().PaddingTop(2).LineHorizontal(1).LineColor(primaryBlue);

                // Patient and Date Row
                column.Item().PaddingVertical(2).Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Patient Name: ").Bold();
                        t.Span(invoiceDto.PatientName);
                    });
                    row.ConstantItem(60).AlignRight().Text($"Date: {invoiceDto.Date:dd/MM/yy}");
                });

                // Doctor Row
                column.Item().PaddingBottom(2).Text(t =>
                {
                    t.Span("Doctor: ").Bold();
                    t.Span(invoiceDto.DoctorName);
                });
            });

            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(20); // Qty
                    columns.RelativeColumn();   // Description
                    columns.ConstantColumn(25); // Pkg
                    columns.ConstantColumn(25); // Mfg
                    columns.ConstantColumn(30); // Batch
                    columns.ConstantColumn(30); // Exp. Dt
                    columns.ConstantColumn(40); // Amount
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Qty.");
                    header.Cell().Element(CellStyle).Text("Description");
                    header.Cell().Element(CellStyle).Text("Pkg.");
                    header.Cell().Element(CellStyle).Text("Mfg.");
                    header.Cell().Element(CellStyle).Text("Batch");
                    header.Cell().Element(CellStyle).Text("Exp. Dt.");
                    header.Cell().Element(CellStyle).AlignRight().Text("Amount");

                    static IContainer CellStyle(IContainer container) =>
                        container.DefaultTextStyle(x => x.Bold().FontSize(7))
                                 .PaddingVertical(2).Border(1).BorderColor("#002D62").AlignCenter();
                });

                foreach (var item in invoiceDto.Items)
                {
                    table.Cell().Element(RowStyle).Text(item.Quantity.ToString());
                    table.Cell().Element(RowStyle).Text(item.Description);
                    table.Cell().Element(RowStyle).Text(item.Package.ToString());
                    table.Cell().Element(RowStyle).Text(item.Mfg);
                    table.Cell().Element(RowStyle).Text(item.Batch);
                    table.Cell().Element(RowStyle).Text(item.ExpiryDate);
                    table.Cell().Element(RowStyle).AlignRight().Text(item.LineTotal.ToString("N2"));

                    static IContainer RowStyle(IContainer container) =>
                        container.Border(1).BorderColor("#002D62").PaddingHorizontal(2).PaddingVertical(1).DefaultTextStyle(x => x.FontSize(7));
                }
            });

            page.Footer().PaddingTop(5).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(t => t.Span("Goods once sold cannot be taken back or exchanged").FontSize(6));
                    });

                    // Total Box
                    row.ConstantItem(80).Border(1).BorderColor(primaryBlue).Padding(2).Row(r =>
                    {
                        r.RelativeItem().Text(t => t.Span("Total").Bold().FontSize(10));
                        r.RelativeItem().AlignRight().Text(t => t.Span(invoiceDto.NetTotal.ToString("N2")).Bold().FontSize(10));
                    });
                });

                // Signature Lines
                column.Item().PaddingTop(15).Row(row =>
                {
                    row.RelativeItem().Text(t => t.Span("Customer Signature").FontSize(6).Underline());
                    row.RelativeItem().AlignRight().Text(t => t.Span("Q.p. Signature").FontSize(6).Underline());
                });
            });
        });
    }).GeneratePdf();
    }
}