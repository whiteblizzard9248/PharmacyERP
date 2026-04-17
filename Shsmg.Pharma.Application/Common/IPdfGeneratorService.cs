using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Common;


public interface IPdfGeneratorService
{
    byte[] GeneratePdf(CreateInvoiceDto invoiceDto, CompanyDto companyDto);
}