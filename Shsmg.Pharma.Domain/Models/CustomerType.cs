namespace Shsmg.Pharma.Domain.Models;

/// <summary>
/// Represents the type of customer in the pharmacy system.
/// Determines behavior for billing, credit limits, and regulatory requirements.
/// </summary>
public enum CustomerType
{
    /// <summary>
    /// One-time customer purchasing without ongoing relationship.
    /// Requires phone for follow-up communication only.
    /// No credit limit, no outstanding balance tracking.
    /// </summary>
    WalkIn = 0,

    /// <summary>
    /// Registered customer with ongoing relationship.
    /// Full regulatory compliance (GSTIN, patient info, address).
    /// Can have credit limits and outstanding balances.
    /// Prescription history tracking available.
    /// </summary>
    Registered = 1,

    /// <summary>
    /// Corporate/institutional customer (clinic, hospital, NGO).
    /// Bulk purchase agreements and negotiated credit terms.
    /// Compliance and audit requirements.
    /// </summary>
    Corporate = 2
}
