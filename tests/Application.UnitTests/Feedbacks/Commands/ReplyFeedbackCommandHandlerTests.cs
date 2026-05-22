using sp26se058_3dprintshop_be.Application.Feedbacks.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Feedbacks.Commands;

[TestFixture]
public class ReplyFeedbackCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private ReplyFeedbackCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");

        _handler = new ReplyFeedbackCommandHandler(_context, _user.Object, _mapper);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Feedback SeedFeedback(string? existingReply = null)
    {
        var customerId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Username = "feedback_customer",
            Email = "fc@test.com",
            PasswordHash = "hash",
            Fullname = "Feedback Customer"
        };
        _context.Accounts.Add(account);

        var customer = new Customer { Id = customerId, AccountId = account.Id };
        _context.Customers.Add(customer);

        var template = new DesignTemplate
        {
            Id = templateId,
            Code = "TPL-001",
            Name = "Test Template",
            FileUrl = "https://storage.test/template.stl",
            CatalogStatus = CatalogStatuses.Published,
            IsActive = true
        };
        _context.DesignTemplates.Add(template);

        var order = new Order
        {
            Id = orderId,
            Code = "ORD-FB-001",
            CustomerId = customerId,
            OrderStatus = OrderStatuses.Completed,
            TotalPrice = 100_000
        };
        _context.Orders.Add(order);

        var orderItem = new OrderItem
        {
            Id = orderItemId,
            OrderId = orderId,
            Order = order,
            SourceType = SourceTypes.InStock,
            ItemName = "Sản phẩm đã đánh giá",
            QuantityOrdered = 1,
            UnitPrice = 100_000,
            TotalPrice = 100_000,
            FulfillmentStatus = OrderItemStatuses.Finished
        };
        _context.OrderItems.Add(orderItem);

        var feedback = new Feedback
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            DesignTemplateId = templateId,
            OrderItemId = orderItemId,
            Rating = 5,
            Comment = "Rất hài lòng!",
            StaffReply = existingReply,
            IsHidden = false
        };
        _context.Feedbacks.Add(feedback);
        _context.SaveChanges();

        return feedback;
    }

    [Test]
    public async Task Handle_WithValidFeedbackId_SetsStaffReply()
    {
        var feedback = SeedFeedback();
        var command = new ReplyFeedbackCommand
        {
            Id = feedback.Id,
            StaffReply = "Cảm ơn bạn đã đánh giá!"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.Feedbacks.FirstAsync(f => f.Id == feedback.Id);
        updated.StaffReply.Should().Be("Cảm ơn bạn đã đánh giá!");
    }

    [Test]
    public async Task Handle_WithValidFeedbackId_SetsRepliedDate()
    {
        var feedback = SeedFeedback();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        await _handler.Handle(
            new ReplyFeedbackCommand { Id = feedback.Id, StaffReply = "Phản hồi" },
            CancellationToken.None);

        var updated = await _context.Feedbacks.FirstAsync(f => f.Id == feedback.Id);
        updated.RepliedDate.Should().NotBeNull();
        updated.RepliedDate!.Value.Should().BeAfter(before);
    }

    [Test]
    public async Task Handle_WithValidFeedbackId_UpdatesLastModifiedBy()
    {
        var feedback = SeedFeedback();

        await _handler.Handle(
            new ReplyFeedbackCommand { Id = feedback.Id, StaffReply = "Ok" },
            CancellationToken.None);

        var updated = await _context.Feedbacks.FirstAsync(f => f.Id == feedback.Id);
        updated.LastModifiedBy.Should().Be("staff01");
    }

    [Test]
    public void Handle_WithNonExistentFeedbackId_ThrowsDataNotFoundException()
    {
        var command = new ReplyFeedbackCommand
        {
            Id = Guid.NewGuid(),
            StaffReply = "Phản hồi không tồn tại"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public async Task Handle_CanOverwriteExistingReply()
    {
        var feedback = SeedFeedback(existingReply: "Phản hồi cũ");
        var command = new ReplyFeedbackCommand
        {
            Id = feedback.Id,
            StaffReply = "Phản hồi mới"
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.Feedbacks.FirstAsync(f => f.Id == feedback.Id);
        updated.StaffReply.Should().Be("Phản hồi mới");
    }
}
