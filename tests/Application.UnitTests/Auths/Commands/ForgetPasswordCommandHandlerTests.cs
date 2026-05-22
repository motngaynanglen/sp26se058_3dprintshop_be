using sp26se058_3dprintshop_be.Application.Auths.Commands.Login;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Auths.Commands;

[TestFixture]
public class ForgetPasswordCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IEmailService> _emailService = null!;
    private ForgetPasswordCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _emailService = new Mock<IEmailService>();
        _emailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _handler = new ForgetPasswordCommandHandler(_context, _emailService.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Account SeedAccount(string email = "test@example.com")
    {
        var account = new Account
        {
            Username = "user01",
            Email = email,
            PasswordHash = "hash",
            Fullname = "Test User"
        };
        _context.Accounts.Add(account);
        _context.SaveChanges();
        return account;
    }

    [Test]
    public async Task Handle_ExistingEmail_ReturnsTrue()
    {
        SeedAccount("test@example.com");

        var result = await _handler.Handle(
            new ForgetPasswordCommand { Email = "test@example.com" },
            CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    public async Task Handle_ExistingEmail_SetsResetToken()
    {
        SeedAccount("test@example.com");

        await _handler.Handle(
            new ForgetPasswordCommand { Email = "test@example.com" },
            CancellationToken.None);

        var updated = await _context.Accounts.FirstAsync(a => a.Email == "test@example.com");
        updated.PasswordResetToken.Should().NotBeNullOrEmpty();
        updated.ResetTokenExpires.Should().NotBeNull();
        updated.ResetTokenExpires!.Value.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Test]
    public async Task Handle_ExistingEmail_SendsEmail()
    {
        SeedAccount("test@example.com");

        await _handler.Handle(
            new ForgetPasswordCommand { Email = "test@example.com" },
            CancellationToken.None);

        _emailService.Verify(
            e => e.SendEmailAsync("test@example.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public void Handle_EmailNotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _handler.Handle(
            new ForgetPasswordCommand { Email = "nonexistent@example.com" },
            CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
