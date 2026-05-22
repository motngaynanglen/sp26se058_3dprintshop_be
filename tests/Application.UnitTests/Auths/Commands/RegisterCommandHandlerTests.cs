using sp26se058_3dprintshop_be.Application.Auths.Commands.Register;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Auths.Commands;

[TestFixture]
public class RegisterCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IPasswordService> _passwordService = null!;
    private RegisterCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _passwordService = new Mock<IPasswordService>();
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed_pw");

        _handler = new RegisterCommandHandler(_context, _passwordService.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private static RegisterCommand ValidCommand(string username = "newuser", string email = "new@test.com") => new()
    {
        Username = username,
        Password = "secure123",
        Fullname = "Nguyen Van A",
        Email = email,
        ContactPhone = "0901234567"
    };

    [Test]
    public async Task Handle_WithValidData_CreatesAccountAndCustomer()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().BeTrue();
        _context.Accounts.Should().HaveCount(1);
        _context.Customers.Should().HaveCount(1);
    }

    [Test]
    public async Task Handle_WithValidData_HashesPassword()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        var account = _context.Accounts.First();
        account.PasswordHash.Should().Be("hashed_pw");
        _passwordService.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void Handle_WithDuplicateUsername_ThrowsDuplicateException()
    {
        _context.Accounts.Add(new Account
        {
            Username = "existinguser",
            Email = "existing@test.com",
            PasswordHash = "hash",
            Fullname = "Existing"
        });
        _context.SaveChanges();

        var command = ValidCommand(username: "existinguser");

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DuplicateException>()
            .WithMessage("*đã tồn tại*");
    }

    [Test]
    public void Handle_WithDuplicateEmail_ThrowsDuplicateException()
    {
        _context.Accounts.Add(new Account
        {
            Username = "other_user",
            Email = "taken@test.com",
            PasswordHash = "hash",
            Fullname = "Another"
        });
        _context.SaveChanges();

        var command = ValidCommand(email: "taken@test.com");

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DuplicateException>();
    }

    [Test]
    public void Handle_WhenHashPasswordReturnsNull_ThrowsBusinessException()
    {
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns((string?)null!);

        Func<Task> act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*mã hóa*");
    }
}
