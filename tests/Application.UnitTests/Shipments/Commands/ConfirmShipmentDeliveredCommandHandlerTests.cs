using sp26se058_3dprintshop_be.Application.Shipments.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Shipments.Commands;

[TestFixture]
public class ConfirmShipmentDeliveredCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private ConfirmShipmentDeliveredCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _handler = new ConfirmShipmentDeliveredCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Shipment SeedShipment(string status = ShipmentStatuses.InTransit)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = "ORD-DELIV-001",
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
            AddressLine = "789 Road",
            Ward = "Ward 3",
            District = "District 2",
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
    public async Task Handle_InTransit_TransitionsToDelivered()
    {
        var shipment = SeedShipment(ShipmentStatuses.InTransit);

        var result = await _handler.Handle(
            new ConfirmShipmentDeliveredCommand { Id = shipment.Id }, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.Shipments.FirstAsync(s => s.Id == shipment.Id);
        updated.ShipmentStatus.Should().Be(ShipmentStatuses.Delivered);
        updated.DeliveredAt.Should().NotBeNull();
    }

    [Test]
    public void Handle_AlreadyDelivered_ThrowsBusinessException()
    {
        var shipment = SeedShipment(ShipmentStatuses.Delivered);

        Func<Task> act = async () => await _handler.Handle(
            new ConfirmShipmentDeliveredCommand { Id = shipment.Id }, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được xác nhận giao thành công*");
    }

    [Test]
    public void Handle_NotInTransit_ThrowsBusinessException()
    {
        var shipment = SeedShipment(ShipmentStatuses.ReadyForPickup);

        Func<Task> act = async () => await _handler.Handle(
            new ConfirmShipmentDeliveredCommand { Id = shipment.Id }, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Không thể xác nhận giao hàng*");
    }

    [Test]
    public void Handle_ShipmentNotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _handler.Handle(
            new ConfirmShipmentDeliveredCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
