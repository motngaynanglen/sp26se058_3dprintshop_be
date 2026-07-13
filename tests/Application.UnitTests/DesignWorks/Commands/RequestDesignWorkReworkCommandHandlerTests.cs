using sp26se058_3dprintshop_be.Application.DesignWorks.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignWorks.Commands;

[TestFixture]
public class RequestDesignWorkReworkCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private RequestDesignWorkReworkCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("customer01");
        _user.Setup(u => u.Role).Returns(Roles.CUSTOMER);
        _handler = new RequestDesignWorkReworkCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (DesignWork work, Customer customer) SeedDesignWork()
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
            Status = DesignWorkStatus.Completed,
            IsLocked = false,
            RelationshipType = DesignRelationshipType.Original
        };
        _context.DesignWorks.Add(work);
        _context.SaveChanges();

        _user.Setup(u => u.Id).Returns(accountId.ToString());
        return (work, customer);
    }

    [Test]
    public async Task Handle_ValidRequest_CreatesRevisionDesignWork()
    {
        var (work, _) = SeedDesignWork();
        var command = new RequestDesignWorkReworkCommand
        {
            Id = work.Id,
            Note = "Chỉnh lại chi tiết khuôn mặt"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var revisionWorks = await _context.DesignWorks
            .Where(w => w.ParentDesignWorkId == work.Id)
            .ToListAsync();
        revisionWorks.Should().HaveCount(1);
        revisionWorks.First().RelationshipType.Should().Be(DesignRelationshipType.Revision);
    }

    [Test]
    public async Task Handle_ValidRequest_SetsParentDesignWorkId()
    {
        var (work, _) = SeedDesignWork();

        var result = await _handler.Handle(
            new RequestDesignWorkReworkCommand { Id = work.Id, Note = "Sửa thêm" },
            CancellationToken.None);

        var revision = await _context.DesignWorks
            .FirstAsync(w => w.ParentDesignWorkId == work.Id);
        revision.ParentDesignWorkId.Should().Be(work.Id);
        revision.Status.Should().Be(DesignWorkStatus.Sketching);
    }

    [Test]
    public async Task Handle_ValidRequest_CreatesDesignLogs()
    {
        var (work, _) = SeedDesignWork();

        await _handler.Handle(
            new RequestDesignWorkReworkCommand { Id = work.Id, Note = "Ghi chú yêu cầu" },
            CancellationToken.None);

        var revision = await _context.DesignWorks.FirstAsync(w => w.ParentDesignWorkId == work.Id);
        var logs = await _context.DesignLogs.Where(l => l.DesignWorkId == revision.Id).ToListAsync();
        logs.Should().HaveCount(2); // communication log + system log
    }

    [Test]
    public void Handle_CustomerNotFound_ThrowsForbiddenAccessException()
    {
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString()); // no matching customer

        var command = new RequestDesignWorkReworkCommand
        {
            Id = Guid.NewGuid(),
            Note = "Test"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*tài khoản khách hàng*");
    }

    [Test]
    public void Handle_DesignWorkNotFoundOrNotOwned_ThrowsDataNotFoundException()
    {
        var (work, customer) = SeedDesignWork();
        // Customer exists but design work ID doesn't match
        var command = new RequestDesignWorkReworkCommand
        {
            Id = Guid.NewGuid(), // wrong ID
            Note = "Test"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
