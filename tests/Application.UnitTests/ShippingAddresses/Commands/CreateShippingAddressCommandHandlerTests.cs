using sp26se058_3dprintshop_be.Application.ShippingAddresses.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.ShippingAddresses.Commands;

[TestFixture]
public class CreateShippingAddressCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private CreateShippingAddressHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _handler = new CreateShippingAddressHandler(_context, _user.Object, _mapper);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (Account account, Customer customer) SeedCustomer()
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
        _context.SaveChanges();

        _user.Setup(u => u.Id).Returns(accountId.ToString());
        return (account, customer);
    }

    [Test]
    public async Task Handle_ValidRequest_CreatesShippingAddress()
    {
        var (_, customer) = SeedCustomer();
        var command = new CreateShippingAddressCommand
        {
            ReceiverName = "Nguyễn Văn A",
            Phone = "0901234567",
            AddressLine = "123 Đường ABC",
            Ward = "Phường 5",
            District = "Quận 1",
            City = "HCM",
            Province = "HCM"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var saved = await _context.ShippingAddresses
            .FirstOrDefaultAsync(a => a.CustomerId == customer.Id);
        saved.Should().NotBeNull();
        saved!.ReceiverName.Should().Be("Nguyễn Văn A");
        saved.Phone.Should().Be("0901234567");
    }

    [Test]
    public async Task Handle_IsDefaultTrue_NewAddressHasIsDefaultTrue()
    {
        var (_, _) = SeedCustomer();

        var command = new CreateShippingAddressCommand
        {
            ReceiverName = "New Receiver",
            Phone = "0911111111",
            AddressLine = "New Address",
            Ward = "Ward 5",
            District = "District 3",
            City = "HCM",
            Province = "HCM",
            IsDefault = true
        };

        await _handler.Handle(command, CancellationToken.None);

        var newAddress = await _context.ShippingAddresses
            .FirstAsync(a => a.ReceiverName == "New Receiver");
        newAddress.IsDefault.Should().BeTrue();
    }

    [Test]
    public void Handle_NoCustomerRecord_ThrowsForbiddenAccessException()
    {
        var accountId = Guid.NewGuid();
        _context.Accounts.Add(new Account
        {
            Id = accountId,
            Username = "nocust",
            Email = "nocust@test.com",
            PasswordHash = "hash",
            Fullname = "No Customer"
        });
        _context.SaveChanges();
        _user.Setup(u => u.Id).Returns(accountId.ToString());

        var command = new CreateShippingAddressCommand
        {
            ReceiverName = "Test",
            Phone = "0900000000",
            AddressLine = "Somewhere",
            Ward = "Ward 1",
            District = "District 1",
            City = "HCM",
            Province = "HCM"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public void Handle_AccountNotFound_ThrowsUnauthorizedAccessException()
    {
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());

        var command = new CreateShippingAddressCommand
        {
            ReceiverName = "Test",
            Phone = "0900000000",
            AddressLine = "Somewhere",
            Ward = "Ward 1",
            District = "District 1",
            City = "HCM",
            Province = "HCM"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
