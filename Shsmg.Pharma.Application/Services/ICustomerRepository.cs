using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.Services;

/// <summary>
/// Repository interface for Customer aggregate root.
/// Provides domain-driven data access patterns for customer management.
/// 
/// DDD Pattern Notes:
/// - Repository abstracts persistence details
/// - Methods return domain entities (Customer) not DTOs
/// - Queries are expressed in domain language (phone number, type)
/// - Mutation methods (Create, Update) work with full aggregate
/// </summary>
public interface ICustomerRepository
{
    /// <summary>
    /// Retrieves a customer by their unique ID.
    /// Returns null if customer not found.
    /// </summary>
    Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a customer by phone number (primary identifier for walk-ins).
    /// Phone number is unique within customer type (multiple customers with same phone possible, but unusual).
    /// Returns first match or null if not found.
    /// </summary>
    Task<Customer?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a customer by phone number and type (more specific than GetByPhoneAsync).
    /// Useful for "customer found" workflow: check if phone already exists as Registered customer.
    /// </summary>
    Task<Customer?> GetByPhoneAndTypeAsync(string phoneNumber, CustomerType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all customers of a specific type (WalkIn, Registered, Corporate).
    /// Paginated for performance on large datasets.
    /// </summary>
    Task<(IEnumerable<Customer> Customers, int TotalCount)> GetByTypeAsync(
        CustomerType type,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for customers by name (partial match, case-insensitive).
    /// Returns paginated results sorted by name.
    /// </summary>
    Task<(IEnumerable<Customer> Customers, int TotalCount)> SearchByNameAsync(
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all customers with outstanding balance (positive OutstandingAmount).
    /// Used for collection follow-ups and aging analysis.
    /// </summary>
    Task<IEnumerable<Customer>> GetWithOutstandingBalanceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all blacklisted customers.
    /// </summary>
    Task<IEnumerable<Customer>> GetBlacklistedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves customers who haven't made a purchase in specified days.
    /// Used for inactivity analysis and re-engagement campaigns.
    /// </summary>
    Task<IEnumerable<Customer>> GetInactiveAsync(int daysSinceLastPurchase, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers by credit status: high-value (lifetime > threshold), active credit users, etc.
    /// </summary>
    Task<(IEnumerable<Customer> Customers, int TotalCount)> GetByCreditStatusAsync(
        decimal minCreditLimit,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new customer in the database.
    /// Called after business logic validation in the Customer aggregate.
    /// </summary>
    Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing customer.
    /// Overwrites all properties except Id, CreatedAt, CreatedBy (base entity tracking).
    /// </summary>
    Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a customer (sets IsDeleted flag).
    /// Invoice records are preserved for audit/compliance.
    /// </summary>
    Task<bool> DeleteAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the total lifetime value across all customers.
    /// Used for dashboard analytics.
    /// </summary>
    Task<decimal> GetTotalLifetimeValueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts customers by type for dashboard/reporting.
    /// </summary>
    Task<Dictionary<CustomerType, int>> CountByTypeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a phone number already exists (useful before creating new customer).
    /// </summary>
    Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes to the database.
    /// Called by the Unit of Work pattern.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    IQueryable<Customer> GetAll();
}
