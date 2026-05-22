using sp26se058_3dprintshop_be.Application.DesignWorks.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignWorks.Commands;

[TestFixture]
public class LockDesignWorkCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private LockDesignWorkCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _user.Setup(u => u.Role).Returns(Roles.STAFF);
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        _handler = new LockDesignWorkCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (DesignWork work, Customer customer) SeedDesignWork(bool isLocked = false)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Username = "cust01",
            Email = "cust01@test.com",
            PasswordHash = "hash",
            Fullname = "Customer"
        };
        _context.Accounts.Add(account);

        var customer = new Customer { Id = Guid.NewGuid(), AccountId = account.Id };
        _context.Customers.Add(customer);

        var work = new DesignWork
        {
            Id = Guid.NewGuid(),
            Name = "Test Design",
            CustomerId = customer.Id,
            Status = DesignWorkStatus.Sketching,
            IsLocked = isLocked,
            RelationshipType = DesignRelationshipType.Original
        };
        _context.DesignWorks.Add(work);
        _context.SaveChanges();

        return (work, customer);
    }

    [Test]
    public async Task Handle_Staff_LocksDesignWork()
    {
        var (work, _) = SeedDesignWork(isLocked: false);

        var result = await _handler.Handle(
            new LockDesignWorkCommand { Id = work.Id }, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.DesignWorks.FirstAsync(w => w.Id == work.Id);
        updated.IsLocked.Should().BeTrue();
    }

    [Test]
    public async Task Handle_Staff_LocksDesignWork_CreatesDesignLog()
    {
        var (work, _) = SeedDesignWork(isLocked: false);

        await _handler.Handle(new LockDesignWorkCommand { Id = work.Id }, CancellationToken.None);

        var log = await _context.DesignLogs.FirstOrDefaultAsync(l => l.DesignWorkId == work.Id);
        log.Should().NotBeNull();
        log!.LogType.Should().Be(DesignLogType.StatusChange);
    }

    [Test]
    public async Task Handle_AlreadyLocked_ReturnsWithoutChanging()
    {
        var (work, _) = SeedDesignWork(isLocked: true);

        var result = await _handler.Handle(
            new LockDesignWorkCommand { Id = work.Id }, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.DesignWorks.FirstAsync(w => w.Id == work.Id);
        updated.IsLocked.Should().BeTrue();
        var logCount = await _context.DesignLogs.CountAsync(l => l.DesignWorkId == work.Id);
        logCount.Should().Be(0); // no log created since already locked
    }

    [Test]
    public void Handle_DesignWorkNotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _handler.Handle(
            new LockDesignWorkCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Handle_CustomerDoesNotOwnDesignWork_ThrowsForbiddenAccessException()
    {
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString()); // different customer

        var (work, _) = SeedDesignWork(isLocked: false);

        Func<Task> act = async () => await _handler.Handle(
            new LockDesignWorkCommand { Id = work.Id }, CancellationToken.None);

        act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
