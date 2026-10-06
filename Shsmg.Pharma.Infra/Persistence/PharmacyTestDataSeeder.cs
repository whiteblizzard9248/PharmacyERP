using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Infra.Persistence;

public static class PharmacyTestDataSeeder
{
    public static async Task SeedAsync(PharmacyDbContext context, CancellationToken cancellationToken = default)
    {
        if (CheckIfCompanyExists(context, cancellationToken)) return;
        await context.Database.MigrateAsync(cancellationToken);

        var now = DateTime.UtcNow;

        await UpsertCompanyAsync(context, now, cancellationToken);
        var suppliers = await UpsertSuppliersAsync(context, now, cancellationToken);
        var inventory = await UpsertInventoryAsync(context, now, cancellationToken);
        var customers = await UpsertCustomersAsync(context, now, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        var purchaseInvoices = await UpsertPurchaseInvoicesAsync(context, suppliers, inventory, now, cancellationToken);
        await UpsertPaymentsAsync(context, suppliers, purchaseInvoices, now, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        var invoices = await UpsertInvoicesAsync(context, customers, inventory, now, cancellationToken);
        await UpsertReceiptsAsync(context, customers, invoices, now, cancellationToken);
        await UpsertInvoiceAuditLogsAsync(context, invoices, now, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static bool CheckIfCompanyExists(PharmacyDbContext context, CancellationToken cancellationToken)
    {
        return context.Companies.Any();
    }

    private static async Task UpsertCompanyAsync(PharmacyDbContext context, DateTime now,
        CancellationToken cancellationToken)
    {
        const string licenseNumber = "KA-SMG-ERP-2026-001";
        var company =
            await context.Companies.FirstOrDefaultAsync(x => x.LicenseNumber == licenseNumber, cancellationToken);

        if (company == null)
        {
            company = new Company
            {
                Name = "Sirius Pharmacy Care",
                Address = "No. 14, Market Road, Shivamogga, Karnataka 577201",
                LicenseNumber = licenseNumber,
                ContactNumber = "+91-98860-12001",
                LicenseKey = "TEST-LICENSE-KEY",
                LicenseExpiry = now.AddYears(1),
                HardwareId = "TEST-HW-001",
                IsActivated = true,
                CreatedAt = now,
                CreatedBy = "seeder"
            };
            context.Companies.Add(company);
            return;
        }

        company.Name = "Sirius Pharmacy Care";
        company.Address = "No. 14, Market Road, Shivamogga, Karnataka 577201";
        company.ContactNumber = "+91-98860-12001";
        company.LicenseKey = "TEST-LICENSE-KEY";
        company.LicenseExpiry = now.AddYears(1);
        company.HardwareId = "TEST-HW-001";
        company.IsActivated = true;
        company.LastModified = now;
        company.LastModifiedBy = "seeder";
    }

    private static async Task<List<Supplier>> UpsertSuppliersAsync(PharmacyDbContext context, DateTime now,
        CancellationToken cancellationToken)
    {
        var items = new[]
        {
            new Supplier
            {
                Name = "Medico Distributors", ContactPerson = "A. Ramesh", PhoneNumber = "+91-98450-11001",
                Email = "sales@medicodistributors.test", Address = "Bengaluru, Karnataka",
                GstNumber = "29ABCDE1234F1Z5", OutstandingAmount = 12450m
            },
            new Supplier
            {
                Name = "HealthPlus Pharma", ContactPerson = "Nisha Rao", PhoneNumber = "+91-98450-11002",
                Email = "orders@healthpluspharma.test", Address = "Mysuru, Karnataka", GstNumber = "29ABCDE1234F1Z6",
                OutstandingAmount = 8600m
            },
            new Supplier
            {
                Name = "CurePoint Labs", ContactPerson = "Sanjay Kumar", PhoneNumber = "+91-98450-11003",
                Email = "dispatch@curepointlabs.test", Address = "Hubballi, Karnataka", GstNumber = "29ABCDE1234F1Z7",
                OutstandingAmount = 0m
            }
        };

        foreach (var seed in items)
        {
            var supplier =
                await context.Suppliers.FirstOrDefaultAsync(x => x.GstNumber == seed.GstNumber, cancellationToken);
            if (supplier == null)
            {
                seed.CreatedAt = now;
                seed.CreatedBy = "seeder";
                context.Suppliers.Add(seed);
            }
            else
            {
                supplier.Name = seed.Name;
                supplier.ContactPerson = seed.ContactPerson;
                supplier.PhoneNumber = seed.PhoneNumber;
                supplier.Email = seed.Email;
                supplier.Address = seed.Address;
                supplier.OutstandingAmount = seed.OutstandingAmount;
                supplier.LastModified = now;
                supplier.LastModifiedBy = "seeder";
            }
        }

        return await context.Suppliers.Where(x => items.Select(i => i.GstNumber).Contains(x.GstNumber))
            .ToListAsync(cancellationToken);
    }

    private static async Task<List<InventoryItem>> UpsertInventoryAsync(PharmacyDbContext context, DateTime now,
        CancellationToken cancellationToken)
    {
        var items = new[]
        {
            new InventoryItem
            {
                Description = "Paracetamol 650 mg Tablets", HsnCode = "3004", Package = "10's", Mfg = "Cipla",
                Batch = "PCM650A", ExpiryDate = "12/2027", QuantityInStock = 180, ReorderLevel = 60, Rate = 18m,
                GstPercentage = 12m
            },
            new InventoryItem
            {
                Description = "Amoxicillin 500 mg Capsules", HsnCode = "3004", Package = "10's", Mfg = "Alkem",
                Batch = "AMX500B", ExpiryDate = "09/2027", QuantityInStock = 120, ReorderLevel = 40, Rate = 92m,
                GstPercentage = 12m
            },
            new InventoryItem
            {
                Description = "Pantoprazole 40 mg Tablets", HsnCode = "3004", Package = "10's", Mfg = "Sun Pharma",
                Batch = "PAN40C", ExpiryDate = "08/2027", QuantityInStock = 95, ReorderLevel = 30, Rate = 74m,
                GstPercentage = 12m
            },
            new InventoryItem
            {
                Description = "ORS Powder Sachet", HsnCode = "3004", Package = "1's", Mfg = "GlaxoSmithKline",
                Batch = "ORS01D", ExpiryDate = "06/2027", QuantityInStock = 260, ReorderLevel = 80, Rate = 22m,
                GstPercentage = 5m
            },
            new InventoryItem
            {
                Description = "Cetirizine 10 mg Tablets", HsnCode = "3004", Package = "10's", Mfg = "Intas",
                Batch = "CET10E", ExpiryDate = "11/2027", QuantityInStock = 150, ReorderLevel = 50, Rate = 15m,
                GstPercentage = 12m
            }
        };

        foreach (var seed in items)
        {
            var existing = await context.InventoryItems.FirstOrDefaultAsync(x =>
                x.Description == seed.Description && x.Batch == seed.Batch, cancellationToken);

            if (existing == null)
            {
                seed.CreatedAt = now;
                seed.CreatedBy = "seeder";
                context.InventoryItems.Add(seed);
            }
            else
            {
                existing.HsnCode = seed.HsnCode;
                existing.Package = seed.Package;
                existing.Mfg = seed.Mfg;
                existing.ExpiryDate = seed.ExpiryDate;
                existing.QuantityInStock = seed.QuantityInStock;
                existing.ReorderLevel = seed.ReorderLevel;
                existing.Rate = seed.Rate;
                existing.GstPercentage = seed.GstPercentage;
                existing.LastModified = now;
                existing.LastModifiedBy = "seeder";
            }
        }

        return await context.InventoryItems.Where(x => items.Select(i => i.Batch).Contains(x.Batch))
            .ToListAsync(cancellationToken);
    }

    private static async Task<List<Customer>> UpsertCustomersAsync(PharmacyDbContext context, DateTime now,
        CancellationToken cancellationToken)
    {
        var items = new[]
        {
            new Customer
            {
                Name = "Ramesh Shetty",
                PhoneNumber = "9000000001",
                Email = "ramesh.shetty@test.local",
                Type = CustomerType.Registered,
                BillingAddress = Address.Create("12, 2nd Cross", "Shivamogga", "Karnataka", "577201", "Market Ward"),
                ShippingAddress = Address.Create("12, 2nd Cross", "Shivamogga", "Karnataka", "577201", "Market Ward"),
                PatientInfo = PatientInfo.Create(age: 42, gender: 'M', gstin: "29ABCDE1234F1Z8",
                    doctorName: "Dr. V. Kumar", medicalNotes: "Hypertension follow-up"),
                CreditLimit = 5000m,
                OutstandingAmount = 1250m,
                LifetimeValue = 18450m,
                InvoiceCount = 12,
                LastPurchaseDate = now.AddDays(-3)
            },
            new Customer
            {
                Name = "Anitha Rao",
                PhoneNumber = "9000000002",
                Email = "anitha.rao@test.local",
                Type = CustomerType.WalkIn,
                PatientInfo = PatientInfo.CreateEmpty(),
                LifetimeValue = 820m,
                InvoiceCount = 2,
                LastPurchaseDate = now.AddDays(-12)
            },
            new Customer
            {
                Name = "Sri Krishna Clinic",
                PhoneNumber = "9000000003",
                Email = "accounts@skrishnaclinic.test",
                Type = CustomerType.Corporate,
                BillingAddress = Address.Create("88, Hospital Road", "Shivamogga", "Karnataka", "577202"),
                PatientInfo = PatientInfo.Create(gstin: "29ABCDE1234F1Z9", doctorName: "Accounts Desk",
                    medicalNotes: "Corporate account"),
                CreditLimit = 25000m,
                OutstandingAmount = 7250m,
                LifetimeValue = 84250m,
                InvoiceCount = 31,
                LastPurchaseDate = now.AddDays(-1)
            }
        };

        foreach (var seed in items)
        {
            var existing =
                await context.Customers.FirstOrDefaultAsync(x => x.PhoneNumber == seed.PhoneNumber, cancellationToken);
            if (existing == null)
            {
                seed.CreatedAt = now;
                seed.CreatedBy = "seeder";
                context.Customers.Add(seed);
            }
            else
            {
                existing.Name = seed.Name;
                existing.Email = seed.Email;
                existing.Type = seed.Type;
                existing.BillingAddress = seed.BillingAddress;
                existing.ShippingAddress = seed.ShippingAddress;
                existing.PatientInfo = seed.PatientInfo;
                existing.CreditLimit = seed.CreditLimit;
                existing.OutstandingAmount = seed.OutstandingAmount;
                existing.LifetimeValue = seed.LifetimeValue;
                existing.InvoiceCount = seed.InvoiceCount;
                existing.LastPurchaseDate = seed.LastPurchaseDate;
                existing.LastModified = now;
                existing.LastModifiedBy = "seeder";
            }
        }

        return await context.Customers.Where(x => items.Select(i => i.PhoneNumber).Contains(x.PhoneNumber!))
            .ToListAsync(cancellationToken);
    }

    private static async Task<List<PurchaseInvoice>> UpsertPurchaseInvoicesAsync(
        PharmacyDbContext context,
        List<Supplier> suppliers,
        List<InventoryItem> inventory,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var supplier = suppliers.Single(x => x.GstNumber == "29ABCDE1234F1Z5");
        var inventoryA = inventory.Single(x => x.Batch == "PCM650A");
        var inventoryB = inventory.Single(x => x.Batch == "AMX500B");

        var items = new[]
        {
            new PurchaseInvoice
            {
                PurchaseInvoiceNumber = "PI-2026-0001",
                SupplierInvoiceNumber = "MED-77881",
                PurchaseDate = now.AddDays(-14),
                SupplierId = supplier.Id,
                Notes = "Initial stock fill for common OTC and antibiotics",
                GrossTotal = 19800m,
                TaxTotal = 2376m,
                NetTotal = 22176m,
                Items =
                [
                    new PurchaseInvoiceItem
                    {
                        InventoryItemId = inventoryA.Id, Description = inventoryA.Description,
                        HsnCode = inventoryA.HsnCode, Package = inventoryA.Package, Mfg = inventoryA.Mfg,
                        Batch = inventoryA.Batch, ExpiryDate = inventoryA.ExpiryDate, Quantity = 300, Rate = 15m,
                        GstPercentage = 12m
                    },
                    new PurchaseInvoiceItem
                    {
                        InventoryItemId = inventoryB.Id, Description = inventoryB.Description,
                        HsnCode = inventoryB.HsnCode, Package = inventoryB.Package, Mfg = inventoryB.Mfg,
                        Batch = inventoryB.Batch, ExpiryDate = inventoryB.ExpiryDate, Quantity = 100, Rate = 82m,
                        GstPercentage = 12m
                    }
                ]
            }
        };

        foreach (var seed in items)
        {
            var existing = await context.PurchaseInvoices.Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.PurchaseInvoiceNumber == seed.PurchaseInvoiceNumber, cancellationToken);

            if (existing == null)
            {
                seed.CreatedAt = now;
                seed.CreatedBy = "seeder";
                foreach (var item in seed.Items)
                {
                    item.CreatedAt = now;
                    item.CreatedBy = "seeder";
                }

                context.PurchaseInvoices.Add(seed);
            }
            else
            {
                existing.SupplierId = seed.SupplierId;
                existing.SupplierInvoiceNumber = seed.SupplierInvoiceNumber;
                existing.PurchaseDate = seed.PurchaseDate;
                existing.Notes = seed.Notes;
                existing.GrossTotal = seed.GrossTotal;
                existing.TaxTotal = seed.TaxTotal;
                existing.NetTotal = seed.NetTotal;
                existing.LastModified = now;
                existing.LastModifiedBy = "seeder";
            }
        }

        return await context.PurchaseInvoices.Include(x => x.Items)
            .Where(x => items.Select(i => i.PurchaseInvoiceNumber).Contains(x.PurchaseInvoiceNumber))
            .ToListAsync(cancellationToken);
    }

    private static async Task UpsertPaymentsAsync(
        PharmacyDbContext context,
        List<Supplier> suppliers,
        List<PurchaseInvoice> purchaseInvoices,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var supplier = suppliers.Single(x => x.GstNumber == "29ABCDE1234F1Z5");
        var purchaseInvoice = purchaseInvoices.Single(x => x.PurchaseInvoiceNumber == "PI-2026-0001");

        var seed = new Payment
        {
            PaymentNumber = "PAY-2026-0001",
            PaymentDate = now.AddDays(-10),
            SupplierId = supplier.Id,
            PurchaseInvoiceId = purchaseInvoice.Id,
            Amount = 5000m,
            PaymentMethod = "NEFT",
            ReferenceNumber = "UTR20260814001",
            Notes = "Partial settlement for initial stock"
        };

        var existing =
            await context.Payments.FirstOrDefaultAsync(x => x.PaymentNumber == seed.PaymentNumber, cancellationToken);
        if (existing == null)
        {
            seed.CreatedAt = now;
            seed.CreatedBy = "seeder";
            context.Payments.Add(seed);
        }
        else
        {
            existing.SupplierId = seed.SupplierId;
            existing.PurchaseInvoiceId = seed.PurchaseInvoiceId;
            existing.PaymentDate = seed.PaymentDate;
            existing.Amount = seed.Amount;
            existing.PaymentMethod = seed.PaymentMethod;
            existing.ReferenceNumber = seed.ReferenceNumber;
            existing.Notes = seed.Notes;
            existing.LastModified = now;
            existing.LastModifiedBy = "seeder";
        }
    }

    private static async Task<List<Invoice>> UpsertInvoicesAsync(
        PharmacyDbContext context,
        List<Customer> customers,
        List<InventoryItem> inventory,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var customerA = customers.Single(x => x.PhoneNumber == "9000000001");
        var customerB = customers.Single(x => x.PhoneNumber == "9000000002");
        var inventoryA = inventory.Single(x => x.Batch == "PCM650A");
        var inventoryC = inventory.Single(x => x.Batch == "PAN40C");
        var inventoryD = inventory.Single(x => x.Batch == "ORS01D");

        var items = new[]
        {
            new Invoice
            {
                InvoiceNumber = "INV-2026-0001",
                InvoiceDate = now.AddDays(-2),
                PatientName = "Ramesh Shetty",
                DoctorName = "Dr. V. Kumar",
                CustomerId = customerA.Id,
                GrossTotal = 420m,
                TaxTotal = 50.40m,
                NetTotal = 470.40m,
                Items =
                [
                    new InvoiceItem
                    {
                        InventoryItemId = inventoryA.Id, Description = inventoryA.Description,
                        HsnCode = inventoryA.HsnCode, Package = inventoryA.Package, Mfg = inventoryA.Mfg,
                        Batch = inventoryA.Batch, ExpiryDate = inventoryA.ExpiryDate, Quantity = 6, Rate = 18m,
                        GstPercentage = 12m
                    },
                    new InvoiceItem
                    {
                        InventoryItemId = inventoryC.Id, Description = inventoryC.Description,
                        HsnCode = inventoryC.HsnCode, Package = inventoryC.Package, Mfg = inventoryC.Mfg,
                        Batch = inventoryC.Batch, ExpiryDate = inventoryC.ExpiryDate, Quantity = 2, Rate = 74m,
                        GstPercentage = 12m
                    }
                ]
            },
            new Invoice
            {
                InvoiceNumber = "INV-2026-0002",
                InvoiceDate = now.AddDays(-1),
                PatientName = "Anitha Rao",
                DoctorName = "Dr. S. Patil",
                CustomerId = customerB.Id,
                GrossTotal = 176m,
                TaxTotal = 14.08m,
                NetTotal = 190.08m,
                Items =
                [
                    new InvoiceItem
                    {
                        InventoryItemId = inventoryD.Id, Description = inventoryD.Description,
                        HsnCode = inventoryD.HsnCode, Package = inventoryD.Package, Mfg = inventoryD.Mfg,
                        Batch = inventoryD.Batch, ExpiryDate = inventoryD.ExpiryDate, Quantity = 8, Rate = 22m,
                        GstPercentage = 8m
                    }
                ]
            }
        };

        foreach (var seed in items)
        {
            var existing = await context.Invoices.Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.InvoiceNumber == seed.InvoiceNumber, cancellationToken);

            if (existing == null)
            {
                seed.CreatedAt = now;
                seed.CreatedBy = "seeder";
                foreach (var item in seed.Items)
                {
                    item.CreatedAt = now;
                    item.CreatedBy = "seeder";
                }

                context.Invoices.Add(seed);
            }
            else
            {
                existing.CustomerId = seed.CustomerId;
                existing.InvoiceDate = seed.InvoiceDate;
                existing.PatientName = seed.PatientName;
                existing.DoctorName = seed.DoctorName;
                existing.GrossTotal = seed.GrossTotal;
                existing.TaxTotal = seed.TaxTotal;
                existing.NetTotal = seed.NetTotal;
                existing.LastModified = now;
                existing.LastModifiedBy = "seeder";
            }
        }

        return items.ToList();
    }

    private static async Task UpsertReceiptsAsync(
        PharmacyDbContext context,
        List<Customer> customers,
        List<Invoice> invoices,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var customer = customers.Single(x => x.PhoneNumber == "9000000001");
        var invoice = invoices.Single(x => x.InvoiceNumber == "INV-2026-0001");

        var seed = new Receipt
        {
            ReceiptNumber = "REC-2026-0001",
            ReceiptDate = now.AddDays(-1),
            CustomerId = customer.Id,
            InvoiceId = invoice.Id,
            Amount = 200m,
            PaymentMethod = "UPI",
            ReferenceNumber = "UPI20260818001",
            Notes = "Part payment against invoice INV-2026-0001"
        };

        var existing =
            await context.Receipts.FirstOrDefaultAsync(x => x.ReceiptNumber == seed.ReceiptNumber, cancellationToken);
        if (existing == null)
        {
            seed.CreatedAt = now;
            seed.CreatedBy = "seeder";
            context.Receipts.Add(seed);
        }
        else
        {
            existing.CustomerId = seed.CustomerId;
            existing.InvoiceId = seed.InvoiceId;
            existing.ReceiptDate = seed.ReceiptDate;
            existing.Amount = seed.Amount;
            existing.PaymentMethod = seed.PaymentMethod;
            existing.ReferenceNumber = seed.ReferenceNumber;
            existing.Notes = seed.Notes;
            existing.LastModified = now;
            existing.LastModifiedBy = "seeder";
        }
    }

    private static async Task UpsertInvoiceAuditLogsAsync(
        PharmacyDbContext context,
        List<Invoice> invoices,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var invoice = invoices.Single(x => x.InvoiceNumber == "INV-2026-0001");
        var seed = new InvoiceAuditLog
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Action = "Created",
            Summary = "Seeded sale for common OTC and prescription items",
            SnapshotJson = "{\"source\":\"seed\",\"type\":\"invoice\"}",
            PerformedAt = now.AddDays(-2),
            PerformedBy = "seeder"
        };

        var exists = await context.InvoiceAuditLogs.AnyAsync(x =>
            x.InvoiceId == seed.InvoiceId &&
            x.Action == seed.Action &&
            x.PerformedAt == seed.PerformedAt, cancellationToken);

        if (!exists)
        {
            context.InvoiceAuditLogs.Add(seed);
        }
    }
}