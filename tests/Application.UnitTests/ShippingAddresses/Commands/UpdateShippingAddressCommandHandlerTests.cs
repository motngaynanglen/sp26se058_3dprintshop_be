using sp26se058_3dprintshop_be.Application.ShippingAddresses.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.ShippingAddresses.Commands;

[TestFixture]
public class UpdateShippingAddressCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private UpdateShippingAddressCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("cust01");
        _handler = new UpdateShippingAddressCommandHandler(_context, _user.Object, _mapper);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (ShippingAddress address, Guid accountId) SeedAddress(bool isDefault = false)
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

        var customer = new Customer { Id = Guid.NewGuid(), AccountId = accountId, Account = account };
        _context.Customers.Add(customer);

        var address = new ShippingAddress
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Customer = customer,
            ReceiverName = "Old Name",
            Phone = "0900000000",
            AddressLine = "Old Address",
            Ward = "Old Ward",
            District = "Old District",
            City = "Old City",
            Province = "Old Province",
            IsDefault = isDefault
        };
        _context.ShippingAddresses.Add(address);
        _context.SaveChanges();

        _user.Setup(u => u.Id).Returns(accountId.ToString());
        return (address, accountId);
    }

    [Test]
    public async Task Handle_ValidUpdate_UpdatesFields()
    {
        var (address, _) = SeedAddress();
        var command = new UpdateShippingAddressCommand
        {
            Id = address.Id,
            ReceiverName = "New Name",
            Phone = "0911111111",
            AddressLine = "New Street",
            Ward = "New Ward",
            District = "New District",
            City = "New City",
            Province = "New Province"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.ShippingAddresses.FindAsync(address.Id);
        updated!.ReceiverName.Should().Be("New Name");
        updated.Phone.Should().Be("0911111111");
        updated.AddressLine.Should().Be("New Street");
    }

    [Test]
    public async Task Handle_ValidUpdate_SetsLastModifiedBy()
    {
        var (address, _) = SeedAddress();
        var command = new UpdateShippingAddressCommand
        {
            Id = address.Id,
            ReceiverName = "Changed"
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.ShippingAddresses.FindAsync(address.Id);
        updated!.LastModifiedBy.Should().Be("cust01");
    }

    [Test]
    public async Task Handle_SetIsDefaultTrue_SetsIsDefaultAndClearsPreviousDefault()
    {
        var (address, accountId) = SeedAddress(isDefault: false);

        // Seed a second address that's currently the default
        var customer = await _context.Customers.FirstAsync(c => c.AccountId == accountId);
        var existingDefault = new ShippingAddress
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Customer = customer,
            ReceiverName = "Old Default",
            Phone = "0000000000",
            AddressLine = "Default St",
            Ward = "W",
            District = "D",
            City = "C",
            Province = "P",
            IsDefault = true
        };
        _context.ShippingAddresses.Add(existingDefault);
        _context.SaveChanges();

        var command = new UpdateShippingAddressCommand
        {
            Id = address.Id,
            IsDefault = true,
            ReceiverName = address.ReceiverName
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.ShippingAddresses.FindAsync(address.Id);
        updated!.IsDefault.Should().BeTrue();

        var oldDefault = await _context.ShippingAddresses.FindAsync(existingDefault.Id);
        oldDefault!.IsDefault.Should().BeFalse();
    }

    [Test]
    public void Handle_DifferentUser_ThrowsForbiddenAccessException()
    {
        var (address, _) = SeedAddress();
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString()); // different user

        var command = new UpdateShippingAddressCommand
        {
            Id = address.Id,
            ReceiverName = "Hacker"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public void Handle_AddressNotFound_ThrowsDataNotFoundException()
    {
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        var command = new UpdateShippingAddressCommand
        {
            Id = Guid.NewGuid(),
            ReceiverName = "Ghost"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
