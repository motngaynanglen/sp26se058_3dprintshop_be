using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Infrastructure.Data;

/// <summary>
/// Idempotent mock-data seeder for demo / smoke-test runs. Triggered manually via
/// <c>POST /api/dev/seed-mock</c>. Run nhiều lần đều an toàn: bỏ qua mọi entity đã tồn tại theo Username/Code/Name.
/// </summary>
public class MockDataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordService _password;
    private readonly ILogger<MockDataSeeder> _logger;

    private const string SystemUser = "system-seeder";
    private const string DemoPassword = "Pass@123";

    public MockDataSeeder(
        ApplicationDbContext db,
        IPasswordService password,
        ILogger<MockDataSeeder> logger)
    {
        _db = db;
        _password = password;
        _logger = logger;
    }

    public async Task<MockSeedResult> SeedAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var result = new MockSeedResult();

        var accounts = await SeedAccountsAsync(now, result, ct);
        var materials = await SeedMaterialsAsync(now, result, ct);
        var concepts = await SeedConceptTagsAsync(now, result, ct);
        var templates = await SeedDesignTemplatesAsync(materials, concepts, now, result, ct);

        await SeedShippingAddressesAsync(accounts.Customers, now, result, ct);
        await SeedSampleOrdersAsync(accounts, templates, now, result, ct);

        result.PasswordForDemoUsers = DemoPassword;
        _logger.LogInformation("MockDataSeeder finished: {Result}", result);
        return result;
    }

    private async Task<SeededAccounts> SeedAccountsAsync(DateTimeOffset now, MockSeedResult result, CancellationToken ct)
    {
        var seeded = new SeededAccounts();

        seeded.Manager = await EnsureAccountAsync(new AccountSeedSpec(
            Username: "manager01", Fullname: "Nguyễn Quản Lý", Email: "manager01@demo.io",
            Phone: "0900000001", Role: Roles.MANAGER), now, result, ct);

        var staffSpecs = new[]
        {
            new AccountSeedSpec("staff01", "Trần Hoàng Anh", "staff01@demo.io", "0900000010", Roles.STAFF),
            new AccountSeedSpec("staff02", "Lê Thị Mai", "staff02@demo.io", "0900000011", Roles.STAFF),
        };
        foreach (var spec in staffSpecs)
            seeded.Staff.Add(await EnsureAccountAsync(spec, now, result, ct));

        var customerSpecs = new[]
        {
            new AccountSeedSpec("customer01", "Phạm Khánh Linh", "customer01@demo.io", "0911111101", Roles.CUSTOMER),
            new AccountSeedSpec("customer02", "Đặng Minh Tú",   "customer02@demo.io", "0911111102", Roles.CUSTOMER),
            new AccountSeedSpec("customer03", "Bùi Thanh Hà",   "customer03@demo.io", "0911111103", Roles.CUSTOMER),
        };
        foreach (var spec in customerSpecs)
            seeded.Customers.Add(await EnsureAccountAsync(spec, now, result, ct));

        await _db.SaveChangesAsync(ct);
        return seeded;
    }

    private async Task<AccountBundle> EnsureAccountAsync(AccountSeedSpec spec, DateTimeOffset now, MockSeedResult result, CancellationToken ct)
    {
        var existing = await _db.Accounts
            .Include(a => a.Customer)
            .Include(a => a.Staff)
            .Include(a => a.Manager)
            .FirstOrDefaultAsync(a => a.Username == spec.Username, ct);

        if (existing != null)
        {
            return new AccountBundle(
                existing,
                existing.Customer?.Id,
                existing.Staff?.Id,
                existing.Manager?.Id);
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Username = spec.Username,
            Fullname = spec.Fullname,
            Email = spec.Email,
            ContactPhone = spec.Phone,
            PasswordHash = _password.HashPassword(DemoPassword),
            IsActive = true,
            Created = now,
            CreatedBy = SystemUser,
            LastModified = now,
            LastModifiedBy = SystemUser,
        };
        _db.Accounts.Add(account);

        Guid? customerId = null, staffId = null, managerId = null;
        if (spec.Role == Roles.CUSTOMER)
        {
            var c = new Customer
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Created = now,
                CreatedBy = SystemUser,
                LastModified = now,
                LastModifiedBy = SystemUser,
            };
            _db.Customers.Add(c);
            customerId = c.Id;
        }
        else if (spec.Role == Roles.STAFF)
        {
            var s = new Staff
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Created = now,
                CreatedBy = SystemUser,
                LastModified = now,
                LastModifiedBy = SystemUser,
            };
            _db.Staffs.Add(s);
            staffId = s.Id;
        }
        else if (spec.Role == Roles.MANAGER)
        {
            var m = new Manager
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Created = now,
                CreatedBy = SystemUser,
                LastModified = now,
                LastModifiedBy = SystemUser,
            };
            _db.Managers.Add(m);
            managerId = m.Id;
        }

        result.AccountsCreated++;
        return new AccountBundle(account, customerId, staffId, managerId);
    }

    private async Task<List<Material>> SeedMaterialsAsync(DateTimeOffset now, MockSeedResult result, CancellationToken ct)
    {
        var specs = new (string Name, string Desc, decimal BaseCost, decimal TotalCost, decimal StockGrams)[]
        {
            ("PLA", "Nhựa PLA cơ bản — dễ in, không độc, phù hợp đồ trang trí.", 350m, 800m, 120_000m),
            ("PETG", "Nhựa PETG — bền hơn PLA, chịu nhiệt tốt hơn.", 480m, 950m, 80_000m),
            ("ABS", "Nhựa ABS — chịu nhiệt và lực tốt, in công nghiệp.", 520m, 1050m, 35_000m),
            ("Resin Tiêu chuẩn", "Resin tiêu chuẩn — độ phân giải cao, chi tiết tinh xảo.", 1200m, 2200m, 100_000m),
        };

        var materials = new List<Material>();
        foreach (var (name, desc, baseCost, totalCost, stockGrams) in specs)
        {
            var existing = await _db.Materials
                .Include(m => m.PriceHistories)
                .FirstOrDefaultAsync(m => m.Name == name, ct);
            if (existing != null)
            {
                materials.Add(existing);
                continue;
            }

            var material = new Material
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = desc,
                IsActive = true,
                StockQuantityGrams = stockGrams,
                Created = now,
                CreatedBy = SystemUser,
                LastModified = now,
                LastModifiedBy = SystemUser,
            };
            _db.Materials.Add(material);

            _db.MaterialPriceHistories.Add(new MaterialPriceHistory
            {
                Id = Guid.NewGuid(),
                MaterialId = material.Id,
                BaseCostPerGram = baseCost,
                TotalServiceCostPerGram = totalCost,
                EffectiveDate = DateTime.UtcNow.AddDays(-30),
                IsCurrent = true,
                Created = now,
                CreatedBy = SystemUser,
                LastModified = now,
                LastModifiedBy = SystemUser,
            });

            materials.Add(material);
            result.MaterialsCreated++;
        }

        await _db.SaveChangesAsync(ct);
        return materials;
    }

    private async Task<List<ConceptTag>> SeedConceptTagsAsync(DateTimeOffset now, MockSeedResult result, CancellationToken ct)
    {
        var specs = new (string Name, bool Main, string Desc)[]
        {
            ("Mô hình Anime",     true,  "Nhân vật anime / chibi / figure trang trí."),
            ("Đồ chơi trẻ em",    true,  "Đồ chơi an toàn cho trẻ em."),
            ("Móc khóa",          false, "Móc khóa cá nhân hoá."),
            ("Trang trí",         false, "Đồ trang trí bàn làm việc / nhà cửa."),
            ("Kiến trúc",         false, "Mô hình kiến trúc / sa bàn."),
            ("Sinh nhật",         false, "Quà tặng sinh nhật."),
        };

        var tags = new List<ConceptTag>();
        foreach (var (name, main, desc) in specs)
        {
            var existing = await _db.ConceptTags.FirstOrDefaultAsync(t => t.Name == name, ct);
            if (existing != null)
            {
                tags.Add(existing);
                continue;
            }

            var tag = new ConceptTag
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = desc,
                IsMainTag = main,
                IsActive = true,
                Created = now,
                CreatedBy = SystemUser,
                LastModified = now,
                LastModifiedBy = SystemUser,
            };
            _db.ConceptTags.Add(tag);
            tags.Add(tag);
            result.ConceptTagsCreated++;
        }

        await _db.SaveChangesAsync(ct);
        return tags;
    }

    private async Task<List<DesignTemplate>> SeedDesignTemplatesAsync(
        List<Material> materials,
        List<ConceptTag> concepts,
        DateTimeOffset now,
        MockSeedResult result,
        CancellationToken ct)
    {
        var astronaut = "https://modelviewer.dev/shared-assets/models/Astronaut.glb";
        var horse = "https://modelviewer.dev/shared-assets/models/Horse.glb";
        var duck = "https://modelviewer.dev/shared-assets/models/glTF-Sample-Assets/Models/Duck/glTF-Binary/Duck.glb";
        var avocado = "https://modelviewer.dev/shared-assets/models/glTF-Sample-Assets/Models/Avocado/glTF-Binary/Avocado.glb";

        var pla = materials.First(m => m.Name == "PLA");
        var petg = materials.First(m => m.Name == "PETG");
        var resin = materials.First(m => m.Name == "Resin Tiêu chuẩn");

        var animeTag = concepts.First(c => c.Name == "Mô hình Anime");
        var toysTag = concepts.First(c => c.Name == "Đồ chơi trẻ em");
        var deco = concepts.First(c => c.Name == "Trang trí");
        var keychain = concepts.First(c => c.Name == "Móc khóa");

        var specs = new[]
        {
            new TemplateSpec("DT-ASTRO", "Phi hành gia mini", "Mô hình phi hành gia trang trí — phù hợp làm quà tặng.", astronaut, astronaut,
                new[] { animeTag.Id, deco.Id },
                new[]
                {
                    new VariantSpec("V-ASTRO-PLA-S", "Phi hành gia S - PLA", "Bản size nhỏ, vật liệu PLA.", 199_000m, pla.Id, 25, 60m, 90m, true),
                    new VariantSpec("V-ASTRO-PETG-M", "Phi hành gia M - PETG", "Bản size trung, vật liệu PETG.", 349_000m, petg.Id, 12, 120m, 180m, true),
                }),
            new TemplateSpec("DT-DUCK", "Vịt cao su mini", "Mô hình vịt cao su vui nhộn — phù hợp đồ chơi trẻ em.", duck, duck,
                new[] { toysTag.Id, deco.Id, keychain.Id },
                new[]
                {
                    new VariantSpec("V-DUCK-PLA-S", "Vịt mini - PLA", "Đồ chơi nhỏ gọn, an toàn cho trẻ em.", 89_000m, pla.Id, 50, 35m, 45m, false),
                    new VariantSpec("V-DUCK-PETG-S", "Vịt mini - PETG", "Phiên bản PETG bền hơn.", 119_000m, petg.Id, 30, 35m, 50m, true),
                }),
            new TemplateSpec("DT-HORSE", "Ngựa hoang dã", "Mô hình ngựa phi nước đại — chi tiết cao.", horse, horse,
                new[] { animeTag.Id, deco.Id },
                new[]
                {
                    new VariantSpec("V-HORSE-PLA-M", "Ngựa - PLA", "Bản size M, in PLA.", 259_000m, pla.Id, 18, 95m, 150m, true),
                    new VariantSpec("V-HORSE-RESIN-S", "Ngựa - Resin", "Bản resin chi tiết siêu mịn.", 499_000m, resin.Id, 6, 70m, 280m, false),
                }),
            new TemplateSpec("DT-AVO", "Bơ trang trí", "Mô hình quả bơ kawaii — dễ thương.", avocado, avocado,
                new[] { deco.Id, keychain.Id },
                new[]
                {
                    new VariantSpec("V-AVO-PLA-S", "Bơ mini PLA", "Móc khoá / trang trí bàn.", 59_000m, pla.Id, 80, 20m, 35m, false),
                }),
        };

        var templates = new List<DesignTemplate>();
        foreach (var spec in specs)
        {
            var template = await _db.DesignTemplates
                .Include(t => t.Variants)
                .Include(t => t.DesignTags)
                .FirstOrDefaultAsync(t => t.Code == spec.Code, ct);

            if (template == null)
            {
                template = new DesignTemplate
                {
                    Id = Guid.NewGuid(),
                    Code = spec.Code,
                    Name = spec.Name,
                    Description = spec.Description,
                    FileUrl = spec.FileUrl,
                    ThumbnailUrl = spec.ThumbnailUrl,
                    IsActive = true,
                    Created = now,
                    CreatedBy = SystemUser,
                    LastModified = now,
                    LastModifiedBy = SystemUser,
                };
                _db.DesignTemplates.Add(template);
                result.DesignTemplatesCreated++;
            }

            foreach (var tagId in spec.ConceptTagIds)
            {
                var exists = await _db.DesignTags.AnyAsync(
                    dt => dt.ConceptTagId == tagId && dt.DesignTemplateId == template.Id, ct);
                if (exists) continue;
                _db.DesignTags.Add(new DesignTag
                {
                    Id = Guid.NewGuid(),
                    ConceptTagId = tagId,
                    DesignTemplateId = template.Id,
                    IsActive = true,
                    Created = now,
                    CreatedBy = SystemUser,
                    LastModified = now,
                    LastModifiedBy = SystemUser,
                });
            }

            foreach (var vspec in spec.Variants)
            {
                var variantExists = await _db.DesignVariants.AnyAsync(v => v.Code == vspec.Code, ct);
                if (variantExists) continue;

                _db.DesignVariants.Add(new DesignVariant
                {
                    Id = Guid.NewGuid(),
                    Code = vspec.Code,
                    Name = vspec.Name,
                    Description = vspec.Description,
                    Price = vspec.Price,
                    PreviewModelUrl = spec.FileUrl,
                    DesignTemplateId = template.Id,
                    MaterialId = vspec.MaterialId,
                    StockQuantity = vspec.Stock,
                    MinimumStockLevel = 2,
                    IsAllowPreOrder = vspec.AllowPreOrder,
                    EstimatedWeightPerUnit = vspec.WeightG,
                    EstimatedPrintTimePerUnit = vspec.PrintTimeMin,
                    IsActive = true,
                    Created = now,
                    CreatedBy = SystemUser,
                    LastModified = now,
                    LastModifiedBy = SystemUser,
                });
                result.DesignVariantsCreated++;
            }

            templates.Add(template);
        }

        await _db.SaveChangesAsync(ct);
        return templates;
    }

    private async Task SeedShippingAddressesAsync(List<AccountBundle> customers, DateTimeOffset now, MockSeedResult result, CancellationToken ct)
    {
        foreach (var bundle in customers)
        {
            if (bundle.CustomerId == null) continue;
            var hasAddress = await _db.ShippingAddresses.AnyAsync(s => s.CustomerId == bundle.CustomerId.Value, ct);
            if (hasAddress) continue;

            _db.ShippingAddresses.Add(new ShippingAddress
            {
                Id = Guid.NewGuid(),
                CustomerId = bundle.CustomerId.Value,
                Customer = null!,
                ReceiverName = bundle.Account.Fullname,
                Phone = bundle.Account.ContactPhone ?? "0900000000",
                AddressLine = "123 Nguyễn Văn Cừ",
                Ward = "Phường Bến Nghé",
                District = "Quận 1",
                City = "TP.HCM",
                Province = "Việt Nam",
                IsDefault = true,
                Created = now,
                CreatedBy = SystemUser,
                LastModified = now,
                LastModifiedBy = SystemUser,
            });
            result.ShippingAddressesCreated++;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedSampleOrdersAsync(
        SeededAccounts accounts,
        List<DesignTemplate> templates,
        DateTimeOffset now,
        MockSeedResult result,
        CancellationToken ct)
    {
        if (accounts.Customers.Count == 0) return;

        var customer = accounts.Customers[0];
        if (customer.CustomerId == null) return;

        var variant = await _db.DesignVariants
            .Include(v => v.DesignTemplate)
            .Where(v => v.IsActive)
            .OrderByDescending(v => v.StockQuantity)
            .FirstOrDefaultAsync(ct);
        if (variant == null) return;

        var address = await _db.ShippingAddresses.FirstOrDefaultAsync(a => a.CustomerId == customer.CustomerId.Value, ct);
        if (address == null) return;

        var alreadySeeded = await _db.Orders
            .Where(o => o.Code.StartsWith("DEMO-"))
            .CountAsync(ct);
        if (alreadySeeded >= 2) return;

        var shippingFee = 30_000m;

        // 1. Đơn COMPLETED (đã giao) — cho phép feedback
        var qtyCompleted = 1;
        var unitPriceCompleted = variant.Price;
        var totalCompleted = unitPriceCompleted * qtyCompleted + shippingFee;
        var orderCompleted = new Order
        {
            Id = Guid.NewGuid(),
            Code = $"DEMO-{now.ToString("yyMMddHHmm")}-1",
            CustomerId = customer.CustomerId.Value,
            TotalPrice = totalCompleted,
            OrderStatus = OrderStatuses.Completed,
            Priority = 0,
            DepositedAt = now.AddDays(-10),
            DeliveredAt = now.AddDays(-3),
            CompletedAt = now.AddDays(-2),
            Created = now.AddDays(-10),
            CreatedBy = SystemUser,
            LastModified = now,
            LastModifiedBy = SystemUser,
        };
        _db.Orders.Add(orderCompleted);

        var oiCompleted = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderCompleted.Id,
            Order = orderCompleted,
            SourceType = SourceTypes.InStock,
            DesignVariantId = variant.Id,
            ItemName = $"{variant.DesignTemplate?.Name} — {variant.Name}",
            QuantityOrdered = qtyCompleted,
            UnitPrice = unitPriceCompleted,
            TotalPrice = unitPriceCompleted * qtyCompleted,
            FulfillmentStatus = OrderItemStatuses.Finished,
            Created = now.AddDays(-10),
            CreatedBy = SystemUser,
            LastModified = now,
            LastModifiedBy = SystemUser,
        };
        _db.OrderItems.Add(oiCompleted);

        _db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = orderCompleted.Id,
            InvoiceCode = $"INV-{orderCompleted.Code}",
            SubTotal = unitPriceCompleted * qtyCompleted,
            TaxAmount = 0,
            ShippingFee = shippingFee,
            TotalAmount = totalCompleted,
            PaymentStatus = InvoiceStatuses.Paid,
            DueDate = DateTime.UtcNow.AddDays(7),
            Created = now.AddDays(-10),
            CreatedBy = SystemUser,
            LastModified = now,
            LastModifiedBy = SystemUser,
        });

        _db.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderCompleted.Id,
            ShippingAddressId = address.Id,
            ShippingFee = shippingFee,
            TrackingNumber = "VN-DEMO-001",
            ShipmentStatus = ShipmentStatuses.Delivered,
            ShippedAt = DateTime.UtcNow.AddDays(-5),
            DeliveredAt = DateTime.UtcNow.AddDays(-3),
            EstimatedDeliveryTime = DateTime.UtcNow.AddDays(-3),
            Created = now.AddDays(-10),
            CreatedBy = SystemUser,
            LastModified = now,
            LastModifiedBy = SystemUser,
        });

        if (!await _db.Feedbacks.AnyAsync(f => f.OrderItemId == oiCompleted.Id, ct))
        {
            _db.Feedbacks.Add(new Feedback
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.CustomerId.Value,
                DesignTemplateId = variant.DesignTemplateId,
                OrderItemId = oiCompleted.Id,
                Rating = 5,
                Comment = "Sản phẩm in rất chi tiết, giao nhanh, đóng gói cẩn thận. Sẽ ủng hộ tiếp!",
                Created = now.AddDays(-2),
                CreatedBy = SystemUser,
                LastModified = now,
                LastModifiedBy = SystemUser,
            });
            result.FeedbacksCreated++;
        }

        // 2. Đơn PENDING (chờ xác nhận / chờ thanh toán)
        var qtyPending = 2;
        var unitPricePending = variant.Price;
        var totalPending = unitPricePending * qtyPending + shippingFee;
        var orderPending = new Order
        {
            Id = Guid.NewGuid(),
            Code = $"DEMO-{now.ToString("yyMMddHHmm")}-2",
            CustomerId = customer.CustomerId.Value,
            TotalPrice = totalPending,
            OrderStatus = OrderStatuses.Pending,
            Priority = 0,
            Created = now,
            CreatedBy = SystemUser,
            LastModified = now,
            LastModifiedBy = SystemUser,
        };
        _db.Orders.Add(orderPending);

        _db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderPending.Id,
            Order = orderPending,
            SourceType = SourceTypes.InStock,
            DesignVariantId = variant.Id,
            ItemName = $"{variant.DesignTemplate?.Name} — {variant.Name}",
            QuantityOrdered = qtyPending,
            UnitPrice = unitPricePending,
            TotalPrice = unitPricePending * qtyPending,
            FulfillmentStatus = OrderItemStatuses.Pending,
            Created = now,
            CreatedBy = SystemUser,
            LastModified = now,
            LastModifiedBy = SystemUser,
        });

        _db.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = orderPending.Id,
            InvoiceCode = $"INV-{orderPending.Code}",
            SubTotal = unitPricePending * qtyPending,
            TaxAmount = 0,
            ShippingFee = shippingFee,
            TotalAmount = totalPending,
            PaymentStatus = InvoiceStatuses.Unpaid,
            DueDate = DateTime.UtcNow.AddDays(3),
            Created = now,
            CreatedBy = SystemUser,
            LastModified = now,
            LastModifiedBy = SystemUser,
        });

        _db.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderPending.Id,
            ShippingAddressId = address.Id,
            ShippingFee = shippingFee,
            ShipmentStatus = ShipmentStatuses.Preparing,
            EstimatedDeliveryTime = DateTime.UtcNow.AddDays(5),
            Created = now,
            CreatedBy = SystemUser,
            LastModified = now,
            LastModifiedBy = SystemUser,
        });

        result.OrdersCreated += 2;

        await _db.SaveChangesAsync(ct);
    }

    private record AccountSeedSpec(string Username, string Fullname, string Email, string Phone, string Role);
    private record TemplateSpec(string Code, string Name, string Description, string FileUrl, string ThumbnailUrl, Guid[] ConceptTagIds, VariantSpec[] Variants);
    private record VariantSpec(string Code, string Name, string Description, decimal Price, Guid MaterialId, int Stock, decimal WeightG, decimal PrintTimeMin, bool AllowPreOrder);
}

public class SeededAccounts
{
    public AccountBundle Manager { get; set; } = null!;
    public List<AccountBundle> Staff { get; } = new();
    public List<AccountBundle> Customers { get; } = new();
}

public record AccountBundle(Account Account, Guid? CustomerId, Guid? StaffId, Guid? ManagerId);

public class MockSeedResult
{
    public int AccountsCreated { get; set; }
    public int MaterialsCreated { get; set; }
    public int ConceptTagsCreated { get; set; }
    public int DesignTemplatesCreated { get; set; }
    public int DesignVariantsCreated { get; set; }
    public int ShippingAddressesCreated { get; set; }
    public int OrdersCreated { get; set; }
    public int FeedbacksCreated { get; set; }
    public string PasswordForDemoUsers { get; set; } = string.Empty;

    public override string ToString() =>
        $"Accounts={AccountsCreated}, Materials={MaterialsCreated}, Concepts={ConceptTagsCreated}, " +
        $"Templates={DesignTemplatesCreated}, Variants={DesignVariantsCreated}, " +
        $"ShippingAddresses={ShippingAddressesCreated}, Orders={OrdersCreated}, Feedbacks={FeedbacksCreated}";
}
