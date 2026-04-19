using Microsoft.VisualBasic;
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
                    column.Item().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(companyDto.Name).Bold().FontSize(12);
                                col.Item().Text(companyDto.Address).FontSize(8);
                            });

                            row.RelativeItem(1)
                                .AlignRight()
                                .Text($"No. {invoiceDto.InvoiceNumber}")
                                .FontColor(Colors.Red.Medium)
                                .Bold()
                                .FontSize(7);
                        });

                        header.Item().AlignCenter()
                            .Text($"DL No. {companyDto.LicenseNumber} | M. {companyDto.ContactNumber}")
                            .FontSize(7);

                        header.Item().LineHorizontal(1).LineColor(primaryBlue);

                        header.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Patient: {invoiceDto.PatientName}");
                            row.ConstantItem(120).AlignRight().Text($"Date: {invoiceDto.Date:dd/MM/yyyy}");
                        });

                        header.Item().Text($"Doctor: {invoiceDto.DoctorName}");
                    });

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
}