using sp26se058_3dprintshop_be.Application.Auths.Commands.Login;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Auths.Commands;

[TestFixture]
public class LoginCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IJwtTokenGenerator> _tokenGenerator = null!;
    private Mock<IPasswordService> _passwordService = null!;
    private LoginCommandHandler _handler = null!;

    private const string HashedPassword = "hashed_secret";
    private const string PlainPassword = "secret";
    private const string FakeToken = "jwt.token.fake";

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _tokenGenerator = new Mock<IJwtTokenGenerator>();
        _passwordService = new Mock<IPasswordService>();

        _tokenGenerator.Setup(t => t.GenerateToken(It.IsAny<UserIdentity>())).Returns(FakeToken);

        _handler = new LoginCommandHandler(_context, _tokenGenerator.Object, _passwordService.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private async Task<Account> SeedAccountAsync(string username, bool hasCustomer = false, bool hasStaff = false, bool hasManager = false)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = HashedPassword,
            Fullname = "Test User",
            Email = $"{username}@test.com"
        };
        _context.Accounts.Add(account);

        if (hasCustomer) _context.Customers.Add(new Customer { AccountId = account.Id });
        if (hasStaff) _context.Staffs.Add(new Staff { AccountId = account.Id });
        if (hasManager) _context.Managers.Add(new Manager { AccountId = account.Id });

        await _context.SaveChangesAsync(CancellationToken.None);
        return account;
    }

    [Test]
    public async Task Handle_WithValidCustomerCredentials_ReturnsTokenWithCustomerRole()
    {
        await SeedAccountAsync("customer01", hasCustomer: true);
        _passwordService.Setup(p => p.VerifyPassword(PlainPassword, HashedPassword)).Returns(true);

        var command = new LoginCommand { Username = "customer01", Password = PlainPassword };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Role.Should().Be(Roles.CUSTOMER);
        result.Token.Should().Be(FakeToken);
    }

    [Test]
    public async Task Handle_WithValidStaffCredentials_ReturnsTokenWithStaffRole()
    {
        await SeedAccountAsync("staff01", hasStaff: true);
        _passwordService.Setup(p => p.VerifyPassword(PlainPassword, HashedPassword)).Returns(true);

        var command = new LoginCommand { Username = "staff01", Password = PlainPassword };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Role.Should().Be(Roles.STAFF);
    }

    [Test]
    public async Task Handle_WithValidManagerCredentials_ReturnsTokenWithManagerRole()
    {
        await SeedAccountAsync("manager01", hasManager: true);
        _passwordService.Setup(p => p.VerifyPassword(PlainPassword, HashedPassword)).Returns(true);

        var command = new LoginCommand { Username = "manager01", Password = PlainPassword };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Role.Should().Be(Roles.MANAGER);
    }

    [Test]
    public void Handle_WithWrongPassword_ThrowsUnauthorized()
    {
        SeedAccountAsync("user01", hasCustomer: true).Wait();
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), HashedPassword)).Returns(false);

        var command = new LoginCommand { Username = "user01", Password = "wrong_password" };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Tên đăng nhập hoặc mật khẩu không chính xác*");
    }

    [Test]
    public void Handle_WithNonExistentUsername_ThrowsUnauthorized()
    {
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var command = new LoginCommand { Username = "ghost_user", Password = PlainPassword };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public void Handle_WithAccountThatHasNoRole_ThrowsUnauthorized()
    {
        SeedAccountAsync("norole01").Wait();
        _passwordService.Setup(p => p.VerifyPassword(PlainPassword, HashedPassword)).Returns(true);

        var command = new LoginCommand { Username = "norole01", Password = PlainPassword };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task Handle_WithUsernameInDifferentCase_LoginSucceeds()
    {
        await SeedAccountAsync("MixedCase", hasCustomer: true);
        _passwordService.Setup(p => p.VerifyPassword(PlainPassword, HashedPassword)).Returns(true);

        var command = new LoginCommand { Username = "mixedcase", Password = PlainPassword };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
    }
}
