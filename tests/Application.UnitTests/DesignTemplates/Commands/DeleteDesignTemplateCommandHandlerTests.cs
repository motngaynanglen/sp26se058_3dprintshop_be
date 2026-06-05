using sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignTemplates.Commands;

[TestFixture]
public class DeleteDesignTemplateCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IUser> _user = null!;
    private DeleteDesignTemplateHander _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("manager01");
        _handler = new DeleteDesignTemplateHander(_context, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private DesignTemplate SeedTemplate(bool withOrderItems = false)
    {
        var materialId = Guid.NewGuid();
        _context.Materials.Add(new Material { Id = materialId, Name = "PLA", IsActive = true });

        var template = new DesignTemplate
        {
            Code = $"TPL-{Guid.NewGuid():N}",
            Name = "Template to delete",
            FileUrl = "https://storage.test/tpl.stl"
        };
        _context.DesignTemplates.Add(template);

        if (withOrderItems)
        {
            var variant = new DesignVariant
            {
                Code = $"VAR-{Guid.NewGuid():N}",
                Name = "Variant",
                Price = 50_000,
                MaterialId = materialId,
                DesignTemplateId = template.Id,
                DesignTemplate = template,
                CatalogStatus = CatalogStatuses.Published
            };
            _context.DesignVariants.Add(variant);

            var order = new Order
            {
                Code = "ORD-TPL-001",
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
                ItemName = "Sản phẩm từ mẫu",
                QuantityOrdered = 1,
                UnitPrice = 50_000,
                TotalPrice = 50_000,
                FulfillmentStatus = OrderItemStatuses.Finished
            });
        }

        _context.SaveChanges();
        return template;
    }

    [Test]
    public async Task Handle_NoOrderItems_SoftDeletesTemplate()
    {
        var template = SeedTemplate(withOrderItems: false);

        var result = await _handler.Handle(
            new DeleteDesignTemplateCommand { Id = template.Id }, CancellationToken.None);

        result.Should().BeTrue();
        var updated = await _context.DesignTemplates
            .IgnoreQueryFilters()
            .FirstAsync(t => t.Id == template.Id);
        updated.Deleted.Should().NotBeNull();
        updated.DeletedBy.Should().Be("manager01");
    }

    [Test]
    public void Handle_HasOrderItems_ThrowsDeleteFailureException()
    {
        var template = SeedTemplate(withOrderItems: true);

        Func<Task> act = async () => await _handler.Handle(
            new DeleteDesignTemplateCommand { Id = template.Id }, CancellationToken.None);

        act.Should().ThrowAsync<DeleteFailureException>()
            .WithMessage("*lịch sử*");
    }

    [Test]
    public void Handle_TemplateNotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _handler.Handle(
            new DeleteDesignTemplateCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
