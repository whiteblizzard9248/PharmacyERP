namespace Shsmg.Pharma.Domain.Models;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string GstNumber { get; set; } = string.Empty;
    public decimal OutstandingAmount { get; set; } = decimal.Zero;

    public List<PurchaseInvoice> PurchaseInvoices { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];

    public void RecordPurchase(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Purchase amount must be positive.", nameof(amount));

        OutstandingAmount += amount;
    }

    public void RecordPayment(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be positive.", nameof(amount));

        OutstandingAmount = Math.Max(0, OutstandingAmount - amount);
    }
}
