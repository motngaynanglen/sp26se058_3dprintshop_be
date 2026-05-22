using sp26se058_3dprintshop_be.Application.DesignLogs.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignLogs.Commands;

[TestFixture]
public class CreateDesignLogCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private CreateDesignLogCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _user.Setup(u => u.Role).Returns(Roles.STAFF);
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        _handler = new CreateDesignLogCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (DesignWork work, Customer customer, Guid accountId) SeedDesignWork()
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
            Name = "Work",
            CustomerId = customer.Id,
            Status = DesignWorkStatus.InProgress,
            IsLocked = false,
            RelationshipType = DesignRelationshipType.Original
        };
        _context.DesignWorks.Add(work);
        _context.SaveChanges();

        return (work, customer, accountId);
    }

    [Test]
    public async Task Handle_StaffCreatesCommunicationLog_Succeeds()
    {
        var (work, _, _) = SeedDesignWork();
        var command = new CreateDesignLogCommand
        {
            DesignWorkId = work.Id,
            LogType = DesignLogType.Communication,
            Content = "Ghi chú từ nhân viên"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var log = await _context.DesignLogs.FirstOrDefaultAsync(l => l.DesignWorkId == work.Id);
        log.Should().NotBeNull();
        log!.LogType.Should().Be(DesignLogType.Communication);
        log.Content.Should().Be("Ghi chú từ nhân viên");
    }

    [Test]
    public async Task Handle_StaffCreatesInternalNote_Succeeds()
    {
        var (work, _, _) = SeedDesignWork();
        var command = new CreateDesignLogCommand
        {
            DesignWorkId = work.Id,
            LogType = DesignLogType.InternalNote,
            Content = "Ghi chú nội bộ"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Test]
    public async Task Handle_CustomerCreatesLogForOwnWork_Succeeds()
    {
        var (work, _, accountId) = SeedDesignWork();
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);
        _user.Setup(u => u.Id).Returns(accountId.ToString());

        var command = new CreateDesignLogCommand
        {
            DesignWorkId = work.Id,
            LogType = DesignLogType.Communication,
            Content = "Yêu cầu chỉnh sửa"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Test]
    public void Handle_CustomerCreatesInternalNote_ThrowsForbiddenAccessException()
    {
        var (work, _, accountId) = SeedDesignWork();
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);
        _user.Setup(u => u.Id).Returns(accountId.ToString());

        var command = new CreateDesignLogCommand
        {
            DesignWorkId = work.Id,
            LogType = DesignLogType.InternalNote,
            Content = "Không được phép"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public void Handle_CustomerCreatesLogForOtherWork_ThrowsForbiddenAccessException()
    {
        var (work, _, _) = SeedDesignWork();
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString()); // different customer

        var command = new CreateDesignLogCommand
        {
            DesignWorkId = work.Id,
            LogType = DesignLogType.Communication,
            Content = "Không phải của tôi"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public void Handle_DesignWorkNotFound_ThrowsDataNotFoundException()
    {
        var command = new CreateDesignLogCommand
        {
            DesignWorkId = Guid.NewGuid(),
            LogType = DesignLogType.Communication,
            Content = "Ghost work"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Handle_ParentLogNotFound_ThrowsDataNotFoundException()
    {
        var (work, _, _) = SeedDesignWork();
        var command = new CreateDesignLogCommand
        {
            DesignWorkId = work.Id,
            LogType = DesignLogType.Communication,
            Content = "Reply to ghost",
            ParentLogId = Guid.NewGuid()
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
