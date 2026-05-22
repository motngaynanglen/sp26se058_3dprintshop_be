using sp26se058_3dprintshop_be.Application.Transactions.Command;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Transactions.Commands;

[TestFixture]
public class CancelTransactionCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IPaymentService> _paymentService = null!;
    private Mock<IUser> _user = null!;
    private CancelTransactionCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _paymentService = new Mock<IPaymentService>();
        _paymentService.Setup(p => p.CancelPaymentLink(It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _user.Setup(u => u.Role).Returns(Roles.STAFF);
        _handler = new CancelTransactionCommandHandler(_context, _paymentService.Object, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (Transaction transaction, Guid customerId) SeedPendingTransaction(
        string transactionStatus = TransactionStatuses.Pending,
        string invoiceStatus = InvoiceStatuses.Unpaid)
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
            Code = "ORD-TX-001",
            CustomerId = customer.Id,
            Customer = customer,
            OrderStatus = OrderStatuses.Pending,
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
            FulfillmentStatus = OrderItemStatuses.Pending
        };
        _context.OrderItems.Add(orderItem);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            InvoiceCode = "INV-001",
            TotalAmount = 100_000,
            PaymentStatus = invoiceStatus
        };
        _context.Invoices.Add(invoice);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            Invoice = invoice,
            Amount = 100_000,
            PaymentMethod = PaymentMethods.PAYOS,
            InternalCode = "123456789",
            TransactionStatus = transactionStatus
        };
        _context.Transactions.Add(transaction);
        _context.SaveChanges();

        return (transaction, accountId);
    }

    [Test]
    public async Task Handle_Staff_CancelsPendingTransaction()
    {
        var (transaction, _) = SeedPendingTransaction();

        var result = await _handler.Handle(
            new CancelTransactionCommand { TransactionId = transaction.Id },
            CancellationToken.None);

        result.Should().BeTrue();
        var updated = await _context.Transactions.FirstAsync(t => t.Id == transaction.Id);
        updated.TransactionStatus.Should().Be(TransactionStatuses.Cancelled);
    }

    [Test]
    public async Task Handle_Staff_SetsNoteWithUsername()
    {
        var (transaction, _) = SeedPendingTransaction();

        await _handler.Handle(
            new CancelTransactionCommand { TransactionId = transaction.Id, Reason = "Test cancel" },
            CancellationToken.None);

        var updated = await _context.Transactions.FirstAsync(t => t.Id == transaction.Id);
        updated.Note.Should().Contain("staff01");
        updated.Note.Should().Contain("Test cancel");
    }

    [Test]
    public void Handle_AlreadySucceeded_ThrowsBusinessException()
    {
        var (transaction, _) = SeedPendingTransaction(transactionStatus: TransactionStatuses.Success);

        Func<Task> act = async () => await _handler.Handle(
            new CancelTransactionCommand { TransactionId = transaction.Id },
            CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được thanh toán thành công*");
    }

    [Test]
    public void Handle_AlreadyCancelled_ThrowsBusinessException()
    {
        var (transaction, _) = SeedPendingTransaction(transactionStatus: TransactionStatuses.Cancelled);

        Func<Task> act = async () => await _handler.Handle(
            new CancelTransactionCommand { TransactionId = transaction.Id },
            CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được hủy từ trước*");
    }

    [Test]
    public void Handle_TransactionNotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _handler.Handle(
            new CancelTransactionCommand { TransactionId = Guid.NewGuid() },
            CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Handle_CustomerCancelsAnotherCustomerTransaction_ThrowsForbiddenAccessException()
    {
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString()); // different customer

        var (transaction, _) = SeedPendingTransaction();

        Func<Task> act = async () => await _handler.Handle(
            new CancelTransactionCommand { TransactionId = transaction.Id },
            CancellationToken.None);

        act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
