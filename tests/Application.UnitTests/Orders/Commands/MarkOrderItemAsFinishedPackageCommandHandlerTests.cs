using sp26se058_3dprintshop_be.Application.Orders.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Orders.Commands;

[TestFixture]
public class MarkOrderItemAsFinishedPackageCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private MarkOrderItemAsFinishedCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _handler = new MarkOrderItemAsFinishedCommandHandler(_context, _user.Object, _mapper);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (OrderItem item, Order order) SeedOrderItem(
        string sourceType = SourceTypes.InStock,
        string fulfillmentStatus = OrderItemStatuses.Printing,
        bool withShipment = false)
    {
        var customer = new Customer { Id = Guid.NewGuid(), AccountId = Guid.NewGuid() };
        _context.Customers.Add(customer);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = "ORD-PKG-001",
            CustomerId = customer.Id,
            Customer = customer,
            OrderStatus = OrderStatuses.Processing,
            TotalPrice = 100_000
        };
        _context.Orders.Add(order);

        var item = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            SourceType = sourceType,
            ItemName = "Sản phẩm test",
            QuantityOrdered = 1,
            UnitPrice = 100_000,
            TotalPrice = 100_000,
            FulfillmentStatus = fulfillmentStatus
        };
        _context.OrderItems.Add(item);

        if (withShipment)
        {
            _context.Shipments.Add(new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Order = order,
                ShipmentStatus = ShipmentStatuses.Preparing,
                ShippingFee = 0,
                RecipientName = "Customer",
                RecipientPhone = "0900000000",
                AddressLine = "123 Street",
                Ward = "Ward 1",
                District = "District 1",
                City = "HCM",
                Province = "HCM"
            });
        }

        _context.SaveChanges();
        return (item, order);
    }

    [Test]
    public async Task Handle_InStockItem_MarksAsFinished()
    {
        var (item, _) = SeedOrderItem(sourceType: SourceTypes.InStock);
        var command = new MarkOrderItemAsFinishedPackageCommand { Id = item.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.OrderItems.FirstAsync(i => i.Id == item.Id);
        updated.FulfillmentStatus.Should().Be(OrderItemStatuses.Finished);
    }

    [Test]
    public async Task Handle_InStockItem_SetsLastModifiedBy()
    {
        var (item, _) = SeedOrderItem();
        await _handler.Handle(
            new MarkOrderItemAsFinishedPackageCommand { Id = item.Id }, CancellationToken.None);

        var updated = await _context.OrderItems.FirstAsync(i => i.Id == item.Id);
        updated.LastModifiedBy.Should().Be("staff01");
    }

    [Test]
    public void Handle_AlreadyFinished_ThrowsBusinessException()
    {
        var (item, _) = SeedOrderItem(fulfillmentStatus: OrderItemStatuses.Finished);
        var command = new MarkOrderItemAsFinishedPackageCommand { Id = item.Id };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*hoàn thành*");
    }

    [Test]
    public void Handle_ItemNotFound_ThrowsDataNotFoundException()
    {
        var command = new MarkOrderItemAsFinishedPackageCommand { Id = Guid.NewGuid() };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public async Task Handle_AllItemsFinished_WithShipment_TransitionsShipmentToReady()
    {
        var (item, order) = SeedOrderItem(sourceType: SourceTypes.InStock, withShipment: true);
        var command = new MarkOrderItemAsFinishedPackageCommand { Id = item.Id };

        await _handler.Handle(command, CancellationToken.None);

        var shipment = await _context.Shipments.FirstAsync(s => s.OrderId == order.Id);
        shipment.ShipmentStatus.Should().Be(ShipmentStatuses.ReadyForPickup);
    }

    [Test]
    public async Task Handle_PrintServiceItem_MarksAsFinished()
    {
        var (item, _) = SeedOrderItem(sourceType: SourceTypes.PrintService);
        var command = new MarkOrderItemAsFinishedPackageCommand { Id = item.Id };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.OrderItems.FirstAsync(i => i.Id == item.Id);
        updated.FulfillmentStatus.Should().Be(OrderItemStatuses.Finished);
    }
}
