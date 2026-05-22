using sp26se058_3dprintshop_be.Application.Accounts.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Accounts.Commands;

[TestFixture]
public class ChangePasswordAccountCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IPasswordService> _passwordService = null!;
    private Mock<IUser> _user = null!;
    private ChangePasswordAccountCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _passwordService = new Mock<IPasswordService>();
        _user = new Mock<IUser>();
        _handler = new ChangePasswordAccountCommandHandler(_context, _passwordService.Object, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Account SeedAccount(Guid userId)
    {
        var account = new Account
        {
            Id = userId,
            Username = "customer01",
            Email = "cust@test.com",
            PasswordHash = "hashed_old_password",
            Fullname = "Test Customer"
        };
        _context.Accounts.Add(account);
        _context.SaveChanges();
        return account;
    }

    [Test]
    public async Task Handle_CorrectOldPassword_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        SeedAccount(userId);
        _user.Setup(u => u.Id).Returns(userId.ToString());
        _passwordService.Setup(p => p.VerifyPassword("OldPass123", "hashed_old_password")).Returns(true);
        _passwordService.Setup(p => p.HashPassword("NewPass456")).Returns("hashed_new_password");

        var command = new ChangePasswordAccountCommand
        {
            OldPassword = "OldPass123",
            NewPassword = "NewPass456",
            ConfirmNewPassword = "NewPass456"
        };

        var result = await _handler.Handle(command, CancellationToken.None);
        result.Should().BeTrue();
    }

    [Test]
    public async Task Handle_CorrectOldPassword_UpdatesPasswordHash()
    {
        var userId = Guid.NewGuid();
        SeedAccount(userId);
        _user.Setup(u => u.Id).Returns(userId.ToString());
        _passwordService.Setup(p => p.VerifyPassword("OldPass123", "hashed_old_password")).Returns(true);
        _passwordService.Setup(p => p.HashPassword("NewPass456")).Returns("hashed_new_password");

        await _handler.Handle(new ChangePasswordAccountCommand
        {
            OldPassword = "OldPass123",
            NewPassword = "NewPass456",
            ConfirmNewPassword = "NewPass456"
        }, CancellationToken.None);

        var updated = await _context.Accounts.FirstAsync(a => a.Id == userId);
        updated.PasswordHash.Should().Be("hashed_new_password");
    }

    [Test]
    public void Handle_WrongOldPassword_ThrowsBusinessException()
    {
        var userId = Guid.NewGuid();
        SeedAccount(userId);
        _user.Setup(u => u.Id).Returns(userId.ToString());
        _passwordService.Setup(p => p.VerifyPassword("WrongPass", "hashed_old_password")).Returns(false);

        var command = new ChangePasswordAccountCommand
        {
            OldPassword = "WrongPass",
            NewPassword = "NewPass456",
            ConfirmNewPassword = "NewPass456"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Mật khẩu cũ không chính xác*");
    }

    [Test]
    public void Handle_UserNotFound_ThrowsDataNotFoundException()
    {
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());

        var command = new ChangePasswordAccountCommand
        {
            OldPassword = "OldPass123",
            NewPassword = "NewPass456",
            ConfirmNewPassword = "NewPass456"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
