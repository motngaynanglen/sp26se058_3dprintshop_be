using sp26se058_3dprintshop_be.Application.Transactions.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Transactions.Commands;

[TestFixture]
public class ConfirmManualPaymentCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IOrderWorkflowService> _orderWorkflowService = null!;
    private Mock<IUser> _user = null!;
    private ConfirmManualPaymentCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _orderWorkflowService = new Mock<IOrderWorkflowService>();
        _orderWorkflowService
            .Setup(s => s.ActivateOrderWorkflowAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _handler = new ConfirmManualPaymentCommandHandler(_context, _orderWorkflowService.Object, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Order SeedOrder(
        string orderStatus = OrderStatuses.Pending,
        bool hasInvoice = false,
        string invoicePaymentStatus = InvoiceStatuses.Unpaid)
    {
        var customer = new Customer { Id = Guid.NewGuid(), AccountId = Guid.NewGuid() };
        _context.Customers.Add(customer);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = $"ORD-MAN-{Guid.NewGuid():N}".Substring(0, 14),
            CustomerId = customer.Id,
            Customer = customer,
            OrderStatus = orderStatus,
            TotalPrice = 150_000
        };
        _context.Orders.Add(order);

        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            Order = order,
            SourceType = SourceTypes.InStock,
            ItemName = "Sản phẩm test",
            QuantityOrdered = 1,
            UnitPrice = 150_000,
            TotalPrice = 150_000,
            FulfillmentStatus = OrderItemStatuses.Pending
        };
        _context.OrderItems.Add(orderItem);

        if (hasInvoice)
        {
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                InvoiceCode = $"INV-{order.Code}",
                TotalAmount = order.TotalPrice,
                PaymentStatus = invoicePaymentStatus
            };
            _context.Invoices.Add(invoice);
        }

        _context.SaveChanges();
        return order;
    }

    [Test]
    public async Task Handle_CashPayment_CreatesTransactionAndMarksInvoicePaid()
    {
        var order = SeedOrder();
        var command = new ConfirmManualPaymentCommand
        {
            OrderId = order.Id,
            PaymentMethod = PaymentMethods.Cash
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.PaymentMethod == PaymentMethods.Cash);
        transaction.Should().NotBeNull();
        transaction!.TransactionStatus.Should().Be(TransactionStatuses.Success);

        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == order.Id);
        invoice.Should().NotBeNull();
        invoice!.PaymentStatus.Should().Be(InvoiceStatuses.Paid);
    }

    [Test]
    public async Task Handle_BankTransferPayment_CreatesTransaction()
    {
        var order = SeedOrder();
        var command = new ConfirmManualPaymentCommand
        {
            OrderId = order.Id,
            PaymentMethod = PaymentMethods.BankTransfer,
            ExternalReference = "REF-12345"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var transaction = await _context.Transactions.FirstOrDefaultAsync();
        transaction.Should().NotBeNull();
        transaction!.ExternalTransactionId.Should().Be("REF-12345");
    }

    [Test]
    public async Task Handle_NoExistingInvoice_CreatesInvoiceAutomatically()
    {
        var order = SeedOrder(hasInvoice: false);
        var command = new ConfirmManualPaymentCommand
        {
            OrderId = order.Id,
            PaymentMethod = PaymentMethods.Cash
        };

        await _handler.Handle(command, CancellationToken.None);

        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == order.Id);
        invoice.Should().NotBeNull();
        invoice!.InvoiceCode.Should().Contain(order.Code);
    }

    [Test]
    public async Task Handle_ExistingUnpaidInvoice_ReusesIt()
    {
        var order = SeedOrder(hasInvoice: true, invoicePaymentStatus: InvoiceStatuses.Unpaid);
        var command = new ConfirmManualPaymentCommand
        {
            OrderId = order.Id,
            PaymentMethod = PaymentMethods.Cash
        };

        await _handler.Handle(command, CancellationToken.None);

        var invoiceCount = await _context.Invoices.CountAsync(i => i.OrderId == order.Id);
        invoiceCount.Should().Be(1);
    }

    [Test]
    public void Handle_InvalidPaymentMethod_ThrowsBusinessException()
    {
        var order = SeedOrder();
        var command = new ConfirmManualPaymentCommand
        {
            OrderId = order.Id,
            PaymentMethod = PaymentMethods.PAYOS
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*không hỗ trợ*");
    }

    [Test]
    public void Handle_OrderNotPending_ThrowsBusinessException()
    {
        var order = SeedOrder(orderStatus: OrderStatuses.Processing);
        var command = new ConfirmManualPaymentCommand
        {
            OrderId = order.Id,
            PaymentMethod = PaymentMethods.Cash
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*chờ thanh toán*");
    }

    [Test]
    public void Handle_AlreadyPaidInvoice_ThrowsBusinessException()
    {
        var order = SeedOrder(hasInvoice: true, invoicePaymentStatus: InvoiceStatuses.Paid);
        var command = new ConfirmManualPaymentCommand
        {
            OrderId = order.Id,
            PaymentMethod = PaymentMethods.Cash
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*thanh toán*");
    }

    [Test]
    public void Handle_OrderNotFound_ThrowsDataNotFoundException()
    {
        var command = new ConfirmManualPaymentCommand
        {
            OrderId = Guid.NewGuid(),
            PaymentMethod = PaymentMethods.Cash
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public async Task Handle_ValidPayment_CallsOrderWorkflowService()
    {
        var order = SeedOrder();
        var command = new ConfirmManualPaymentCommand
        {
            OrderId = order.Id,
            PaymentMethod = PaymentMethods.Cash
        };

        await _handler.Handle(command, CancellationToken.None);

        _orderWorkflowService.Verify(
            s => s.ActivateOrderWorkflowAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
