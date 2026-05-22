using sp26se058_3dprintshop_be.Application.Shipments.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Shipments.Commands;

[TestFixture]
public class RejectShipmentAddressChangeCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private RejectShipmentAddressChangeCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        _handler = new RejectShipmentAddressChangeCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private ShipmentAddressChangeRequest SeedData(
        string requestStatus = ShipmentAddressChangeRequestStatuses.Pending)
    {
        var customer = new Customer { Id = Guid.NewGuid(), AccountId = Guid.NewGuid() };
        _context.Customers.Add(customer);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = "ORD-REJ-001",
            CustomerId = customer.Id,
            OrderStatus = OrderStatuses.Processing,
            TotalPrice = 100_000
        };
        _context.Orders.Add(order);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            ShipmentStatus = ShipmentStatuses.Preparing,
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

        var changeRequest = new ShipmentAddressChangeRequest
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            RequestedByCustomerId = customer.Id,
            NewShippingAddressId = address.Id,
            NewShippingAddress = address,
            Status = requestStatus,
            Reason = "Chuyển nhà"
        };
        _context.ShipmentAddressChangeRequests.Add(changeRequest);
        _context.SaveChanges();

        return changeRequest;
    }

    [Test]
    public async Task Handle_ValidPendingRequest_RejectsRequest()
    {
        var changeRequest = SeedData();
        var command = new RejectShipmentAddressChangeCommand
        {
            Id = changeRequest.Id,
            ResponseNote = "Đơn đã được bàn giao"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.ShipmentAddressChangeRequests
            .FirstAsync(r => r.Id == changeRequest.Id);
        updated.Status.Should().Be(ShipmentAddressChangeRequestStatuses.Rejected);
        updated.ResponseNote.Should().Be("Đơn đã được bàn giao");
        updated.ReviewedAt.Should().NotBeNull();
    }

    [Test]
    public async Task Handle_ValidRejection_DoesNotChangeShipmentAddress()
    {
        var changeRequest = SeedData();
        var command = new RejectShipmentAddressChangeCommand
        {
            Id = changeRequest.Id,
            ResponseNote = "Không thể đổi"
        };

        await _handler.Handle(command, CancellationToken.None);

        var shipment = await _context.Shipments.FirstAsync();
        shipment.AddressLine.Should().Be("123 Old Street");
        shipment.RecipientName.Should().Be("Customer");
    }

    [Test]
    public void Handle_RequestNotFound_ThrowsDataNotFoundException()
    {
        var command = new RejectShipmentAddressChangeCommand
        {
            Id = Guid.NewGuid(),
            ResponseNote = "Từ chối"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Handle_AlreadyApproved_ThrowsBusinessException()
    {
        var changeRequest = SeedData(requestStatus: ShipmentAddressChangeRequestStatuses.Approved);
        var command = new RejectShipmentAddressChangeCommand
        {
            Id = changeRequest.Id,
            ResponseNote = "Muộn rồi"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được xử lý*");
    }

    [Test]
    public void Handle_AlreadyRejected_ThrowsBusinessException()
    {
        var changeRequest = SeedData(requestStatus: ShipmentAddressChangeRequestStatuses.Rejected);
        var command = new RejectShipmentAddressChangeCommand
        {
            Id = changeRequest.Id,
            ResponseNote = "Đã từ chối rồi"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được xử lý*");
    }

    [Test]
    public async Task Handle_ValidRejection_SetsLastModifiedBy()
    {
        var changeRequest = SeedData();
        var command = new RejectShipmentAddressChangeCommand
        {
            Id = changeRequest.Id,
            ResponseNote = "Lý do từ chối"
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.ShipmentAddressChangeRequests
            .FirstAsync(r => r.Id == changeRequest.Id);
        updated.LastModifiedBy.Should().Be("staff01");
    }
}
