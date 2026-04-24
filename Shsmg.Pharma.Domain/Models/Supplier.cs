namespace Shsmg.Pharma.Domain.Models;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string GstNumber { get; set; } = string.Empty;

    public List<PurchaseInvoice> PurchaseInvoices { get; set; } = [];
}
