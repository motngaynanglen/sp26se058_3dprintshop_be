using PayOS.Exceptions;
using sp26se058_3dprintshop_be.Application.InventoryTransactions.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.UnitTests.InventoryTransactions.Commands;

[TestFixture]
public class CreateInventoryTransactionCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IUser> _user = null!;
    private CreateInventoryTransactionHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("manager01");
        _user.Setup(u => u.Role).Returns(Roles.MANAGER);
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        _handler = new CreateInventoryTransactionHandler(_context, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private DesignVariant SeedVariant(int initialStock = 10)
    {
        var variant = new DesignVariant
        {
            Code = "VAR-INV-001",
            Name = "Test Variant",
            Price = 50_000,
            MaterialId = Guid.NewGuid(),
            DesignTemplateId = Guid.NewGuid(),
            CatalogStatus = CatalogStatuses.Published,
            IsActive = true,
            StockQuantity = initialStock
        };
        _context.DesignVariants.Add(variant);
        _context.SaveChanges();
        return variant;
    }

    [Test]
    public async Task Handle_Manager_IncreaseStock_Success()
    {
        var variant = SeedVariant(initialStock: 10);
        var command = new CreateInventoryTransactionCommand
        {
            DesignVariantId = variant.Id,
            Quantity = 5,
            Type = InventoryTransactionTypes.PurchaseIn
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.DesignVariants.FirstAsync(v => v.Id == variant.Id);
        updated.StockQuantity.Should().Be(15);
    }

    [Test]
    public async Task Handle_Manager_AdjustmentDecreaseStock_Success()
    {
        var variant = SeedVariant(initialStock: 10);
        var command = new CreateInventoryTransactionCommand
        {
            DesignVariantId = variant.Id,
            Quantity = -3,
            Type = InventoryTransactionTypes.Adjustment
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.DesignVariants.FirstAsync(v => v.Id == variant.Id);
        updated.StockQuantity.Should().Be(7);
    }

    [Test]
    public async Task Handle_Success_CreatesInventoryTransactionRecord()
    {
        var variant = SeedVariant(initialStock: 5);
        var command = new CreateInventoryTransactionCommand
        {
            DesignVariantId = variant.Id,
            Quantity = 10,
            Type = InventoryTransactionTypes.ProductionIn,
            Note = "Nhập kho từ máy in"
        };

        await _handler.Handle(command, CancellationToken.None);

        var txn = await _context.InventoryTransactions.FirstOrDefaultAsync(
            t => t.DesignVariantId == variant.Id);
        txn.Should().NotBeNull();
        txn!.Quantity.Should().Be(10);
        txn.Type.Should().Be(InventoryTransactionTypes.ProductionIn);
        txn.Note.Should().Be("Nhập kho từ máy in");
    }

    [Test]
    public void Handle_InsufficientStock_ThrowsBadRequestException()
    {
        var variant = SeedVariant(initialStock: 3);
        var command = new CreateInventoryTransactionCommand
        {
            DesignVariantId = variant.Id,
            Quantity = -10, // more than stock
            Type = InventoryTransactionTypes.Adjustment
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Kho không đủ hàng*");
    }

    [Test]
    public void Handle_VariantNotFound_ThrowsDataNotFoundException()
    {
        var command = new CreateInventoryTransactionCommand
        {
            DesignVariantId = Guid.NewGuid(),
            Quantity = 5,
            Type = InventoryTransactionTypes.PurchaseIn
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Handle_StaffWithoutRecord_ThrowsForbiddenAccessException()
    {
        _user.Setup(u => u.Role).Returns(Roles.STAFF);
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString()); // no Staff record

        var variant = SeedVariant();
        var command = new CreateInventoryTransactionCommand
        {
            DesignVariantId = variant.Id,
            Quantity = 5,
            Type = InventoryTransactionTypes.PurchaseIn
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
