using sp26se058_3dprintshop_be.Application.ShippingAddresses.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.ShippingAddresses.Commands;

[TestFixture]
public class DeleteShippingAddressCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IUser> _user = null!;
    private DeleteShippingAddressHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("cust01");
        _handler = new DeleteShippingAddressHandler(_context, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (ShippingAddress address, Guid accountId) SeedAddress()
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
            ReceiverName = "Customer",
            Phone = "0900000000",
            AddressLine = "123 Street",
            Ward = "Ward 1",
            District = "District 1",
            City = "HCM",
            Province = "HCM",
            IsDefault = true
        };
        _context.ShippingAddresses.Add(address);
        _context.SaveChanges();

        _user.Setup(u => u.Id).Returns(accountId.ToString());
        return (address, accountId);
    }

    [Test]
    public async Task Handle_OwnedAddress_SoftDeletesIt()
    {
        var (address, _) = SeedAddress();
        var command = new DeleteShippingAddressCommand { Id = address.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        var deleted = await _context.ShippingAddresses
            .IgnoreQueryFilters()
            .FirstAsync(a => a.Id == address.Id);
        deleted.Deleted.Should().NotBeNull();
        deleted.DeletedBy.Should().Be("cust01");
        deleted.IsDefault.Should().BeFalse();
    }

    [Test]
    public void Handle_AddressBelongsToOtherCustomer_ThrowsDataNotFoundException()
    {
        SeedAddress();
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString()); // different user

        var address = _context.ShippingAddresses.First();
        var command = new DeleteShippingAddressCommand { Id = address.Id };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Handle_AddressNotFound_ThrowsDataNotFoundException()
    {
        SeedAddress(); // ensure a user exists
        var command = new DeleteShippingAddressCommand { Id = Guid.NewGuid() };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
