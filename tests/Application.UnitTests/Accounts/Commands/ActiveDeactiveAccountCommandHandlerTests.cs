using sp26se058_3dprintshop_be.Application.Accounts.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Accounts.Commands;

[TestFixture]
public class ActiveDeactiveAccountCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IUser> _user = null!;
    private Mock<IPasswordService> _passwordService = null!;
    private ActiveAccountCommandHandler _activeHandler = null!;
    private DeactiveAccountCommandHandler _deactiveHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("manager01");
        _user.Setup(u => u.Role).Returns(Roles.MANAGER);
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        _passwordService = new Mock<IPasswordService>();
        _activeHandler = new ActiveAccountCommandHandler(_context, _user.Object);
        _deactiveHandler = new DeactiveAccountCommandHandler(_context, _passwordService.Object, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Account SeedAccount(bool isActive = true, bool withStaff = false)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Username = $"user-{Guid.NewGuid():N}".Substring(0, 15),
            Email = $"{Guid.NewGuid():N}@test.com",
            PasswordHash = "hash",
            Fullname = "Test User",
            IsActive = isActive
        };
        _context.Accounts.Add(account);

        if (withStaff)
        {
            _context.Staffs.Add(new Staff { Id = Guid.NewGuid(), AccountId = account.Id });
        }

        _context.SaveChanges();
        return account;
    }

    // ActiveAccountCommandHandler tests

    [Test]
    public async Task Active_InactiveAccount_ActivatesIt()
    {
        var account = SeedAccount(isActive: false, withStaff: true);
        var command = new ActiveAccountCommand { Id = account.Id };

        var result = await _activeHandler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        var updated = await _context.Accounts.FirstAsync(a => a.Id == account.Id);
        updated.IsActive.Should().BeTrue();
    }

    [Test]
    public void Active_AlreadyActiveAccount_ThrowsBusinessException()
    {
        var account = SeedAccount(isActive: true, withStaff: true);
        var command = new ActiveAccountCommand { Id = account.Id };

        Func<Task> act = async () => await _activeHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đang hoạt động*");
    }

    [Test]
    public void Active_ManagerActivatingNonStaffAccount_ThrowsForbiddenAccessException()
    {
        var account = SeedAccount(isActive: false, withStaff: false);
        var command = new ActiveAccountCommand { Id = account.Id };

        Func<Task> act = async () => await _activeHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public void Active_AccountNotFound_ThrowsDataNotFoundException()
    {
        var command = new ActiveAccountCommand { Id = Guid.NewGuid() };

        Func<Task> act = async () => await _activeHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public async Task Active_SystemAdminActivatingNonStaffAccount_Succeeds()
    {
        _user.Setup(u => u.Role).Returns(Roles.SystemAdmin);
        _activeHandler = new ActiveAccountCommandHandler(_context, _user.Object);

        var account = SeedAccount(isActive: false, withStaff: false);
        var command = new ActiveAccountCommand { Id = account.Id };

        var result = await _activeHandler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
    }

    // DeactiveAccountCommandHandler tests

    [Test]
    public async Task Deactive_ActiveAccount_DeactivatesIt()
    {
        var account = SeedAccount(isActive: true, withStaff: true);
        var command = new DeactiveAccountCommand { Id = account.Id };

        var result = await _deactiveHandler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        var updated = await _context.Accounts.FirstAsync(a => a.Id == account.Id);
        updated.IsActive.Should().BeFalse();
    }

    [Test]
    public void Deactive_AlreadyInactiveAccount_ThrowsBusinessException()
    {
        var account = SeedAccount(isActive: false, withStaff: true);
        var command = new DeactiveAccountCommand { Id = account.Id };

        Func<Task> act = async () => await _deactiveHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*vô hiệu hóa*");
    }

    [Test]
    public void Deactive_ManagerDeactivatingNonStaffAccount_ThrowsForbiddenAccessException()
    {
        var account = SeedAccount(isActive: true, withStaff: false);
        var command = new DeactiveAccountCommand { Id = account.Id };

        Func<Task> act = async () => await _deactiveHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public void Deactive_AccountNotFound_ThrowsDataNotFoundException()
    {
        var command = new DeactiveAccountCommand { Id = Guid.NewGuid() };

        Func<Task> act = async () => await _deactiveHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
