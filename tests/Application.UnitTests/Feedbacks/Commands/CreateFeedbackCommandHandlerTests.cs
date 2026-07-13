using sp26se058_3dprintshop_be.Application.Feedbacks.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Feedbacks.Commands;

[TestFixture]
public class CreateFeedbackCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private CreateFeedbackCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("cust01");
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);
        _handler = new CreateFeedbackCommandHandler(_context, _user.Object, _mapper);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (OrderItem orderItem, Guid accountId) SeedData(
        string orderStatus = OrderStatuses.Completed,
        bool hasDesignVariant = true)
    {
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            Username = "cust01",
            Email = "cust01@test.com",
            PasswordHash = "hash",
            Fullname = "Customer"
        };
        _context.Accounts.Add(account);

        var customer = new Customer { Id = Guid.NewGuid(), AccountId = accountId };
        _context.Customers.Add(customer);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = "ORD-FB-001",
            CustomerId = customer.Id,
            Customer = customer,
            OrderStatus = orderStatus,
            TotalPrice = 100_000
        };
        _context.Orders.Add(order);

        DesignVariant? variant = null;
        if (hasDesignVariant)
        {
            var material = new Material { Id = Guid.NewGuid(), Name = "PLA", IsActive = true };
            _context.Materials.Add(material);

            var template = new DesignTemplate
            {
                Id = Guid.NewGuid(),
                Code = $"TPL-{Guid.NewGuid():N}".Substring(0, 15),
                Name = "Template",
                FileUrl = "https://storage.test/tpl.stl"
            };
            _context.DesignTemplates.Add(template);

            variant = new DesignVariant
            {
                Id = Guid.NewGuid(),
                Code = $"VAR-{Guid.NewGuid():N}".Substring(0, 15),
                Name = "Variant",
                Price = 50_000,
                MaterialId = material.Id,
                DesignTemplateId = template.Id,
                CatalogStatus = CatalogStatuses.Published,
                IsActive = true
            };
            _context.DesignVariants.Add(variant);
        }

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            DesignVariantId = variant?.Id,
            SourceType = SourceTypes.InStock,
            ItemName = "Sản phẩm test",
            QuantityOrdered = 1,
            UnitPrice = 100_000,
            TotalPrice = 100_000,
            FulfillmentStatus = OrderItemStatuses.Finished
        };
        _context.OrderItems.Add(orderItem);
        _context.SaveChanges();

        _user.Setup(u => u.Id).Returns(accountId.ToString());
        return (orderItem, accountId);
    }

    [Test]
    public async Task Handle_ValidRequest_CreatesFeedback()
    {
        var (orderItem, _) = SeedData();
        var command = new CreateFeedbackCommand
        {
            OrderItemId = orderItem.Id,
            Rating = 5,
            Comment = "Sản phẩm tuyệt vời!"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var saved = await _context.Feedbacks.FirstOrDefaultAsync(f => f.OrderItemId == orderItem.Id);
        saved.Should().NotBeNull();
        saved!.Rating.Should().Be(5);
        saved.Comment.Should().Be("Sản phẩm tuyệt vời!");
    }

    [Test]
    public void Handle_OrderNotCompleted_ThrowsBusinessException()
    {
        var (orderItem, _) = SeedData(orderStatus: OrderStatuses.Processing);
        var command = new CreateFeedbackCommand
        {
            OrderItemId = orderItem.Id,
            Rating = 4
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*nhận hàng*");
    }

    [Test]
    public void Handle_WrongCustomer_ThrowsForbiddenAccessException()
    {
        var (orderItem, _) = SeedData();
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString()); // different user

        var command = new CreateFeedbackCommand
        {
            OrderItemId = orderItem.Id,
            Rating = 3
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public void Handle_AlreadyFeedback_ThrowsBusinessException()
    {
        var (orderItem, _) = SeedData();

        var order = _context.Orders.First();
        var template = _context.DesignTemplates.First();
        _context.Feedbacks.Add(new Feedback
        {
            Id = Guid.NewGuid(),
            CustomerId = order.CustomerId,
            OrderItemId = orderItem.Id,
            DesignTemplateId = template.Id,
            Rating = 4
        });
        _context.SaveChanges();

        var command = new CreateFeedbackCommand
        {
            OrderItemId = orderItem.Id,
            Rating = 5
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được*");
    }

    [Test]
    public void Handle_NoDesignVariant_ThrowsBusinessException()
    {
        var (orderItem, _) = SeedData(hasDesignVariant: false);
        var command = new CreateFeedbackCommand
        {
            OrderItemId = orderItem.Id,
            Rating = 3
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*theo mẫu*");
    }

    [Test]
    public void Handle_OrderItemNotFound_ThrowsDataNotFoundException()
    {
        var command = new CreateFeedbackCommand
        {
            OrderItemId = Guid.NewGuid(),
            Rating = 5
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
