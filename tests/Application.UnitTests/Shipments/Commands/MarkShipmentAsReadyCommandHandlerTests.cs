using sp26se058_3dprintshop_be.Application.Shipments.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Shipments.Commands;

[TestFixture]
public class MarkShipmentAsReadyCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private MarkShipmentAsReadyCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");

        _handler = new MarkShipmentAsReadyCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (Shipment shipment, Guid orderId) SeedShipment(
        string shipmentStatus = ShipmentStatuses.Preparing,
        bool allItemsFinished = true)
    {
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            Code = "ORD-SHIP-001",
            CustomerId = Guid.NewGuid(),
            OrderStatus = OrderStatuses.Processing,
            TotalPrice = 150_000
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Order = order,
            SourceType = SourceTypes.InStock,
            ItemName = "Sản phẩm 3D test",
            QuantityOrdered = 1,
            UnitPrice = 150_000,
            TotalPrice = 150_000,
            FulfillmentStatus = allItemsFinished ? OrderItemStatuses.Finished : OrderItemStatuses.Printing
        };

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ShipmentStatus = shipmentStatus,
            ShippingFee = 0,
            RecipientName = "Test Customer",
            RecipientPhone = "0909090909",
            AddressLine = "456 Avenue",
            Ward = "Ward 2",
            District = "District 3",
            City = "HCM",
            Province = "HCM"
        };

        _context.Orders.Add(order);
        _context.OrderItems.Add(orderItem);
        _context.Shipments.Add(shipment);
        _context.SaveChanges();

        return (shipment, orderId);
    }

    [Test]
    public async Task Handle_WithPreparingStatusAndAllItemsFinished_TransitionsToReadyForPickup()
    {
        var (shipment, _) = SeedShipment(ShipmentStatuses.Preparing, allItemsFinished: true);

        var result = await _handler.Handle(
            new MarkShipmentAsReadyCommand { Id = shipment.Id }, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.Shipments.FirstAsync(s => s.Id == shipment.Id);
        updated.ShipmentStatus.Should().Be(ShipmentStatuses.ReadyForPickup);
    }

    [Test]
    public void Handle_WhenAnyItemNotFinished_ThrowsBusinessException()
    {
        var (shipment, _) = SeedShipment(ShipmentStatuses.Preparing, allItemsFinished: false);

        Func<Task> act = async () => await _handler.Handle(
            new MarkShipmentAsReadyCommand { Id = shipment.Id }, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*chưa hoàn thành*");
    }

    [Test]
    public void Handle_WhenAlreadyReadyForPickup_ThrowsBusinessException()
    {
        var (shipment, _) = SeedShipment(ShipmentStatuses.ReadyForPickup, allItemsFinished: true);

        Func<Task> act = async () => await _handler.Handle(
            new MarkShipmentAsReadyCommand { Id = shipment.Id }, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được xác nhận*");
    }

    [Test]
    public void Handle_WhenShipmentInTransit_ThrowsBusinessException()
    {
        var (shipment, _) = SeedShipment(ShipmentStatuses.InTransit, allItemsFinished: true);

        Func<Task> act = async () => await _handler.Handle(
            new MarkShipmentAsReadyCommand { Id = shipment.Id }, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*không thể xác nhận*");
    }

    [Test]
    public void Handle_ShipmentNotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _handler.Handle(
            new MarkShipmentAsReadyCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public async Task Handle_Succeeds_UpdatesLastModifiedBy()
    {
        var (shipment, _) = SeedShipment(ShipmentStatuses.Preparing, allItemsFinished: true);

        await _handler.Handle(new MarkShipmentAsReadyCommand { Id = shipment.Id }, CancellationToken.None);

        var updated = await _context.Shipments.FirstAsync(s => s.Id == shipment.Id);
        updated.LastModifiedBy.Should().Be("staff01");
    }
}
