namespace Shsmg.Pharma.Domain.Models;

/// <summary>
/// Customer aggregate root for pharmacy ERP system.
/// Manages customer relationships, billing, and regulatory compliance.
/// 
/// Supports three customer types:
/// - WalkIn: One-time customers with minimal data collection (phone optional)
/// - Registered: Ongoing customers with full compliance data
/// - Corporate: Institutional customers with special terms
/// 
/// Strategy: Use phone number as unique identifier for walk-ins (enables "customer found" workflow).
/// For registered customers, maintain full address and patient information.
/// </summary>
public class Customer : BaseEntity
{
    /// <summary>
    /// Full name of the customer (required).
    /// For walk-ins: Often collected at point of sale.
    /// For registered: Used for official records and prescriptions.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact phone number (optional for walk-ins, required for registered).
    /// Acts as unique identifier within customer type (used for "customer found" workflow).
    /// Enables SMS notifications, appointment reminders, and outreach.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Email address (optional).
    /// Future use: Invoice delivery, promotional campaigns, prescription updates.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Type of customer: WalkIn, Registered, or Corporate.
    /// Determines billing behavior, credit limits, and compliance requirements.
    /// </summary>
    public CustomerType Type { get; set; } = CustomerType.WalkIn;

    /// <summary>
    /// Billing/primary address (required for Registered and Corporate customers).
    /// Embedded value object that includes street, city, state, postal code.
    /// Optional for walk-in customers.
    /// </summary>
    public Address? BillingAddress { get; set; }

    /// <summary>
    /// Alternate/shipping address (optional).
    /// Used when delivery address differs from billing address.
    /// </summary>
    public Address? ShippingAddress { get; set; }

    /// <summary>
    /// Patient information for regulatory compliance (required for Registered customers).
    /// Includes age, gender, GSTIN, Aadhar, medical notes.
    /// Value object - immutable for audit trail.
    /// Empty for walk-in customers (can be populated later if they register).
    /// </summary>
    public PatientInfo? PatientInfo { get; set; }

    /// <summary>
    /// Credit limit for this customer (default: 0 for walk-ins, configurable for Registered).
    /// Controls whether customer can purchase on credit.
    /// Updated by managers/admins.
    /// </summary>
    public decimal CreditLimit { get; set; } = decimal.Zero;

    /// <summary>
    /// Outstanding balance (negative = amount owed).
    /// Updated after each invoice creation/payment.
    /// Critical for credit decisions and collection follow-up.
    /// </summary>
    public decimal OutstandingAmount { get; set; } = decimal.Zero;

    /// <summary>
    /// Total lifetime purchases in rupees (for analytics).
    /// Aggregate of all completed invoices.
    /// </summary>
    public decimal LifetimeValue { get; set; } = decimal.Zero;

    /// <summary>
    /// Number of invoices associated with this customer.
    /// Used for quick analytics and customer segmentation.
    /// </summary>
    public int InvoiceCount { get; set; } = 0;

    /// <summary>
    /// Whether customer is blacklisted (default: false).
    /// If true, customer cannot make purchases or use credit.
    /// </summary>
    public bool IsBlacklisted { get; set; } = false;

    /// <summary>
    /// Reason for blacklisting (e.g., "Fraud", "Non-payment", "Policy violation").
    /// Required if IsBlacklisted is true.
    /// </summary>
    public string? BlacklistReason { get; set; }

    /// <summary>
    /// Date when customer record was last purchased from.
    /// Used to identify inactive customers for outreach.
    /// </summary>
    public DateTime? LastPurchaseDate { get; set; }

    /// <summary>
    /// Navigation property: All invoices for this customer.
    /// Populated by EF Core for querying related invoices.
    /// </summary>
    public List<Invoice> Invoices { get; set; } = [];

    // ==================== BUSINESS LOGIC METHODS ====================

    /// <summary>
    /// Validates if customer can make a purchase (not blacklisted, auth complete if Registered).
    /// </summary>
    public bool CanPurchase()
    {
        if (IsBlacklisted)
            return false;

        // For registered customers, require billing address and patient info
        if (Type == CustomerType.Registered)
        {
            return BillingAddress != null && PatientInfo != null;
        }

        // For walk-in, only requires name
        return !string.IsNullOrWhiteSpace(Name);
    }

    /// <summary>
    /// Checks if customer has sufficient credit for a transaction.
    /// </summary>
    public bool HasSufficientCredit(decimal amount)
    {
        if (Type == CustomerType.WalkIn)
            return false; // Walk-ins don't use credit

        var availableCredit = CreditLimit - Math.Abs(OutstandingAmount);
        return availableCredit >= amount;
    }

    /// <summary>
    /// Records a purchase/invoice creation. Updates balance and metrics.
    /// Should be called by InvoiceService after invoice creation.
    /// </summary>
    public void RecordPurchase(decimal amount)
    {
        if (!CanPurchase())
            throw new InvalidOperationException($"Customer {Name} cannot make purchases.");

        OutstandingAmount += amount;
        LifetimeValue += amount;
        InvoiceCount += 1;
        LastPurchaseDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a payment against outstanding balance.
    /// Call this after payment processing (cash, card, cheque).
    /// </summary>
    public void RecordPayment(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be positive.", nameof(amount));

        OutstandingAmount -= amount;

        // Prevent negative balance (overpayment) - track as credit
        if (OutstandingAmount < 0)
        {
            OutstandingAmount = Math.Abs(OutstandingAmount);
            // TODO: Track credit balance separately for refunds
        }
    }

    /// <summary>
    /// Assigns or updates credit limit for this customer.
    /// Should be called only by Admin/Manager roles.
    /// </summary>
    public void AssignCreditLimit(decimal limit)
    {
        if (limit < 0)
            throw new ArgumentException("Credit limit cannot be negative.", nameof(limit));

        if (Type == CustomerType.WalkIn)
            throw new InvalidOperationException("Walk-in customers cannot have credit limits.");

        CreditLimit = limit;
    }

    /// <summary>
    /// Converts walk-in customer to registered with full details.
    /// Used in "customer found" workflow when phone matches an existing registered customer,
    /// or when walk-in decides to complete registration.
    /// </summary>
    public void ConvertToRegistered(Address billingAddress, PatientInfo patientInfo)
    {
        if (billingAddress == null)
            throw new ArgumentNullException(nameof(billingAddress));
        if (patientInfo == null)
            throw new ArgumentNullException(nameof(patientInfo));

        if (Type == CustomerType.Registered)
            throw new InvalidOperationException("Customer is already registered.");

        Type = CustomerType.Registered;
        BillingAddress = billingAddress;
        PatientInfo = patientInfo;
    }

    /// <summary>
    /// Registers this customer as a corporate account.
    /// </summary>
    public void ConvertToCorporate(Address billingAddress, string gstin)
    {
        if (billingAddress == null)
            throw new ArgumentNullException(nameof(billingAddress));
        if (string.IsNullOrWhiteSpace(gstin))
            throw new ArgumentException("GSTIN is required for corporate accounts.", nameof(gstin));

        if (Type == CustomerType.Corporate)
            throw new InvalidOperationException("Customer is already corporate.");

        Type = CustomerType.Corporate;
        BillingAddress = billingAddress;

        // Create PatientInfo equivalent for corporate (using GSTIN)
        PatientInfo = PatientInfo.Create(gstin: gstin);
    }

    /// <summary>
    /// Blacklists customer (prevents purchases and credit usage).
    /// </summary>
    public void Blacklist(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required for blacklisting.", nameof(reason));

        IsBlacklisted = true;
        BlacklistReason = reason.Trim();
    }

    /// <summary>
    /// Removes blacklist status, allowing purchases again.
    /// </summary>
    public void RemoveFromBlacklist()
    {
        IsBlacklisted = false;
        BlacklistReason = null;
    }

    /// <summary>
    /// Gets customer's credit status for display/reporting.
    /// </summary>
    public (decimal AvailableCredit, bool CanBuyOnCredit) GetCreditStatus()
    {
        if (Type == CustomerType.WalkIn)
            return (decimal.Zero, false);

        var available = CreditLimit - Math.Abs(OutstandingAmount);
        var canBuy = available > 0 && !IsBlacklisted;
        return (available, canBuy);
    }
}
