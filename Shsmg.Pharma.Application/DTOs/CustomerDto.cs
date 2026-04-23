namespace Shsmg.Pharma.Application.DTOs;

/// <summary>
/// DTO for transferring customer data to UI layer.
/// </summary>
public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public int Type { get; set; } // 0=WalkIn, 1=Registered, 2=Corporate
    public decimal CreditLimit { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal LifetimeValue { get; set; }
    public int InvoiceCount { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
    public AddressDto? BillingAddress { get; set; }
    public AddressDto? ShippingAddress { get; set; }
    public PatientInfoDto? PatientInfo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }
}

/// <summary>
/// DTO for address data transfer.
/// </summary>
public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string? Street2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "India";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

/// <summary>
/// DTO for patient information transfer.
/// </summary>
public class PatientInfoDto
{
    public int? Age { get; set; }
    public char? Gender { get; set; }
    public string? GSTIN { get; set; }
    public string? AadharNumber { get; set; }
    public string? PanNumber { get; set; }
    public string? DoctorName { get; set; }
    public string? MedicalNotes { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new customer.
/// </summary>
public class CreateCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public int Type { get; set; } // 0=WalkIn, 1=Registered, 2=Corporate
    public AddressDto? BillingAddress { get; set; }
    public AddressDto? ShippingAddress { get; set; }
    public PatientInfoDto? PatientInfo { get; set; }
}

/// <summary>
/// DTO for updating customer details.
/// </summary>
public class UpdateCustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public AddressDto? BillingAddress { get; set; }
    public AddressDto? ShippingAddress { get; set; }
    public PatientInfoDto? PatientInfo { get; set; }
}

/// <summary>
/// DTO for assigning credit limit (Admin only).
/// </summary>
public class AssignCreditLimitDto
{
    public Guid CustomerId { get; set; }
    public decimal CreditLimit { get; set; }
}

/// <summary>
/// DTO for recording payment.
/// </summary>
public class RecordPaymentDto
{
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, Cheque, etc.
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for blacklisting customer.
/// </summary>
public class BlacklistCustomerDto
{
    public Guid CustomerId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// DTO for customer search results with pagination.
/// </summary>
public class CustomerListDto
{
    public IEnumerable<CustomerDto> Customers { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// DTO for customer credit status.
/// </summary>
public class CustomerCreditStatusDto
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal AvailableCredit { get; set; }
    public bool CanBuyOnCredit { get; set; }
}
