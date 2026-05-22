using sp26se058_3dprintshop_be.Application.Accounts.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Accounts.Commands;

[TestFixture]
public class CreateAccountCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IPasswordService> _passwordService = null!;
    private Mock<IUser> _user = null!;
    private CreateAccountCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _passwordService = new Mock<IPasswordService>();
        _user = new Mock<IUser>();

        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed_pw");

        _handler = new CreateAccountCommandHandler(_context, _passwordService.Object, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private CreateAccountCommand ValidCommand(string role = Roles.CUSTOMER) => new()
    {
        Username = "newstaff",
        Password = "Pass@123",
        Fullname = "Staff Member",
        Email = "staff@shop.com",
        ContactPhone = "0911111111",
        Role = role
    };

    [Test]
    public async Task Handle_AdminCreatingCustomer_CreatesAccountWithCustomerEntity()
    {
        _user.Setup(u => u.Role).Returns(Roles.ADMIN);

        var id = await _handler.Handle(ValidCommand(Roles.CUSTOMER), CancellationToken.None);

        id.Should().NotBeEmpty();
        _context.Customers.Should().HaveCount(1);
        _context.Staffs.Should().BeEmpty();
        _context.Managers.Should().BeEmpty();
    }

    [Test]
    public async Task Handle_AdminCreatingStaff_CreatesAccountWithStaffEntity()
    {
        _user.Setup(u => u.Role).Returns(Roles.ADMIN);

        var id = await _handler.Handle(ValidCommand(Roles.STAFF), CancellationToken.None);

        id.Should().NotBeEmpty();
        _context.Staffs.Should().HaveCount(1);
        _context.Customers.Should().BeEmpty();
    }

    [Test]
    public async Task Handle_AdminCreatingManager_CreatesAccountWithManagerEntity()
    {
        _user.Setup(u => u.Role).Returns(Roles.ADMIN);

        var id = await _handler.Handle(ValidCommand(Roles.MANAGER), CancellationToken.None);

        id.Should().NotBeEmpty();
        _context.Managers.Should().HaveCount(1);
    }

    [Test]
    public void Handle_ManagerCreatingNonStaffRole_ThrowsValidationException()
    {
        _user.Setup(u => u.Role).Returns(Roles.MANAGER);

        var command = ValidCommand(Roles.CUSTOMER);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<Application.Common.Exceptions.ValidationException>()
            .WithMessage("*Quản lý chỉ được tạo tài khoản nhân viên*");
    }

    [Test]
    public async Task Handle_ManagerCreatingStaff_Succeeds()
    {
        _user.Setup(u => u.Role).Returns(Roles.MANAGER);

        var id = await _handler.Handle(ValidCommand(Roles.STAFF), CancellationToken.None);

        id.Should().NotBeEmpty();
        _context.Staffs.Should().HaveCount(1);
    }

    [Test]
    public void Handle_WithDuplicateUsername_ThrowsValidationException()
    {
        _context.Accounts.Add(new Account
        {
            Username = "newstaff",
            Email = "other@test.com",
            PasswordHash = "hash",
            Fullname = "Existing"
        });
        _context.SaveChanges();
        _user.Setup(u => u.Role).Returns(Roles.ADMIN);

        Func<Task> act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        act.Should().ThrowAsync<Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public void Handle_WithDuplicateEmail_ThrowsValidationException()
    {
        _context.Accounts.Add(new Account
        {
            Username = "other_user",
            Email = "staff@shop.com",
            PasswordHash = "hash",
            Fullname = "Existing"
        });
        _context.SaveChanges();
        _user.Setup(u => u.Role).Returns(Roles.ADMIN);

        Func<Task> act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        act.Should().ThrowAsync<Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public void Handle_WithInvalidRole_ThrowsValidationException()
    {
        _user.Setup(u => u.Role).Returns(Roles.ADMIN);

        var command = ValidCommand("ADMIN");

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<Application.Common.Exceptions.ValidationException>()
            .WithMessage("*Vai trò không tồn tại*");
    }
}
