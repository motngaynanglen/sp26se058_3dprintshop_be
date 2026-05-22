using sp26se058_3dprintshop_be.Application.Shipments.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Shipments.Commands;

[TestFixture]
public class ApproveShipmentAddressChangeCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private ApproveShipmentAddressChangeCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        _handler = new ApproveShipmentAddressChangeCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (ShipmentAddressChangeRequest request, Shipment shipment) SeedData(
        string shipmentStatus = ShipmentStatuses.Preparing,
        string requestStatus = ShipmentAddressChangeRequestStatuses.Pending)
    {
        var customer = new Customer { Id = Guid.NewGuid(), AccountId = Guid.NewGuid() };
        _context.Customers.Add(customer);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = "ORD-APR-001",
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
            ShipmentStatus = shipmentStatus,
            ShippingFee = 0,
            RecipientName = "Old Name",
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
            ReceiverName = "New Name",
            Phone = "0911111111",
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
            Shipment = shipment,
            RequestedByCustomerId = customer.Id,
            NewShippingAddressId = address.Id,
            NewShippingAddress = address,
            Status = requestStatus,
            Reason = "Chuyển nhà"
        };
        _context.ShipmentAddressChangeRequests.Add(changeRequest);
        _context.SaveChanges();

        return (changeRequest, shipment);
    }

    [Test]
    public async Task Handle_ValidPendingRequest_ApprovesRequest()
    {
        var (changeRequest, _) = SeedData();
        var command = new ApproveShipmentAddressChangeCommand
        {
            Id = changeRequest.Id,
            ResponseNote = "Đồng ý"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.ShipmentAddressChangeRequests
            .FirstAsync(r => r.Id == changeRequest.Id);
        updated.Status.Should().Be(ShipmentAddressChangeRequestStatuses.Approved);
        updated.ResponseNote.Should().Be("Đồng ý");
        updated.ReviewedAt.Should().NotBeNull();
    }

    [Test]
    public async Task Handle_ValidApproval_AppliesNewAddressToShipment()
    {
        var (changeRequest, shipment) = SeedData();
        var command = new ApproveShipmentAddressChangeCommand { Id = changeRequest.Id };

        await _handler.Handle(command, CancellationToken.None);

        var updatedShipment = await _context.Shipments.FirstAsync(s => s.Id == shipment.Id);
        updatedShipment.RecipientName.Should().Be("New Name");
        updatedShipment.RecipientPhone.Should().Be("0911111111");
        updatedShipment.AddressLine.Should().Be("456 New Street");
        updatedShipment.Ward.Should().Be("Ward 5");
    }

    [Test]
    public async Task Handle_ValidApproval_ReadyForPickupShipment_Succeeds()
    {
        var (changeRequest, _) = SeedData(shipmentStatus: ShipmentStatuses.ReadyForPickup);
        var command = new ApproveShipmentAddressChangeCommand { Id = changeRequest.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Test]
    public void Handle_RequestNotFound_ThrowsDataNotFoundException()
    {
        var command = new ApproveShipmentAddressChangeCommand { Id = Guid.NewGuid() };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Handle_AlreadyApproved_ThrowsBusinessException()
    {
        var (changeRequest, _) = SeedData(requestStatus: ShipmentAddressChangeRequestStatuses.Approved);
        var command = new ApproveShipmentAddressChangeCommand { Id = changeRequest.Id };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được xử lý*");
    }

    [Test]
    public void Handle_AlreadyRejected_ThrowsBusinessException()
    {
        var (changeRequest, _) = SeedData(requestStatus: ShipmentAddressChangeRequestStatuses.Rejected);
        var command = new ApproveShipmentAddressChangeCommand { Id = changeRequest.Id };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được xử lý*");
    }

    [Test]
    public void Handle_ShipmentInTransit_ThrowsBusinessException()
    {
        var (changeRequest, _) = SeedData(shipmentStatus: ShipmentStatuses.InTransit);
        var command = new ApproveShipmentAddressChangeCommand { Id = changeRequest.Id };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*bàn giao*");
    }

    [Test]
    public async Task Handle_ValidApproval_SetsLastModifiedBy()
    {
        var (changeRequest, _) = SeedData();
        var command = new ApproveShipmentAddressChangeCommand { Id = changeRequest.Id };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.ShipmentAddressChangeRequests
            .FirstAsync(r => r.Id == changeRequest.Id);
        updated.LastModifiedBy.Should().Be("staff01");
    }
}
