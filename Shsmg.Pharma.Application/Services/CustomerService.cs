using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Application.DTOs;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.Services;

/// <summary>
/// Implementation of customer management service.
/// Handles business logic for customer operations, validation, and data transformation.
/// </summary>
public class CustomerService(ICustomerRepository repository) : ICustomerService
{
    private readonly ICustomerRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid customerId)
    {
        var customer = await _repository.GetByIdAsync(customerId);
        return customer != null ? MapToDto(customer) : null;
    }

    public async Task<CustomerDto?> GetCustomerByPhoneAsync(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        var customer = await _repository.GetByPhoneAsync(phoneNumber);
        return customer != null ? MapToDto(customer) : null;
    }

    public async Task<CustomerDto?> GetCustomerByPhoneAndTypeAsync(string phoneNumber, int customerType)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        var type = (CustomerType)customerType;
        var customer = await _repository.GetByPhoneAndTypeAsync(phoneNumber, type);
        return customer != null ? MapToDto(customer) : null;
    }

    public async Task<CustomerListDto> GetCustomersByTypeAsync(int type, int pageNumber = 1, int pageSize = 100)
    {
        var customerType = (CustomerType)type;
        var (customers, totalCount) = await _repository.GetByTypeAsync(customerType, pageNumber, pageSize);

        return new CustomerListDto
        {
            Customers = customers.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<CustomerListDto> SearchCustomersByNameAsync(string searchTerm, int pageNumber = 1, int pageSize = 100)
    {
        var (customers, totalCount) = await _repository.SearchByNameAsync(searchTerm ?? string.Empty, pageNumber, pageSize);

        return new CustomerListDto
        {
            Customers = customers.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<CustomerDto>> GetCustomersWithOutstandingBalanceAsync()
    {
        var customers = await _repository.GetWithOutstandingBalanceAsync();
        return customers.Select(MapToDto);
    }

    public async Task<IEnumerable<CustomerDto>> GetBlacklistedCustomersAsync()
    {
        var customers = await _repository.GetBlacklistedAsync();
        return customers.Select(MapToDto);
    }

    public async Task<IEnumerable<CustomerDto>> GetInactiveCustomersAsync(int daysSinceLastPurchase)
    {
        var customers = await _repository.GetInactiveAsync(daysSinceLastPurchase);
        return customers.Select(MapToDto);
    }

    public async Task<CustomerListDto> GetCustomersByCreditStatusAsync(decimal minCreditLimit, int pageNumber = 1, int pageSize = 100)
    {
        var (customers, totalCount) = await _repository.GetByCreditStatusAsync(minCreditLimit, pageNumber, pageSize);

        return new CustomerListDto
        {
            Customers = customers.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<CustomerCreditStatusDto?> GetCreditStatusAsync(Guid customerId)
    {
        var customer = await _repository.GetByIdAsync(customerId);
        if (customer == null)
            return null;

        var (available, canBuy) = customer.GetCreditStatus();

        return new CustomerCreditStatusDto
        {
            CustomerId = customer.Id,
            Name = customer.Name,
            CreditLimit = customer.CreditLimit,
            OutstandingAmount = customer.OutstandingAmount,
            AvailableCredit = available,
            CanBuyOnCredit = canBuy
        };
    }

    public async Task<CustomerDashboardStatsDto> GetDashboardStatsAsync()
    {
        var counts = await _repository.CountByTypeAsync();
        var blacklisted = await _repository.GetBlacklistedAsync();
        var outstanding = await _repository.GetWithOutstandingBalanceAsync();
        var totalLifetime = await _repository.GetTotalLifetimeValueAsync();

        var totalOutstanding = outstanding.Sum(c => c.OutstandingAmount);
        var customersCount = counts[CustomerType.WalkIn] + counts[CustomerType.Registered] + counts[CustomerType.Corporate];
        var avgOutstanding = customersCount > 0 ? totalOutstanding / customersCount : 0;

        return new CustomerDashboardStatsDto
        {
            TotalCustomers = customersCount,
            WalkInCount = counts[CustomerType.WalkIn],
            RegisteredCount = counts[CustomerType.Registered],
            CorporateCount = counts[CustomerType.Corporate],
            BlacklistedCount = blacklisted.Count(),
            TotalLifetimeValue = totalLifetime,
            TotalOutstandingBalance = totalOutstanding,
            AverageOutstandingPerCustomer = avgOutstanding,
            CustomersWithBalance = outstanding.Count(),
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<Guid> CreateCustomerAsync(CreateCustomerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Customer name is required.", nameof(dto.Name));
        }

        var type = (CustomerType)dto.Type;

        // Business rule: Walk-in customers don't require billing address
        // Registered customers require billing address and patient info
        if (type == CustomerType.Registered)
        {
            if (dto.BillingAddress == null)
                throw new ArgumentException("Billing address is required for registered customers.");
            if (dto.PatientInfo == null)
                throw new ArgumentException("Patient information is required for registered customers.");
        }

        // Create domain entity
        var customer = new Customer
        {
            Name = dto.Name.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            Type = type
        };

        // Map address DTOs to domain value objects
        if (dto.BillingAddress != null)
        {
            customer.BillingAddress = Address.Create(
                dto.BillingAddress.Street,
                dto.BillingAddress.City,
                dto.BillingAddress.State,
                dto.BillingAddress.PostalCode,
                dto.BillingAddress.Street2,
                dto.BillingAddress.Country ?? "India",
                dto.BillingAddress.Latitude,
                dto.BillingAddress.Longitude
            );
        }

        if (dto.ShippingAddress != null)
        {
            customer.ShippingAddress = Address.Create(
                dto.ShippingAddress.Street,
                dto.ShippingAddress.City,
                dto.ShippingAddress.State,
                dto.ShippingAddress.PostalCode,
                dto.ShippingAddress.Street2,
                dto.ShippingAddress.Country ?? "India",
                dto.ShippingAddress.Latitude,
                dto.ShippingAddress.Longitude
            );
        }

        // Map patient info DTO to domain value object
        if (dto.PatientInfo != null)
        {
            customer.PatientInfo = PatientInfo.Create(
                dto.PatientInfo.Age,
                dto.PatientInfo.Gender,
                dto.PatientInfo.GSTIN,
                dto.PatientInfo.AadharNumber,
                dto.PatientInfo.PanNumber,
                dto.PatientInfo.DoctorName,
                dto.PatientInfo.MedicalNotes
            );
        }
        else if (type == CustomerType.Registered)
        {
            customer.PatientInfo = PatientInfo.CreateEmpty();
        }

        // Save to database
        var created = await _repository.CreateAsync(customer);
        return created.Id;
    }

    public async Task<bool> UpdateCustomerAsync(UpdateCustomerDto dto)
    {
        var customer = await _repository.GetByIdAsync(dto.Id);
        if (customer == null)
            return false;

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Customer name is required.", nameof(dto.Name));

        customer.Name = dto.Name.Trim();
        customer.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();

        // Update addresses
        if (dto.BillingAddress != null)
        {
            customer.BillingAddress = Address.Create(
                dto.BillingAddress.Street,
                dto.BillingAddress.City,
                dto.BillingAddress.State,
                dto.BillingAddress.PostalCode,
                dto.BillingAddress.Street2,
                dto.BillingAddress.Country ?? "India"
            );
        }

        if (dto.ShippingAddress != null)
        {
            customer.ShippingAddress = Address.Create(
                dto.ShippingAddress.Street,
                dto.ShippingAddress.City,
                dto.ShippingAddress.State,
                dto.ShippingAddress.PostalCode,
                dto.ShippingAddress.Street2,
                dto.ShippingAddress.Country ?? "India"
            );
        }

        // Update patient info
        if (dto.PatientInfo != null)
        {
            customer.PatientInfo = PatientInfo.Create(
                dto.PatientInfo.Age,
                dto.PatientInfo.Gender,
                dto.PatientInfo.GSTIN,
                dto.PatientInfo.AadharNumber,
                dto.PatientInfo.PanNumber,
                dto.PatientInfo.DoctorName,
                dto.PatientInfo.MedicalNotes
            );
        }

        await _repository.UpdateAsync(customer);
        return true;
    }

    public async Task<bool> DeleteCustomerAsync(Guid customerId)
    {
        return await _repository.DeleteAsync(customerId);
    }

    public async Task<bool> AssignCreditLimitAsync(AssignCreditLimitDto dto)
    {
        var customer = await _repository.GetByIdAsync(dto.CustomerId);
        if (customer == null)
            return false;

        if (customer.Type == CustomerType.WalkIn)
            throw new InvalidOperationException("Cannot assign credit limit to walk-in customers.");

        customer.AssignCreditLimit(dto.CreditLimit);
        await _repository.UpdateAsync(customer);
        return true;
    }

    public async Task<bool> RecordPaymentAsync(RecordPaymentDto dto)
    {
        var customer = await _repository.GetByIdAsync(dto.CustomerId);
        if (customer == null)
            return false;

        customer.RecordPayment(dto.Amount);
        await _repository.UpdateAsync(customer);
        return true;
    }

    public async Task<bool> RecordPurchaseAsync(Guid customerId, decimal amount, Guid invoiceId)
    {
        var customer = await _repository.GetByIdAsync(customerId);
        if (customer == null)
        {
            return false; // Return false if customer doesn't exist for graceful handling
        }

        try
        {
            customer.RecordPurchase(amount);
            customer.LastPurchaseDate = DateTime.UtcNow;
            await _repository.UpdateAsync(customer);
            return true;
        }
        catch (Exception ex)
        {
            // Log the exception and return false to allow caller to handle it
            return false;
        }
    }

    public async Task<bool> BlacklistCustomerAsync(BlacklistCustomerDto dto)
    {
        var customer = await _repository.GetByIdAsync(dto.CustomerId);
        if (customer == null)
            return false;

        customer.Blacklist(dto.Reason);
        await _repository.UpdateAsync(customer);
        return true;
    }

    public async Task<bool> RemoveBlacklistAsync(Guid customerId)
    {
        var customer = await _repository.GetByIdAsync(customerId);
        if (customer == null)
            return false;

        customer.RemoveFromBlacklist();
        await _repository.UpdateAsync(customer);
        return true;
    }

    public async Task<bool> ConvertToRegisteredAsync(Guid customerId, AddressDto billingAddress, PatientInfoDto patientInfo)
    {
        var customer = await _repository.GetByIdAsync(customerId);
        if (customer == null)
            return false;

        var address = Address.Create(
            billingAddress.Street,
            billingAddress.City,
            billingAddress.State,
            billingAddress.PostalCode,
            billingAddress.Street2,
            billingAddress.Country ?? "India"
        );

        var patient = PatientInfo.Create(
            patientInfo.Age,
            patientInfo.Gender,
            patientInfo.GSTIN,
            patientInfo.AadharNumber,
            patientInfo.PanNumber,
            patientInfo.DoctorName,
            patientInfo.MedicalNotes
        );

        customer.ConvertToRegistered(address, patient);
        await _repository.UpdateAsync(customer);
        return true;
    }

    public async Task<bool> ConvertToCorporateAsync(Guid customerId, AddressDto billingAddress, string gstin)
    {
        var customer = await _repository.GetByIdAsync(customerId);
        if (customer == null)
            return false;

        var address = Address.Create(
            billingAddress.Street,
            billingAddress.City,
            billingAddress.State,
            billingAddress.PostalCode,
            billingAddress.Street2,
            billingAddress.Country ?? "India"
        );

        customer.ConvertToCorporate(address, gstin);
        await _repository.UpdateAsync(customer);
        return true;
    }

    public async Task<bool> CanPurchaseAsync(Guid customerId)
    {
        var customer = await _repository.GetByIdAsync(customerId);
        return customer?.CanPurchase() ?? false;
    }

    public async Task<bool> PhoneNumberExistsAsync(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        return await _repository.ExistsByPhoneAsync(phoneNumber);
    }

    public async Task<CustomerListDto> GetCustomersAsync(CustomerQuery request, CancellationToken cancellationToken = default)
    {
        var query = _repository.GetAll();

        // 1. Filtering
        query = ApplyFilters(query, request);

        // 2. Search
        query = ApplySearch(query, request.Search);

        // 3. Count BEFORE paging
        var totalCount = await query.CountAsync(cancellationToken);

        // 4. Sorting (generic)
        query = ApplySorting(query, request.SortBy, request.SortOrder);

        // 5. Paging
        var customers = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new CustomerListDto
        {
            Customers = [.. customers.Select(MapToDto)],
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    // ==================== PRIVATE HELPERS ====================

    private static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Type = (int)customer.Type,
            CreditLimit = customer.CreditLimit,
            OutstandingAmount = customer.OutstandingAmount,
            LifetimeValue = customer.LifetimeValue,
            InvoiceCount = customer.InvoiceCount,
            IsBlacklisted = customer.IsBlacklisted,
            BlacklistReason = customer.BlacklistReason,
            LastPurchaseDate = customer.LastPurchaseDate,
            BillingAddress = customer.BillingAddress != null ? MapAddressToDto(customer.BillingAddress) : null,
            ShippingAddress = customer.ShippingAddress != null ? MapAddressToDto(customer.ShippingAddress) : null,
            PatientInfo = customer.PatientInfo != null ? MapPatientInfoToDto(customer.PatientInfo) : null,
            CreatedAt = customer.CreatedAt,
            LastModified = customer.LastModified
        };
    }

    private static AddressDto MapAddressToDto(Address address)
    {
        return new AddressDto
        {
            Street = address.Street,
            Street2 = address.Street2,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country,
            Latitude = address.Latitude,
            Longitude = address.Longitude
        };
    }

    private static PatientInfoDto MapPatientInfoToDto(PatientInfo patientInfo)
    {
        return new PatientInfoDto
        {
            Age = patientInfo.Age,
            Gender = patientInfo.Gender,
            GSTIN = patientInfo.GSTIN,
            AadharNumber = patientInfo.AadharNumber,
            PanNumber = patientInfo.PanNumber,
            DoctorName = patientInfo.DoctorName,
            MedicalNotes = patientInfo.MedicalNotes,
            LastUpdatedAt = patientInfo.LastUpdatedAt
        };
    }


    private IQueryable<Customer> ApplyFilters(
        IQueryable<Customer> query,
        CustomerQuery request)
    {
        if (request.Type.HasValue)
        {
            var type = (CustomerType)request.Type.Value;
            query = query.Where(c => c.Type == type);
        }

        if (request.IsBlacklisted.HasValue)
        {
            query = query.Where(c => c.IsBlacklisted == request.IsBlacklisted.Value);
        }

        if (request.MinOutstanding.HasValue)
        {
            query = query.Where(c => c.OutstandingAmount >= request.MinOutstanding.Value);
        }

        if (request.MaxOutstanding.HasValue)
        {
            query = query.Where(c => c.OutstandingAmount <= request.MaxOutstanding.Value);
        }

        return query;
    }

    private IQueryable<Customer> ApplySearch(
    IQueryable<Customer> query,
    string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        search = search.Trim().ToLower();

        return query.Where(c =>
            c.Name.ToLower().Contains(search) ||
            (c.PhoneNumber != null && c.PhoneNumber.Contains(search)) ||
            (c.Email != null && c.Email.ToLower().Contains(search))
        );
    }

    private static readonly Dictionary<string, Expression<Func<Customer, object>>> SortMap
    = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = c => c.Name,
        ["phone"] = c => c.PhoneNumber!,
        ["type"] = c => c.Type,
        ["outstanding"] = c => c.OutstandingAmount,
        ["credit"] = c => c.CreditLimit,
        ["lastpurchase"] = c => c.LastPurchaseDate!
    };

    private IQueryable<Customer> ApplySorting(
    IQueryable<Customer> query,
    string? sortBy,
    string? sortOrder)
    {
        if (string.IsNullOrWhiteSpace(sortBy) || !SortMap.ContainsKey(sortBy))
            return query.OrderBy(c => c.Name); // default

        var keySelector = SortMap[sortBy];

        return string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

}
