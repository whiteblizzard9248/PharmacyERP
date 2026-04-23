using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Services;

/// <summary>
/// Application service for customer management.
/// Provides business logic for customer CRUD operations, credit management, and reporting.
/// Acts as a facade over the ICustomerRepository with domain-level validation.
/// </summary>
public interface ICustomerService
{
    // ==================== QUERIES ====================

    Task<CustomerListDto> GetCustomersAsync(CustomerQuery request, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a customer by ID.
    /// </summary>
    Task<CustomerDto?> GetCustomerByIdAsync(Guid customerId);

    /// <summary>
    /// Retrieves a customer by phone number.
    /// Used in "customer found" workflow.
    /// </summary>
    Task<CustomerDto?> GetCustomerByPhoneAsync(string phoneNumber);

    /// <summary>
    /// Retrieves a customer by phone number and type.
    /// </summary>
    Task<CustomerDto?> GetCustomerByPhoneAndTypeAsync(string phoneNumber, int customerType);

    /// <summary>
    /// Gets paginated list of customers by type.
    /// </summary>
    Task<CustomerListDto> GetCustomersByTypeAsync(int type, int pageNumber = 1, int pageSize = 100);

    /// <summary>
    /// Searches customers by name with pagination.
    /// </summary>
    Task<CustomerListDto> SearchCustomersByNameAsync(string searchTerm, int pageNumber = 1, int pageSize = 100);

    /// <summary>
    /// Gets customers with outstanding balance (for collection follow-ups).
    /// </summary>
    Task<IEnumerable<CustomerDto>> GetCustomersWithOutstandingBalanceAsync();

    /// <summary>
    /// Gets blacklisted customers.
    /// </summary>
    Task<IEnumerable<CustomerDto>> GetBlacklistedCustomersAsync();

    /// <summary>
    /// Gets inactive customers (no purchase in specified days).
    /// </summary>
    Task<IEnumerable<CustomerDto>> GetInactiveCustomersAsync(int daysSinceLastPurchase);

    /// <summary>
    /// Gets customers by credit status with pagination.
    /// </summary>
    Task<CustomerListDto> GetCustomersByCreditStatusAsync(decimal minCreditLimit, int pageNumber = 1, int pageSize = 100);

    /// <summary>
    /// Gets credit status for a customer.
    /// </summary>
    Task<CustomerCreditStatusDto?> GetCreditStatusAsync(Guid customerId);

    /// <summary>
    /// Gets dashboard statistics for customers.
    /// </summary>
    Task<CustomerDashboardStatsDto> GetDashboardStatsAsync();

    // ==================== COMMANDS ====================

    /// <summary>
    /// Creates a new customer.
    /// Validates business rules and initializes default values.
    /// </summary>
    Task<Guid> CreateCustomerAsync(CreateCustomerDto dto);

    /// <summary>
    /// Updates customer details.
    /// Cannot change phone number or customer type (immutable).
    /// </summary>
    Task<bool> UpdateCustomerAsync(UpdateCustomerDto dto);

    /// <summary>
    /// Deletes (soft delete) a customer record.
    /// Preserves invoices for audit trail.
    /// </summary>
    Task<bool> DeleteCustomerAsync(Guid customerId);

    /// <summary>
    /// Assigns or updates credit limit for a customer.
    /// Admin only operation.
    /// </summary>
    Task<bool> AssignCreditLimitAsync(AssignCreditLimitDto dto);

    /// <summary>
    /// Records a payment against customer's outstanding balance.
    /// </summary>
    Task<bool> RecordPaymentAsync(RecordPaymentDto dto);

    /// <summary>
    /// Records a purchase/invoice creation.
    /// Updates customer balance and metrics.
    /// </summary>
    Task<bool> RecordPurchaseAsync(Guid customerId, decimal amount, Guid invoiceId);

    /// <summary>
    /// Blacklists a customer (prevents purchases and credit usage).
    /// </summary>
    Task<bool> BlacklistCustomerAsync(BlacklistCustomerDto dto);

    /// <summary>
    /// Removes blacklist status.
    /// </summary>
    Task<bool> RemoveBlacklistAsync(Guid customerId);

    /// <summary>
    /// Converts a walk-in customer to registered.
    /// </summary>
    Task<bool> ConvertToRegisteredAsync(Guid customerId, AddressDto billingAddress, PatientInfoDto patientInfo);

    /// <summary>
    /// Converts a customer to corporate account.
    /// </summary>
    Task<bool> ConvertToCorporateAsync(Guid customerId, AddressDto billingAddress, string gstin);

    /// <summary>
    /// Checks if customer can make a purchase.
    /// </summary>
    Task<bool> CanPurchaseAsync(Guid customerId);

    /// <summary>
    /// Checks if phone number already exists.
    /// </summary>
    Task<bool> PhoneNumberExistsAsync(string phoneNumber);
}

/// <summary>
/// DTO for customer dashboard statistics.
/// </summary>
public class CustomerDashboardStatsDto
{
    public int TotalCustomers { get; set; }
    public int WalkInCount { get; set; }
    public int RegisteredCount { get; set; }
    public int CorporateCount { get; set; }
    public int BlacklistedCount { get; set; }
    public decimal TotalLifetimeValue { get; set; }
    public decimal TotalOutstandingBalance { get; set; }
    public decimal AverageOutstandingPerCustomer { get; set; }
    public int CustomersWithBalance { get; set; }
    public DateTime LastUpdated { get; set; }
}
