using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Application.Services;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Infra.Persistence;

/// <summary>
/// Entity Framework Core implementation of Customer repository.
/// Provides data access for Customer aggregate root with optimized queries for common use cases.
/// </summary>
public class CustomerRepository(PharmacyDbContext context) : ICustomerRepository
{
    private readonly PharmacyDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
    }

    public async Task<Customer?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));

        return await _context.Customers
            .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<Customer?> GetByPhoneAndTypeAsync(string phoneNumber, CustomerType type, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));

        return await _context.Customers
            .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber && c.Type == type, cancellationToken);
    }

    public async Task<(IEnumerable<Customer> Customers, int TotalCount)> GetByTypeAsync(
        CustomerType type,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pageNumber, pageSize);

        var query = _context.Customers
            .Where(c => c.Type == type)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var customers = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (customers, totalCount);
    }

    public async Task<(IEnumerable<Customer> Customers, int TotalCount)> SearchByNameAsync(
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pageNumber, pageSize);

        if (string.IsNullOrWhiteSpace(searchTerm))
            searchTerm = "";

        var normalizedSearch = searchTerm.Trim().ToLower();
        var query = _context.Customers
            .Where(c => c.Name.ToLower().Contains(normalizedSearch))
            .OrderBy(c => c.Name);

        var totalCount = await query.CountAsync(cancellationToken);
        var customers = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (customers, totalCount);
    }

    public async Task<IEnumerable<Customer>> GetWithOutstandingBalanceAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .Where(c => c.OutstandingAmount > 0)
            .OrderByDescending(c => c.OutstandingAmount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetBlacklistedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .Where(c => c.IsBlacklisted)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetInactiveAsync(int daysSinceLastPurchase, CancellationToken cancellationToken = default)
    {
        if (daysSinceLastPurchase < 0)
            throw new ArgumentException("Days must be non-negative.", nameof(daysSinceLastPurchase));

        var cutoffDate = DateTime.UtcNow.AddDays(-daysSinceLastPurchase);

        return await _context.Customers
            .Where(c => c.Type == CustomerType.Registered && (c.LastPurchaseDate == null || c.LastPurchaseDate < cutoffDate))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Customer> Customers, int TotalCount)> GetByCreditStatusAsync(
        decimal minCreditLimit,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pageNumber, pageSize);

        if (minCreditLimit < 0)
            throw new ArgumentException("Credit limit cannot be negative.", nameof(minCreditLimit));

        var query = _context.Customers
            .Where(c => c.CreditLimit >= minCreditLimit)
            .OrderByDescending(c => c.CreditLimit);

        var totalCount = await query.CountAsync(cancellationToken);
        var customers = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (customers, totalCount);
    }

    public async Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        if (customer == null)
            throw new ArgumentNullException(nameof(customer));

        // Validate business rules
        if (string.IsNullOrWhiteSpace(customer.Name))
            throw new ArgumentException("Customer name is required.", nameof(customer));

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        if (customer == null)
            throw new ArgumentNullException(nameof(customer));

        var existing = await _context.Customers.FindAsync(new object[] { customer.Id }, cancellationToken);
        if (existing == null)
            throw new InvalidOperationException($"Customer with ID {customer.Id} not found.");

        // Update all properties (EF Core will track changes)
        _context.Entry(existing).CurrentValues.SetValues(customer);
        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FindAsync(new object[] { customerId }, cancellationToken);
        if (customer == null)
            return false;

        customer.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<decimal> GetTotalLifetimeValueAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .SumAsync(c => c.LifetimeValue, cancellationToken);
    }

    public async Task<Dictionary<CustomerType, int>> CountByTypeAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _context.Customers
            .GroupBy(c => c.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<CustomerType, int>
        {
            { CustomerType.WalkIn, 0 },
            { CustomerType.Registered, 0 },
            { CustomerType.Corporate, 0 }
        };

        foreach (var item in counts)
        {
            result[item.Type] = item.Count;
        }

        return result;
    }

    public async Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        return await _context.Customers
            .AnyAsync(c => c.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    private static void ValidatePagination(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be at least 1.", nameof(pageNumber));

        if (pageSize < 1 || pageSize > 1000)
            throw new ArgumentException("Page size must be between 1 and 1000.", nameof(pageSize));
    }

    public IQueryable<Customer> GetAll()
    {
        return _context.Customers.AsQueryable();
    }
}
