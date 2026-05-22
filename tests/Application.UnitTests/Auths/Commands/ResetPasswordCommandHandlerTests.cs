using sp26se058_3dprintshop_be.Application.Auths.Commands.Login;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Auths.Commands;

[TestFixture]
public class ResetPasswordCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IPasswordService> _passwordService = null!;
    private ResetPasswordCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _passwordService = new Mock<IPasswordService>();
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("new_hashed_password");
        _handler = new ResetPasswordCommandHandler(_context, _passwordService.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Account SeedAccountWithResetToken(
        string username = "user01",
        string token = "valid-token",
        DateTimeOffset? expiry = null)
    {
        var account = new Account
        {
            Username = username,
            Email = $"{username}@test.com",
            PasswordHash = "old_hash",
            Fullname = "Test User",
            PasswordResetToken = token,
            ResetTokenExpires = expiry ?? DateTimeOffset.UtcNow.AddMinutes(15)
        };
        _context.Accounts.Add(account);
        _context.SaveChanges();
        return account;
    }

    [Test]
    public async Task Handle_ValidTokenNotExpired_ResetsPassword()
    {
        SeedAccountWithResetToken("user01", "valid-token");

        var result = await _handler.Handle(new ResetPasswordCommand
        {
            Username = "user01",
            Token = "valid-token",
            NewPassword = "NewPass123"
        }, CancellationToken.None);

        result.Should().BeTrue();
        var updated = await _context.Accounts.FirstAsync(a => a.Username == "user01");
        updated.PasswordHash.Should().Be("new_hashed_password");
    }

    [Test]
    public async Task Handle_ValidToken_ClearsTokenAfterReset()
    {
        SeedAccountWithResetToken("user01", "valid-token");

        await _handler.Handle(new ResetPasswordCommand
        {
            Username = "user01",
            Token = "valid-token",
            NewPassword = "NewPass123"
        }, CancellationToken.None);

        var updated = await _context.Accounts.FirstAsync(a => a.Username == "user01");
        updated.PasswordResetToken.Should().BeNull();
        updated.ResetTokenExpires.Should().BeNull();
    }

    [Test]
    public void Handle_ExpiredToken_ThrowsBusinessException()
    {
        SeedAccountWithResetToken(
            "user01",
            "expired-token",
            expiry: DateTimeOffset.UtcNow.AddMinutes(-5));

        Func<Task> act = async () => await _handler.Handle(new ResetPasswordCommand
        {
            Username = "user01",
            Token = "expired-token",
            NewPassword = "NewPass123"
        }, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*không hợp lệ hoặc đã hết hạn*");
    }

    [Test]
    public void Handle_WrongToken_ThrowsBusinessException()
    {
        SeedAccountWithResetToken("user01", "correct-token");

        Func<Task> act = async () => await _handler.Handle(new ResetPasswordCommand
        {
            Username = "user01",
            Token = "wrong-token",
            NewPassword = "NewPass123"
        }, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*không hợp lệ*");
    }

    [Test]
    public async Task Handle_CaseInsensitiveUsername_ResetsPassword()
    {
        SeedAccountWithResetToken("User01", "valid-token");

        var result = await _handler.Handle(new ResetPasswordCommand
        {
            Username = "USER01",
            Token = "valid-token",
            NewPassword = "NewPass123"
        }, CancellationToken.None);

        result.Should().BeTrue();
    }
}
