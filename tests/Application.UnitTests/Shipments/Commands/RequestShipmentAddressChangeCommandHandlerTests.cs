using sp26se058_3dprintshop_be.Application.Shipments.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Shipments.Commands;

[TestFixture]
public class RequestShipmentAddressChangeCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private RequestShipmentAddressChangeCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("customer01");
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);
        _handler = new RequestShipmentAddressChangeCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (Shipment shipment, Customer customer, ShippingAddress address) SeedData(
        string shipmentStatus = ShipmentStatuses.Preparing,
        bool hasPendingRequest = false)
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
            Code = "ORD-ADDR-001",
            CustomerId = customer.Id,
            OrderStatus = OrderStatuses.Processing,
            TotalPrice = 100_000
        };
        _context.Orders.Add(order);

        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            Order = order,
            SourceType = SourceTypes.InStock,
            ItemName = "Sản phẩm test",
            QuantityOrdered = 1,
            UnitPrice = 100_000,
            TotalPrice = 100_000,
            FulfillmentStatus = OrderItemStatuses.Printing
        };
        _context.OrderItems.Add(orderItem);

        var address = new ShippingAddress
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Customer = customer,
            ReceiverName = "Customer",
            Phone = "0900000000",
            AddressLine = "456 New Street",
            Ward = "Ward 5",
            District = "District 3",
            City = "HCM",
            Province = "HCM"
        };
        _context.ShippingAddresses.Add(address);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            ShipmentStatus = shipmentStatus,
            ShippingFee = 0,
            RecipientName = "Customer",
            RecipientPhone = "0900000000",
            AddressLine = "123 Old Street",
            Ward = "Ward 1",
            District = "District 1",
            City = "HCM",
            Province = "HCM"
        };
        _context.Shipments.Add(shipment);

        if (hasPendingRequest)
        {
            _context.ShipmentAddressChangeRequests.Add(new ShipmentAddressChangeRequest
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                RequestedByCustomerId = customer.Id,
                NewShippingAddressId = address.Id,
                Status = ShipmentAddressChangeRequestStatuses.Pending,
                Reason = "Đã có yêu cầu pending"
            });
        }

        _context.SaveChanges();
        _user.Setup(u => u.Id).Returns(accountId.ToString());
        return (shipment, customer, address);
    }

    [Test]
    public async Task Handle_ValidRequest_CreatesChangeRequest()
    {
        var (shipment, _, address) = SeedData();
        var command = new RequestShipmentAddressChangeCommand
        {
            ShipmentId = shipment.Id,
            NewShippingAddressId = address.Id,
            Reason = "Chuyển nhà"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var saved = await _context.ShipmentAddressChangeRequests
            .FirstOrDefaultAsync(r => r.ShipmentId == shipment.Id);
        saved.Should().NotBeNull();
        saved!.Status.Should().Be(ShipmentAddressChangeRequestStatuses.Pending);
    }

    [Test]
    public void Handle_NoCustomerAccount_ThrowsForbiddenAccessException()
    {
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString()); // no customer record

        var command = new RequestShipmentAddressChangeCommand
        {
            ShipmentId = Guid.NewGuid(),
            NewShippingAddressId = Guid.NewGuid(),
            Reason = "Test"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public void Handle_ShipmentInTransit_ThrowsBusinessException()
    {
        var (shipment, _, address) = SeedData(shipmentStatus: ShipmentStatuses.InTransit);
        var command = new RequestShipmentAddressChangeCommand
        {
            ShipmentId = shipment.Id,
            NewShippingAddressId = address.Id,
            Reason = "Test"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*chưa được bàn giao*");
    }

    [Test]
    public void Handle_AlreadyHasPendingRequest_ThrowsBusinessException()
    {
        var (shipment, _, address) = SeedData(hasPendingRequest: true);
        var command = new RequestShipmentAddressChangeCommand
        {
            ShipmentId = shipment.Id,
            NewShippingAddressId = address.Id,
            Reason = "Test"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đang chờ xử lý*");
    }

    [Test]
    public void Handle_ShipmentNotFound_ThrowsDataNotFoundException()
    {
        var (_, _, address) = SeedData();
        var command = new RequestShipmentAddressChangeCommand
        {
            ShipmentId = Guid.NewGuid(),
            NewShippingAddressId = address.Id,
            Reason = "Test"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
