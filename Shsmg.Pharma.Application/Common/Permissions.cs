namespace Shsmg.Pharma.Application.Common;

public static class Permissions
{
    public const string CompanyView = "Company.View";
    public const string CompanyEdit = "Company.Edit";

    public const string InvoiceView = "Invoice.View";
    public const string InvoiceCreate = "Invoice.Create";
    public const string InvoiceEdit = "Invoice.Edit";
    public const string InvoiceDelete = "Invoice.Delete";

    public const string UserManage = "User.Manage";
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";

    public static readonly IReadOnlyDictionary<string, string[]> RolePermissions = new Dictionary<string, string[]>
    {
        [Admin] = [
            Permissions.CompanyView,
            Permissions.CompanyEdit,
            Permissions.InvoiceView,
            Permissions.InvoiceCreate,
            Permissions.InvoiceEdit,
            Permissions.InvoiceDelete,
            Permissions.UserManage
        ],
        [Manager] = [
            Permissions.CompanyView,
            Permissions.CompanyEdit,
            Permissions.InvoiceView,
            Permissions.InvoiceCreate,
            Permissions.InvoiceEdit,
            Permissions.InvoiceDelete,
            Permissions.UserManage
        ],
        [Employee] = [
            Permissions.CompanyView,
            Permissions.InvoiceCreate,
            Permissions.InvoiceEdit,
            Permissions.InvoiceView
        ]
    };
}