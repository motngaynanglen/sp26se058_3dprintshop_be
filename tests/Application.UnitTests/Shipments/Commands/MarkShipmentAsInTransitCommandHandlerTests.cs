using sp26se058_3dprintshop_be.Application.Shipments.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Shipments.Commands;

[TestFixture]
public class MarkShipmentAsInTransitCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private MarkShipmentAsInTransitCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _handler = new MarkShipmentAsInTransitCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Shipment SeedShipment(string status = ShipmentStatuses.ReadyForPickup)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = "ORD-TRANSIT-001",
            CustomerId = Guid.NewGuid(),
            OrderStatus = OrderStatuses.Processing,
            TotalPrice = 100_000
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            SourceType = SourceTypes.InStock,
            ItemName = "Sản phẩm 3D",
            QuantityOrdered = 1,
            UnitPrice = 100_000,
            TotalPrice = 100_000,
            FulfillmentStatus = OrderItemStatuses.Finished
        };

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            ShipmentStatus = status,
            ShippingFee = 0,
            RecipientName = "Test Customer",
            RecipientPhone = "0909090909",
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
    public async Task Handle_ReadyForPickup_TransitionsToInTransit()
    {
        var shipment = SeedShipment(ShipmentStatuses.ReadyForPickup);
        var command = new MarkShipmentAsInTransitCommand
        {
            Id = shipment.Id,
            CarrierName = "ViettelPost",
            TrackingNumber = "SPX123456789",
            ShippedAt = DateTime.UtcNow,
            EstimatedDeliveryTime = DateTime.UtcNow.AddDays(3)
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.Shipments.FirstAsync(s => s.Id == shipment.Id);
        updated.ShipmentStatus.Should().Be(ShipmentStatuses.InTransit);
        updated.CarrierName.Should().Be("ViettelPost");
        updated.TrackingNumber.Should().Be("SPX123456789");
    }

    [Test]
    public void Handle_AlreadyInTransit_ThrowsBusinessException()
    {
        var shipment = SeedShipment(ShipmentStatuses.InTransit);
        var command = new MarkShipmentAsInTransitCommand
        {
            Id = shipment.Id,
            CarrierName = "GHN",
            TrackingNumber = "GHN999"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được xác nhận đang vận chuyển*");
    }

    [Test]
    public void Handle_WhenStatusIsNotReadyForPickup_ThrowsBusinessException()
    {
        var shipment = SeedShipment(ShipmentStatuses.Preparing);
        var command = new MarkShipmentAsInTransitCommand
        {
            Id = shipment.Id,
            CarrierName = "GHTK",
            TrackingNumber = "GHTK001"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*không thể bắt đầu giao hàng*");
    }

    [Test]
    public void Handle_ShipmentNotFound_ThrowsDataNotFoundException()
    {
        var command = new MarkShipmentAsInTransitCommand
        {
            Id = Guid.NewGuid(),
            CarrierName = "GHN",
            TrackingNumber = "GHN000"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public async Task Handle_Success_UpdatesLastModifiedBy()
    {
        var shipment = SeedShipment(ShipmentStatuses.ReadyForPickup);
        var command = new MarkShipmentAsInTransitCommand
        {
            Id = shipment.Id,
            CarrierName = "ViettelPost",
            TrackingNumber = "VTP123"
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.Shipments.FirstAsync(s => s.Id == shipment.Id);
        updated.LastModifiedBy.Should().Be("staff01");
    }
}
