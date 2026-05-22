using sp26se058_3dprintshop_be.Application.DesignWorks.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignWorks.Commands;

[TestFixture]
public class UpdateDesignWorkIsApproveCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IUser> _user = null!;
    private UpdateDesignWorkIsApproveCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("customer01");
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);
        _handler = new UpdateDesignWorkIsApproveCommandHandler(_context, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (DesignVersionHistory version, Customer customer) SeedDesignVersionHistory(
        bool workIsLocked = false,
        bool ownsWork = true)
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

        var work = new DesignWork
        {
            Id = Guid.NewGuid(),
            Name = "Test Design",
            CustomerId = customer.Id,
            Status = DesignWorkStatus.Sketching,
            IsLocked = workIsLocked,
            RelationshipType = DesignRelationshipType.Original
        };
        _context.DesignWorks.Add(work);

        var version = new DesignVersionHistory
        {
            Id = Guid.NewGuid(),
            DesignWorkId = work.Id,
            DesignWork = work,
            FileUrl = "https://storage.test/design.stl",
            VersionNumber = 1,
            UploaderId = accountId,
            IsApproved = false
        };
        _context.DesignVersionHistorys.Add(version);
        _context.SaveChanges();

        if (ownsWork)
            _user.Setup(u => u.Id).Returns(accountId.ToString());
        else
            _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());

        return (version, customer);
    }

    [Test]
    public async Task Handle_CustomerOwnsWork_NotLocked_ApprovesVersion()
    {
        var (version, _) = SeedDesignVersionHistory(workIsLocked: false, ownsWork: true);

        var result = await _handler.Handle(
            new UpdateDesignWorkIsApproveCommand { Id = version.Id }, CancellationToken.None);

        result.Should().BeTrue();
        var updated = await _context.DesignVersionHistorys.FirstAsync(v => v.Id == version.Id);
        updated.IsApproved.Should().BeTrue();
    }

    [Test]
    public async Task Handle_CustomerOwnsWork_NotLocked_LocksDesignWork()
    {
        var (version, _) = SeedDesignVersionHistory(workIsLocked: false, ownsWork: true);

        await _handler.Handle(
            new UpdateDesignWorkIsApproveCommand { Id = version.Id }, CancellationToken.None);

        var updatedWork = await _context.DesignWorks.FirstAsync(w => w.Id == version.DesignWorkId);
        updatedWork.IsLocked.Should().BeTrue();
        updatedWork.Status.Should().Be(DesignWorkStatus.Completed);
    }

    [Test]
    public void Handle_WorkAlreadyLocked_ThrowsBusinessException()
    {
        var (version, _) = SeedDesignVersionHistory(workIsLocked: true, ownsWork: true);

        Func<Task> act = async () => await _handler.Handle(
            new UpdateDesignWorkIsApproveCommand { Id = version.Id }, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã bị khóa*");
    }

    [Test]
    public void Handle_CustomerDoesNotOwnWork_ThrowsForbiddenAccessException()
    {
        var (version, _) = SeedDesignVersionHistory(workIsLocked: false, ownsWork: false);

        Func<Task> act = async () => await _handler.Handle(
            new UpdateDesignWorkIsApproveCommand { Id = version.Id }, CancellationToken.None);

        act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public void Handle_VersionNotFound_ThrowsDataNotFoundException()
    {
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());

        Func<Task> act = async () => await _handler.Handle(
            new UpdateDesignWorkIsApproveCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
