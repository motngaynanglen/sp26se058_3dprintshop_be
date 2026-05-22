using sp26se058_3dprintshop_be.Application.Orders.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Orders.Commands;

[TestFixture]
public class CancelOrderCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IUser> _user = null!;
    private Mock<IPaymentService> _paymentService = null!;
    private CancelOrderCommandHandler _handler = null!;

    private Guid _customerId;
    private Guid _accountId;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _user = new Mock<IUser>();
        _paymentService = new Mock<IPaymentService>();
        _paymentService.Setup(p => p.CancelPaymentLink(It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _handler = new CancelOrderCommandHandler(_context, _user.Object, _paymentService.Object);

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

    private (Order order, Invoice invoice) SeedPendingOrder(
        string orderStatus = "PENDING",
        string invoiceStatus = "UNPAID")
    {
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            Code = "ORD-001",
            CustomerId = _customerId,
            OrderStatus = orderStatus,
            TotalPrice = 100_000
        };

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            InvoiceCode = "INV-001",
            PaymentStatus = invoiceStatus,
            TotalAmount = 100_000
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Order = order,
            SourceType = SourceTypes.InStock,
            ItemName = "Sản phẩm test",
            QuantityOrdered = 1,
            UnitPrice = 100_000,
            TotalPrice = 100_000,
            FulfillmentStatus = OrderItemStatuses.Pending
        };

        _context.Orders.Add(order);
        _context.Invoices.Add(invoice);
        _context.OrderItems.Add(orderItem);
        _context.SaveChanges();

        return (order, invoice);
    }

    [Test]
    public async Task Handle_CustomerCancelsOwnPendingUnpaidOrder_ReturnsTrue()
    {
        var (order, _) = SeedPendingOrder();
        _user.Setup(u => u.Id).Returns(_accountId.ToString());
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);
        _user.Setup(u => u.Username).Returns("customer01");

        var result = await _handler.Handle(
            new CancelOrderCommand { OrderId = order.Id }, CancellationToken.None);

        result.Should().BeTrue();
        var updated = await _context.Orders.FirstAsync(o => o.Id == order.Id);
        updated.OrderStatus.Should().Be(OrderStatuses.Cancelled);
    }

    [Test]
    public void Handle_CustomerCancelsAnotherCustomersOrder_ThrowsForbidden()
    {
        var (order, _) = SeedPendingOrder();
        var otherId = Guid.NewGuid();
        _user.Setup(u => u.Id).Returns(otherId.ToString());
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);

        Func<Task> act = async () => await _handler.Handle(
            new CancelOrderCommand { OrderId = order.Id }, CancellationToken.None);

        act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public void Handle_CancelAlreadyCancelledOrder_ThrowsBusinessException()
    {
        var (order, _) = SeedPendingOrder(orderStatus: OrderStatuses.Cancelled);
        _user.Setup(u => u.Id).Returns(_accountId.ToString());
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);

        Func<Task> act = async () => await _handler.Handle(
            new CancelOrderCommand { OrderId = order.Id }, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được hủy*");
    }

    [Test]
    public void Handle_CancelPaidOrder_ThrowsBusinessException()
    {
        var (order, _) = SeedPendingOrder(invoiceStatus: InvoiceStatuses.Paid);
        _user.Setup(u => u.Id).Returns(_accountId.ToString());
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);

        Func<Task> act = async () => await _handler.Handle(
            new CancelOrderCommand { OrderId = order.Id }, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*chưa thanh toán*");
    }

    [Test]
    public void Handle_CancelNonPendingOrder_ThrowsBusinessException()
    {
        var (order, _) = SeedPendingOrder(orderStatus: OrderStatuses.Processing);
        _user.Setup(u => u.Id).Returns(_accountId.ToString());
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);

        Func<Task> act = async () => await _handler.Handle(
            new CancelOrderCommand { OrderId = order.Id }, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>();
    }

    [Test]
    public async Task Handle_StaffCancelsAnyPendingUnpaidOrder_Succeeds()
    {
        var (order, _) = SeedPendingOrder();
        var staffId = Guid.NewGuid();
        _user.Setup(u => u.Id).Returns(staffId.ToString());
        _user.Setup(u => u.Role).Returns(Roles.STAFF);
        _user.Setup(u => u.Username).Returns("staff01");

        var result = await _handler.Handle(
            new CancelOrderCommand { OrderId = order.Id }, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    public async Task Handle_ManagerCancelsOrder_Succeeds()
    {
        var (order, _) = SeedPendingOrder();
        var managerId = Guid.NewGuid();
        _user.Setup(u => u.Id).Returns(managerId.ToString());
        _user.Setup(u => u.Role).Returns(Roles.MANAGER);
        _user.Setup(u => u.Username).Returns("mgr01");

        var result = await _handler.Handle(
            new CancelOrderCommand { OrderId = order.Id }, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    public void Handle_OrderNotFound_ThrowsDataNotFoundException()
    {
        _user.Setup(u => u.Id).Returns(_accountId.ToString());
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);

        Func<Task> act = async () => await _handler.Handle(
            new CancelOrderCommand { OrderId = Guid.NewGuid() }, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public async Task Handle_InStockItemCancelled_InvoiceStatusBecomeCancelled()
    {
        var (order, _) = SeedPendingOrder();
        _user.Setup(u => u.Id).Returns(_accountId.ToString());
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);
        _user.Setup(u => u.Username).Returns("customer01");

        await _handler.Handle(new CancelOrderCommand { OrderId = order.Id }, CancellationToken.None);

        var invoice = await _context.Invoices.FirstAsync(i => i.OrderId == order.Id);
        invoice.PaymentStatus.Should().Be(InvoiceStatuses.Cancelled);
    }
}
