using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shsmg.Pharma.Application.Services;

namespace Shsmg.Pharma.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseInvoiceService, PurchaseInvoiceService>();
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IPaymentService, PaymentService>();
        return services;
    }
}
