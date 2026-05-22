using sp26se058_3dprintshop_be.Application.Shipments.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Shipments.Commands;

[TestFixture]
public class CancelShipmentCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private CancelShipmentCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        _handler = new CancelShipmentCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Shipment SeedShipment(string status = ShipmentStatuses.Preparing)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = "ORD-CANCEL-001",
            CustomerId = Guid.NewGuid(),
            OrderStatus = OrderStatuses.Processing,
            TotalPrice = 100_000
        };
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
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ShipmentStatus = status,
            ShippingFee = 0,
            RecipientName = "Customer",
            RecipientPhone = "0900000000",
            AddressLine = "123 Street",
            Ward = "Ward 1",
            District = "District 1",
            City = "HCM",
            Province = "HCM"
        };
        _context.Orders.Add(order);
        _context.OrderItems.Add(orderItem);
        _context.Shipments.Add(shipment);
        _context.SaveChanges();
        return shipment;
    }

    [Test]
    public async Task Handle_Preparing_CancelsShipment()
    {
        var shipment = SeedShipment(ShipmentStatuses.Preparing);

        var result = await _handler.Handle(
            new CancelShipmentCommand { Id = shipment.Id, Reason = "Khách hủy" },
            CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.Shipments.FirstAsync(s => s.Id == shipment.Id);
        updated.ShipmentStatus.Should().Be(ShipmentStatuses.Cancelled);
    }

    [Test]
    public async Task Handle_ReadyForPickup_CancelsShipment()
    {
        var shipment = SeedShipment(ShipmentStatuses.ReadyForPickup);

        await _handler.Handle(
            new CancelShipmentCommand { Id = shipment.Id, Reason = "Lý do hủy" },
            CancellationToken.None);

        var updated = await _context.Shipments.FirstAsync(s => s.Id == shipment.Id);
        updated.ShipmentStatus.Should().Be(ShipmentStatuses.Cancelled);
    }

    [Test]
    public void Handle_AlreadyCancelled_ThrowsBusinessException()
    {
        var shipment = SeedShipment(ShipmentStatuses.Cancelled);

        Func<Task> act = async () => await _handler.Handle(
            new CancelShipmentCommand { Id = shipment.Id, Reason = "Lý do" },
            CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được hủy*");
    }

    [Test]
    public void Handle_InTransit_ThrowsBusinessException()
    {
        var shipment = SeedShipment(ShipmentStatuses.InTransit);

        Func<Task> act = async () => await _handler.Handle(
            new CancelShipmentCommand { Id = shipment.Id, Reason = "Lý do" },
            CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*chưa bàn giao*");
    }

    [Test]
    public void Handle_ShipmentNotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _handler.Handle(
            new CancelShipmentCommand { Id = Guid.NewGuid(), Reason = "Lý do" },
            CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public async Task Handle_WithPendingAddressRequest_RejectsIt()
    {
        var shipment = SeedShipment(ShipmentStatuses.Preparing);

        var changeRequest = new ShipmentAddressChangeRequest
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            RequestedByCustomerId = Guid.NewGuid(),
            NewShippingAddressId = Guid.NewGuid(),
            Status = ShipmentAddressChangeRequestStatuses.Pending,
            Reason = "Đổi địa chỉ"
        };
        _context.ShipmentAddressChangeRequests.Add(changeRequest);
        await _context.SaveChangesAsync();

        await _handler.Handle(
            new CancelShipmentCommand { Id = shipment.Id, Reason = "Hủy vận đơn" },
            CancellationToken.None);

        var updatedRequest = await _context.ShipmentAddressChangeRequests.FirstAsync(r => r.Id == changeRequest.Id);
        updatedRequest.Status.Should().Be(ShipmentAddressChangeRequestStatuses.Rejected);
    }
}
