using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Infra.Services;

public class PdfGeneratorService : IPdfGeneratorService
{
    public byte[] GeneratePdf(InvoiceDetailDto invoiceDto, CompanyDto companyDto)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                // A4 full page
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.PageColor(Colors.White);

                var primaryBlue = "#002D62";

                page.DefaultTextStyle(x => x.FontSize(8).FontColor(primaryBlue));

                // Content limited to ~45% height
                page.Content().Height(400).Column(column =>
                {
                    // ================= HEADER =================
                    column.Item().PaddingBottom(5).Row(row =>
{
    // Left Section: Pharmacy Branding & License
    row.RelativeItem(1.2f).Column(col =>
    {
        col.Item().Text(companyDto.Name).Bold().FontSize(12).FontColor(Colors.Blue.Medium);
        col.Item().Text(companyDto.Address).FontSize(7).LineHeight(1f);
        col.Item().Text($"M. {companyDto.ContactNumber}").FontSize(7);
        col.Item().Text($"DL No. {companyDto.LicenseNumber}").FontSize(7);
    });

    // Right Section: Invoice Metadata
    row.RelativeItem(1f).Column(col =>
    {
        // Invoice No and Date on one line
        col.Item().Row(r =>
        {
            r.RelativeItem().Text($"No. {invoiceDto.InvoiceNumber}").FontColor(Colors.Red.Medium).Bold().FontSize(10);
            r.RelativeItem().AlignRight().Text($"Date: {invoiceDto.Date:dd/MM/yyyy}").FontSize(7);
        });

        col.Item().PaddingTop(5).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingBottom(1).Text(t =>
        {
            t.Span("Patient Name: ").FontSize(7);
            t.Span(invoiceDto.PatientName).FontSize(7).Bold();
        });

        col.Item().PaddingTop(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingBottom(1).Text(t =>
        {
            t.Span("Doctor: ").FontSize(7);
            t.Span(invoiceDto.DoctorName).FontSize(7).Bold();
        });
    });
});

                    // The horizontal line separating the header from the table
                    column.Item().LineHorizontal(1).LineColor(primaryBlue);

                    // ================= TABLE =================
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25); // Qty
                            columns.RelativeColumn();   // Description
                            columns.ConstantColumn(35); // HSN
                            columns.ConstantColumn(35); // Pkg
                            columns.ConstantColumn(40); // Mfg
                            columns.ConstantColumn(50); // Batch
                            columns.ConstantColumn(40); // Exp
                            columns.ConstantColumn(30); // GST %
                            columns.ConstantColumn(60); // Amount
                        });

                        // ===== HEADER =====
                        table.Header(header =>
                        {
                            header.Cell().Element(Cell).Text("Qty");
                            header.Cell().Element(Cell).Text("Description");
                            header.Cell().Element(Cell).Text("HSN");
                            header.Cell().Element(Cell).Text("Pkg");
                            header.Cell().Element(Cell).Text("Mfg");
                            header.Cell().Element(Cell).Text("Batch");
                            header.Cell().Element(Cell).Text("Exp");
                            header.Cell().Element(Cell).Text("GST%");
                            header.Cell().Element(Cell).AlignRight().Text("Amount");

                            static IContainer Cell(IContainer c) =>
                                c.Border(0.5f).BorderColor("#002D62")
                                 .Padding(2)
                                 .AlignCenter()
                                 .DefaultTextStyle(x => x.Bold().FontSize(7));
                        });

                        // ===== ROWS =====
                        foreach (var item in invoiceDto.Items)
                        {
                            table.Cell().Element(Row).Text(item.Quantity.ToString());
                            table.Cell().Element(Row).Text(item.Description);
                            table.Cell().Element(Row).Text(item.HsnCode);
                            table.Cell().Element(Row).Text(item.Package);
                            table.Cell().Element(Row).Text(item.Mfg);
                            table.Cell().Element(Row).Text(item.Batch);
                            table.Cell().Element(Row).Text(item.ExpiryDate);
                            table.Cell().Element(Row).Text(item.GstPercentage.ToString("0.##"));
                            table.Cell().Element(Row).AlignRight().Text(item.LineTotal.ToString("0.00"));

                            static IContainer Row(IContainer c) => c.Border(0.5f).BorderColor("#002D62").Padding(2).DefaultTextStyle(x => x.FontSize(7)).ShowEntire();
                        }
                    });

                    // ================= FOOTER =================
                    column.Item().Row(row =>
                    {
                        row.RelativeItem()
                           .Text("Goods once sold cannot be taken back or exchanged")
                           .FontSize(7);

                        row.ConstantItem(120)
                            .Border(0.5f)
                            .BorderColor(primaryBlue)
                            .Padding(5)
                            // Use Row instead of Column to keep text on the same line
                            .Row(footerRow =>
                            {
                                footerRow.RelativeItem().Text("Total").Bold();

                                footerRow.RelativeItem().AlignRight()
                                    .Text(invoiceDto.NetTotal.ToString("0.00"))
                                    .Bold();
                            });
                    });

                    column.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Text("Customer Signature").FontSize(7);
                        row.RelativeItem().AlignRight().Text("Q.P. Signature").FontSize(7);
                    });
                });
            });
        }).GeneratePdf();
    }

    public byte[] GenerateCombinedPdf(List<InvoiceDetailDto> invoices, CompanyDto companyDto)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(10);
                page.PageColor(Colors.White);

                var primaryBlue = "#002D62";
                page.DefaultTextStyle(x => x.FontSize(7).FontColor(primaryBlue));

                page.Content().Column(mainColumn =>
                {
                    // Display up to 3 invoices in a column layout
                    for (int i = 0; i < invoices.Count && i < 3; i++)
                    {
                        var invoice = invoices[i];

                        // Invoice section
                        mainColumn.Item().Column(invoiceSection =>
                        {
                            // HEADER
                            invoiceSection.Item().Row(row =>
{
    // Left Side: Pharmacy Name, Address, and Licenses
    row.RelativeItem(1.5f).Column(col =>
    {
        col.Item().Text(companyDto.Name).Bold().FontSize(12).FontColor(Colors.Blue.Medium);
        col.Item().Text(companyDto.Address).FontSize(8);
        col.Item().Text($"M. {companyDto.ContactNumber}").FontSize(8);
        col.Item().Text($"DL No. {companyDto.LicenseNumber}").FontSize(8);
    });

    // Right Side: Invoice No, Date, Patient, and Doctor
    row.RelativeItem(1f).Column(col =>
    {
        col.Item().Row(headerRow =>
        {
            headerRow.RelativeItem().Text($"No. {invoice.InvoiceNumber}").FontColor(Colors.Red.Medium).Bold().FontSize(10);
            headerRow.RelativeItem().AlignRight().Text($"Date: {invoice.Date:dd/MM/yyyy}").FontSize(8);
        });

        col.Item().PaddingTop(5).Text(t =>
        {
            t.Span("Patient Name: ").FontSize(8);
            t.Span(invoice.PatientName).FontSize(8);
        });

        col.Item().PaddingTop(2).Text(t =>
        {
            t.Span("Doctor: ").FontSize(8);
            t.Span(invoice.DoctorName).FontSize(8);
        });
    });
});

                            // Optional: Add a thin horizontal line before the table
                            invoiceSection.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            // TABLE
                            invoiceSection.Item().PaddingTop(3).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(18); // Qty
                                    columns.RelativeColumn(2);   // Description
                                    columns.ConstantColumn(25); // HSN
                                    columns.ConstantColumn(25); // Pkg
                                    columns.ConstantColumn(25); // Amount
                                });

                                // HEADER
                                table.Header(header =>
                                {
                                    header.Cell().Element(Cell).Text("Qty");
                                    header.Cell().Element(Cell).Text("Description");
                                    header.Cell().Element(Cell).Text("HSN");
                                    header.Cell().Element(Cell).Text("Pkg");
                                    header.Cell().Element(Cell).AlignRight().Text("Amount");


                                    static IContainer Cell(IContainer c) =>
                                        c.Border(0.3f).BorderColor("#002D62")
                                         .Padding(1)
                                         .AlignCenter()
                                         .DefaultTextStyle(x => x.Bold().FontSize(6));
                                });

                                // ROWS
                                foreach (var item in invoice.Items)
                                {
                                    table.Cell().Element(Row).Text(item.Quantity.ToString());
                                    table.Cell().Element(Row).Text(item.Description);
                                    table.Cell().Element(Row).Text(item.HsnCode);
                                    table.Cell().Element(Row).Text(item.Package);
                                    table.Cell().Element(Row).AlignRight().Text(item.LineTotal.ToString("0.00"));

                                    static IContainer Row(IContainer c) => c.Border(0.3f).BorderColor("#002D62").Padding(1).DefaultTextStyle(x => x.FontSize(6));
                                }
                            });

                            // FOOTER
                            invoiceSection.Item().PaddingTop(2).Row(row =>
                            {
                                row.RelativeItem().Text("Goods once sold cannot be taken back");

                                row.ConstantItem(70)
                                    .Border(0.3f)
                                    .BorderColor(primaryBlue)
                                    .Padding(2)
                                    .Row(footerRow =>
                                    {
                                        footerRow.RelativeItem().Text("Total").Bold();
                                        footerRow.RelativeItem().AlignRight()
                                            .Text(invoice.NetTotal.ToString("0.00"))
                                            .Bold();
                                    });
                            });

                            // Divider between invoices
                            if (i < invoices.Count - 1)
                            {
                                invoiceSection.Item().PaddingVertical(3).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                            }
                        });
                    }
                });
            });
        }).GeneratePdf();
    }
}