using sp26se058_3dprintshop_be.Application.Accounts.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Accounts.Commands;

[TestFixture]
public class UpdateAccountCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IPasswordService> _passwordService = null!;
    private Mock<IUser> _user = null!;
    private UpdateAccountCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _passwordService = new Mock<IPasswordService>();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("admin01");
        _user.Setup(u => u.Role).Returns(Roles.SystemAdmin);
        _handler = new UpdateAccountCommandHandler(_context, _passwordService.Object, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Account SeedAccount(string username = "user01", string email = "user01@test.com", bool isStaff = false)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = "hash",
            Fullname = "Test User"
        };
        _context.Accounts.Add(account);

        if (isStaff)
        {
            _context.Staffs.Add(new Staff { Id = Guid.NewGuid(), AccountId = account.Id });
        }

        _context.SaveChanges();
        return account;
    }

    [Test]
    public async Task Handle_AdminUpdatesAnyAccount_Succeeds()
    {
        var account = SeedAccount();
        var command = new UpdateAccountCommand { Id = account.Id, Fullname = "Updated Name" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(account.Id);
        var updated = await _context.Accounts.FirstAsync(a => a.Id == account.Id);
        updated.Fullname.Should().Be("Updated Name");
    }

    [Test]
    public async Task Handle_ManagerUpdatesStaffAccount_Succeeds()
    {
        _user.Setup(u => u.Role).Returns(Roles.MANAGER);
        var account = SeedAccount(isStaff: true);
        var command = new UpdateAccountCommand { Id = account.Id, Fullname = "New Staff Name" };

        var result = await _handler.Handle(command, CancellationToken.None);
        result.Should().Be(account.Id);
    }

    [Test]
    public void Handle_ManagerUpdatesNonStaffAccount_ThrowsForbiddenAccessException()
    {
        _user.Setup(u => u.Role).Returns(Roles.MANAGER);
        var account = SeedAccount(isStaff: false);
        var command = new UpdateAccountCommand { Id = account.Id, Fullname = "Attempt Update" };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public void Handle_DuplicateEmail_ThrowsDuplicateException()
    {
        var account1 = SeedAccount("user01", "user01@test.com");
        var account2 = SeedAccount("user02", "user02@test.com");
        var command = new UpdateAccountCommand
        {
            Id = account2.Id,
            Email = "user01@test.com"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<DuplicateException>();
    }

    [Test]
    public void Handle_AccountNotFound_ThrowsDataNotFoundException()
    {
        var command = new UpdateAccountCommand { Id = Guid.NewGuid(), Fullname = "No Account" };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
