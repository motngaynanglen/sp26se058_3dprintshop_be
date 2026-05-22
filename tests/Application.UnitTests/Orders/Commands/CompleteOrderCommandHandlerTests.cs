using sp26se058_3dprintshop_be.Application.Orders.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Orders.Commands;

[TestFixture]
public class CompleteOrderCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private CompleteOrderCommandHandler _handler = null!;

    private Guid _accountId;
    private Guid _customerId;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();

        _handler = new CompleteOrderCommandHandler(_context, _mapper, _user.Object);

        _accountId = Guid.NewGuid();
        _customerId = Guid.NewGuid();

        _context.Accounts.Add(new Account
        {
            Id = _accountId,
            Username = "customer01",
            Email = "c01@test.com",
            PasswordHash = "hash",
            Fullname = "Customer One"
        });
        _context.Customers.Add(new Customer { Id = _customerId, AccountId = _accountId });
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (Order order, Shipment? shipment) SeedOrderWithShipment(
        string shipmentStatus = ShipmentStatuses.Delivered,
        DateTime? deliveredAt = null,
        bool includeShipment = true,
        bool allItemsFinished = true)
    {
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            Code = "ORD-COMPLETE-001",
            CustomerId = _customerId,
            OrderStatus = OrderStatuses.Processing,
            TotalPrice = 200_000
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Order = order,
            SourceType = SourceTypes.InStock,
            ItemName = "Sản phẩm test",
            QuantityOrdered = 1,
            UnitPrice = 200_000,
            TotalPrice = 200_000,
            FulfillmentStatus = allItemsFinished ? OrderItemStatuses.Finished : OrderItemStatuses.Printing
        };
        _context.Orders.Add(order);
        _context.OrderItems.Add(orderItem);

        Shipment? shipment = null;
        if (includeShipment)
        {
            shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ShipmentStatus = shipmentStatus,
                ShippingFee = 0,
                RecipientName = "Test Recipient",
                RecipientPhone = "0901234567",
                AddressLine = "123 Street",
                Ward = "Ward 1",
                District = "District 1",
                City = "HCM",
                Province = "HCM",
                DeliveredAt = deliveredAt?.ToUniversalTime() ?? (shipmentStatus == ShipmentStatuses.Delivered
                    ? DateTime.UtcNow.AddDays(-5)
                    : null)
            };
            _context.Shipments.Add(shipment);
        }

        _context.SaveChanges();
        return (order, shipment);
    }

    private void SetupAsCustomer(Guid? accountId = null)
    {
        var id = accountId ?? _accountId;
        _user.Setup(u => u.Id).Returns(id.ToString());
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);
        _user.Setup(u => u.Username).Returns("customer01");
    }

    private void SetupAsStaff()
    {
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        _user.Setup(u => u.Role).Returns(Roles.STAFF);
        _user.Setup(u => u.Username).Returns("staff01");
    }

    [Test]
    public async Task Handle_CustomerCompletesOrderWithDeliveredShipment_Succeeds()
    {
        var (order, _) = SeedOrderWithShipment(ShipmentStatuses.Delivered);
        SetupAsCustomer();

        var result = await _handler.Handle(new CompleteOrderCommand { Id = order.Id }, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.Orders.FirstAsync(o => o.Id == order.Id);
        updated.OrderStatus.Should().Be(OrderStatuses.Completed);
    }

    [Test]
    public async Task Handle_CustomerCompletesOrderWithInTransitShipment_MarksDeliveredAndCompletes()
    {
        var (order, shipment) = SeedOrderWithShipment(ShipmentStatuses.InTransit);
        SetupAsCustomer();

        await _handler.Handle(new CompleteOrderCommand { Id = order.Id }, CancellationToken.None);

        var updatedShipment = await _context.Shipments.FirstAsync(s => s.Id == shipment!.Id);
        updatedShipment.ShipmentStatus.Should().Be(ShipmentStatuses.Delivered);
    }

    [Test]
    public void Handle_CustomerCompletesWithNoShipmentInProgress_ThrowsValidationException()
    {
        var (order, _) = SeedOrderWithShipment(ShipmentStatuses.Preparing);
        SetupAsCustomer();

        Func<Task> act = async () =>
            await _handler.Handle(new CompleteOrderCommand { Id = order.Id }, CancellationToken.None);

        act.Should().ThrowAsync<Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task Handle_StaffCompletesOrderWithAllDeliveredAnd5DaysAfter_Succeeds()
    {
        var deliveredAt = DateTime.UtcNow.AddDays(-5);
        var (order, _) = SeedOrderWithShipment(ShipmentStatuses.Delivered, deliveredAt);
        SetupAsStaff();

        var result = await _handler.Handle(new CompleteOrderCommand { Id = order.Id }, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.Orders.FirstAsync(o => o.Id == order.Id);
        updated.OrderStatus.Should().Be(OrderStatuses.Completed);
    }

    [Test]
    public void Handle_StaffCompletesOrderWithin3DaysOfDelivery_ThrowsValidationException()
    {
        var deliveredAt = DateTime.UtcNow.AddDays(-1);
        var (order, _) = SeedOrderWithShipment(ShipmentStatuses.Delivered, deliveredAt);
        SetupAsStaff();

        Func<Task> act = async () =>
            await _handler.Handle(new CompleteOrderCommand { Id = order.Id }, CancellationToken.None);

        act.Should().ThrowAsync<Application.Common.Exceptions.ValidationException>()
            .WithMessage("*ngày*");
    }

    [Test]
    public void Handle_CompleteAlreadyCompletedOrder_ThrowsBusinessException()
    {
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            Code = "ORD-DONE",
            CustomerId = _customerId,
            OrderStatus = OrderStatuses.Completed,
            TotalPrice = 100_000
        };
        _context.Orders.Add(order);
        _context.SaveChanges();
        SetupAsCustomer();

        Func<Task> act = async () =>
            await _handler.Handle(new CompleteOrderCommand { Id = orderId }, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được hoàn thành*");
    }

    [Test]
    public void Handle_NonOwnerNonStaffTriesToComplete_ThrowsForbidden()
    {
        var (order, _) = SeedOrderWithShipment(ShipmentStatuses.Delivered);
        var otherId = Guid.NewGuid();
        _user.Setup(u => u.Id).Returns(otherId.ToString());
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);

        Func<Task> act = async () =>
            await _handler.Handle(new CompleteOrderCommand { Id = order.Id }, CancellationToken.None);

        act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public void Handle_OrderNotFound_ThrowsDataNotFoundException()
    {
        SetupAsCustomer();

        Func<Task> act = async () =>
            await _handler.Handle(new CompleteOrderCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
