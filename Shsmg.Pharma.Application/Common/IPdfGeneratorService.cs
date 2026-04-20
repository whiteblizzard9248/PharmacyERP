using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Common;


public interface IPdfGeneratorService
{
    byte[] GeneratePdf(InvoiceDetailDto invoiceDto, CompanyDto companyDto);
    byte[] GenerateCombinedPdf(List<InvoiceDetailDto> invoices, CompanyDto companyDto);
}