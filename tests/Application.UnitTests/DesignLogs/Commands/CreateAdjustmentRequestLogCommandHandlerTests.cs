using sp26se058_3dprintshop_be.Application.DesignLogs.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignLogs.Commands;

[TestFixture]
public class CreateAdjustmentRequestLogCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private CreateAdjustmentRequestLogCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _user.Setup(u => u.Role).Returns(Roles.STAFF);
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        _handler = new CreateAdjustmentRequestLogCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (DesignWork work, Guid accountId) SeedWork(
        string status = DesignWorkStatus.InProgress,
        bool isLocked = false)
    {
        var accountId = Guid.NewGuid();
        _context.Accounts.Add(new Account
        {
            Id = accountId,
            Username = "cust01",
            Email = "cust01@test.com",
            PasswordHash = "hash",
            Fullname = "Customer"
        });

        var customer = new Customer { Id = Guid.NewGuid(), AccountId = accountId };
        _context.Customers.Add(customer);

        var work = new DesignWork
        {
            Id = Guid.NewGuid(),
            Name = "Design Work",
            CustomerId = customer.Id,
            Status = status,
            IsLocked = isLocked,
            RelationshipType = DesignRelationshipType.Original
        };
        _context.DesignWorks.Add(work);
        _context.SaveChanges();

        return (work, accountId);
    }

    [Test]
    public async Task Handle_StaffCreatesForInProgressWork_Succeeds()
    {
        var (work, _) = SeedWork();
        var command = new CreateAdjustmentRequestLogCommand
        {
            DesignWorkId = work.Id,
            Content = "Yêu cầu hiệu chỉnh từ staff"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var log = await _context.DesignLogs.FirstAsync(l => l.DesignWorkId == work.Id);
        log.LogType.Should().Be(DesignLogType.AdjustmentRequest);
        log.AdjustmentRequestStatus.Should().Be(AdjustmentRequestStatuses.PendingReview);
    }

    [Test]
    public async Task Handle_CustomerCreatesForOwnWork_Succeeds()
    {
        var (work, accountId) = SeedWork();
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);
        _user.Setup(u => u.Id).Returns(accountId.ToString());

        var command = new CreateAdjustmentRequestLogCommand
        {
            DesignWorkId = work.Id,
            Content = "Khách muốn chỉnh sửa"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Test]
    public void Handle_CustomerCreatesForOtherWork_ThrowsForbiddenAccessException()
    {
        var (work, _) = SeedWork();
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());

        var command = new CreateAdjustmentRequestLogCommand
        {
            DesignWorkId = work.Id,
            Content = "Không phải của tôi"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public void Handle_LockedWork_ThrowsBusinessException()
    {
        var (work, _) = SeedWork(isLocked: true);
        var command = new CreateAdjustmentRequestLogCommand
        {
            DesignWorkId = work.Id,
            Content = "Đã khóa"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*khóa*");
    }

    [Test]
    public void Handle_SketchingStatus_ThrowsBusinessException()
    {
        var (work, _) = SeedWork(status: DesignWorkStatus.Sketching);
        var command = new CreateAdjustmentRequestLogCommand
        {
            DesignWorkId = work.Id,
            Content = "Quá sớm"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*chờ duyệt*");
    }

    [Test]
    public void Handle_WorkNotFound_ThrowsDataNotFoundException()
    {
        var command = new CreateAdjustmentRequestLogCommand
        {
            DesignWorkId = Guid.NewGuid(),
            Content = "Ghost work"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
