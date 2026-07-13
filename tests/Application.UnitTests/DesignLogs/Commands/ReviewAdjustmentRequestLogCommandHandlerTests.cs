using sp26se058_3dprintshop_be.Application.DesignLogs.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignLogs.Commands;

[TestFixture]
public class ReviewAdjustmentRequestLogCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private ReviewAdjustmentRequestLogCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        _handler = new ReviewAdjustmentRequestLogCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (DesignLog log, DesignWork work) SeedLog(
        string workStatus = DesignWorkStatus.Reviewing,
        bool workIsLocked = false,
        string logStatus = AdjustmentRequestStatuses.PendingReview,
        string logType = DesignLogType.AdjustmentRequest)
    {
        var work = new DesignWork
        {
            Id = Guid.NewGuid(),
            Name = "Test Work",
            CustomerId = Guid.NewGuid(),
            Status = workStatus,
            IsLocked = workIsLocked,
            RelationshipType = DesignRelationshipType.Original
        };
        _context.DesignWorks.Add(work);

        var log = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = work.Id,
            DesignWork = work,
            LogType = logType,
            AdjustmentRequestStatus = logStatus,
            Content = "Yêu cầu hiệu chỉnh"
        };
        _context.DesignLogs.Add(log);
        _context.SaveChanges();
        return (log, work);
    }

    private void SeedPaidServiceSelection(Guid designWorkId, DesignWork work)
    {
        var customer = new Customer { Id = Guid.NewGuid(), AccountId = Guid.NewGuid() };
        _context.Customers.Add(customer);

        var orderId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            InvoiceCode = "INV-TEST-001",
            PaymentStatus = InvoiceStatuses.Paid
        };

        var order = new Order
        {
            Id = orderId,
            Code = "ORD-TEST-001",
            CustomerId = customer.Id,
            Customer = customer,
            OrderStatus = OrderStatuses.Processing,
            Invoice = invoice
        };

        _context.Invoices.Add(invoice);
        _context.Orders.Add(order);

        var selection = new ServiceSelection
        {
            Id = Guid.NewGuid(),
            DesignWorkId = designWorkId,
            DesignWork = work,
            IsLocked = true,
            AdjustmentRoundLimit = 3,
            UsedAdjustmentRoundCount = 0
        };
        _context.ServiceSelections.Add(selection);

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            ServiceSelectionId = selection.Id,
            ItemName = "Dịch vụ thiết kế",
            SourceType = "SERVICE",
            QuantityOrdered = 1,
            UnitPrice = 500_000m,
            TotalPrice = 500_000m,
            FulfillmentStatus = "PENDING"
        };
        _context.OrderItems.Add(orderItem);
        _context.SaveChanges();
    }

    // ── Rejection path ────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_RejectRequest_SetsStatusRejected()
    {
        var (log, _) = SeedLog();
        var command = new ReviewAdjustmentRequestLogCommand
        {
            Id = log.Id,
            IsApproved = false,
            DecisionNote = "Vượt phạm vi brief"
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.DesignLogs.FirstAsync(l => l.Id == log.Id);
        updated.AdjustmentRequestStatus.Should().Be(AdjustmentRequestStatuses.Rejected);
    }

    [Test]
    public async Task Handle_RejectRequest_StoresCustomDecisionNote()
    {
        var (log, _) = SeedLog();
        var command = new ReviewAdjustmentRequestLogCommand
        {
            Id = log.Id,
            IsApproved = false,
            DecisionNote = "Lý do từ chối cụ thể"
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.DesignLogs.FirstAsync(l => l.Id == log.Id);
        updated.AdjustmentDecisionNote.Should().Be("Lý do từ chối cụ thể");
    }

    [Test]
    public async Task Handle_RejectRequest_SetsReviewMetadata()
    {
        var reviewerId = Guid.NewGuid();
        _user.Setup(u => u.Id).Returns(reviewerId.ToString());

        var (log, _) = SeedLog();
        var command = new ReviewAdjustmentRequestLogCommand
        {
            Id = log.Id,
            IsApproved = false,
            DecisionNote = "Lý do từ chối"
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.DesignLogs.FirstAsync(l => l.Id == log.Id);
        updated.AdjustmentReviewedByAccountId.Should().Be(reviewerId);
        updated.AdjustmentReviewedAt.Should().NotBeNull();
        updated.LastModifiedBy.Should().Be("staff01");
    }

    // ── Approval path ─────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_ApproveRequest_WithValidServiceSelection_ConsumesRound()
    {
        var (log, work) = SeedLog(workStatus: DesignWorkStatus.Reviewing);
        SeedPaidServiceSelection(work.Id, work);

        var command = new ReviewAdjustmentRequestLogCommand
        {
            Id = log.Id,
            IsApproved = true
        };

        await _handler.Handle(command, CancellationToken.None);

        var selection = await _context.ServiceSelections.FirstAsync(s => s.DesignWorkId == work.Id);
        selection.UsedAdjustmentRoundCount.Should().Be(1);

        var updatedLog = await _context.DesignLogs.FirstAsync(l => l.Id == log.Id);
        updatedLog.AdjustmentRequestStatus.Should().Be(AdjustmentRequestStatuses.Approved);
        updatedLog.AdjustmentConsumedServiceSelectionId.Should().Be(selection.Id);
    }

    [Test]
    public async Task Handle_ApproveRequest_WithValidServiceSelection_SetsWorkStatusToInProgress()
    {
        var (log, work) = SeedLog(workStatus: DesignWorkStatus.Reviewing);
        SeedPaidServiceSelection(work.Id, work);

        var command = new ReviewAdjustmentRequestLogCommand
        {
            Id = log.Id,
            IsApproved = true
        };

        await _handler.Handle(command, CancellationToken.None);

        var updatedWork = await _context.DesignWorks.FirstAsync(w => w.Id == work.Id);
        updatedWork.Status.Should().Be(DesignWorkStatus.InProgress);
    }

    [Test]
    public void Handle_ApproveRequest_NoServiceSelection_ThrowsBusinessException()
    {
        var (log, _) = SeedLog(workStatus: DesignWorkStatus.Reviewing);
        // No ServiceSelection seeded
        var command = new ReviewAdjustmentRequestLogCommand
        {
            Id = log.Id,
            IsApproved = true
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*lượt hiệu chỉnh*");
    }

    [Test]
    public void Handle_ApproveRequest_LockedWork_ThrowsBusinessException()
    {
        var (log, work) = SeedLog(workStatus: DesignWorkStatus.Reviewing, workIsLocked: true);
        SeedPaidServiceSelection(work.Id, work);

        var command = new ReviewAdjustmentRequestLogCommand
        {
            Id = log.Id,
            IsApproved = true
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*khóa*");
    }

    [Test]
    public void Handle_ApproveRequest_WrongWorkStatus_ThrowsBusinessException()
    {
        // InProgress is not in [Reviewing, Completed]
        var (log, _) = SeedLog(workStatus: DesignWorkStatus.InProgress);
        var command = new ReviewAdjustmentRequestLogCommand
        {
            Id = log.Id,
            IsApproved = true
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*chờ duyệt*");
    }

    // ── Guard clauses ─────────────────────────────────────────────────────────

    [Test]
    public void Handle_NotAdjustmentRequestLog_ThrowsBusinessException()
    {
        var (log, _) = SeedLog(logType: DesignLogType.Communication);
        var command = new ReviewAdjustmentRequestLogCommand
        {
            Id = log.Id,
            IsApproved = false,
            DecisionNote = "Reason"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*yêu cầu hiệu chỉnh*");
    }

    [Test]
    public void Handle_AlreadyProcessed_ThrowsBusinessException()
    {
        var (log, _) = SeedLog(logStatus: AdjustmentRequestStatuses.Approved);
        var command = new ReviewAdjustmentRequestLogCommand
        {
            Id = log.Id,
            IsApproved = false,
            DecisionNote = "Reason"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được xử lý*");
    }

    [Test]
    public void Handle_LogNotFound_ThrowsDataNotFoundException()
    {
        var command = new ReviewAdjustmentRequestLogCommand
        {
            Id = Guid.NewGuid(),
            IsApproved = false,
            DecisionNote = "Reason"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
