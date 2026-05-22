using sp26se058_3dprintshop_be.Application.DesignVariants.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignVariants.Commands;

[TestFixture]
public class DeleteDesignVariantCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IUser> _user = null!;
    private DeleteDesignVariantCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("manager01");
        _handler = new DeleteDesignVariantCommandHandler(_context, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private DesignVariant SeedVariant(bool withOrderItems = false)
    {
        var variant = new DesignVariant
        {
            Code = $"VAR-DEL-{Guid.NewGuid():N}",
            Name = "Variant to delete",
            Price = 50_000,
            MaterialId = Guid.NewGuid(),
            DesignTemplateId = Guid.NewGuid(),
            CatalogStatus = CatalogStatuses.Published,
            IsActive = true
        };
        _context.DesignVariants.Add(variant);

        if (withOrderItems)
        {
            var order = new Order
            {
                Code = "ORD-DEL-001",
                CustomerId = Guid.NewGuid(),
                OrderStatus = OrderStatuses.Completed,
                TotalPrice = 50_000
            };
            _context.Orders.Add(order);

            _context.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                Order = order,
                DesignVariantId = variant.Id,
                SourceType = SourceTypes.InStock,
                ItemName = "Sản phẩm đã đặt",
                QuantityOrdered = 1,
                UnitPrice = 50_000,
                TotalPrice = 50_000,
                FulfillmentStatus = OrderItemStatuses.Finished
            });
        }

        _context.SaveChanges();
        return variant;
    }

    [Test]
    public async Task Handle_NoOrderItems_SoftDeletesVariant()
    {
        var variant = SeedVariant(withOrderItems: false);

        var result = await _handler.Handle(
            new DeleteDesignVariantCommand { Id = variant.Id }, CancellationToken.None);

        result.Should().BeTrue();
        var updated = await _context.DesignVariants
            .IgnoreQueryFilters()
            .FirstAsync(v => v.Id == variant.Id);
        updated.CatalogStatus.Should().Be(CatalogStatuses.Archived);
        updated.IsActive.Should().BeFalse();
        updated.Deleted.Should().NotBeNull();
        updated.DeletedBy.Should().Be("manager01");
    }

    [Test]
    public void Handle_HasOrderItems_ThrowsDeleteFailureException()
    {
        var variant = SeedVariant(withOrderItems: true);

        Func<Task> act = async () => await _handler.Handle(
            new DeleteDesignVariantCommand { Id = variant.Id }, CancellationToken.None);

        act.Should().ThrowAsync<DeleteFailureException>()
            .WithMessage("*lịch sử đơn hàng*");
    }

    [Test]
    public void Handle_VariantNotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _handler.Handle(
            new DeleteDesignVariantCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
