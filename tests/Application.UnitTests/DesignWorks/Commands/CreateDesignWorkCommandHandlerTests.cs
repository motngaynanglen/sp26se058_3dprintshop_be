using sp26se058_3dprintshop_be.Application.DesignWorks.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignWorks.Commands;

[TestFixture]
public class CreateDesignWorkCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private CreateDesignWorkCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("cust01");
        _handler = new CreateDesignWorkCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Customer SeedCustomer()
    {
        var accountId = Guid.NewGuid();
        _context.Accounts.Add(new Account
        {
            Id = accountId,
            Username = "cust01",
            Email = "cust01@test.com",
            PasswordHash = "hash",
            Fullname = "Customer"
        });
        var customer = new Customer { Id = Guid.NewGuid(), AccountId = accountId };
        _context.Customers.Add(customer);
        _context.SaveChanges();

        _user.Setup(u => u.Id).Returns(accountId.ToString());
        return customer;
    }

    [Test]
    public async Task Handle_ValidRequest_CreatesDesignWorkWithSketchingStatus()
    {
        SeedCustomer();
        var command = new CreateDesignWorkCommand { Name = "Thiết kế tượng rồng" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var work = await _context.DesignWorks.FirstOrDefaultAsync(w => w.Name == "Thiết kế tượng rồng");
        work.Should().NotBeNull();
        work!.Status.Should().Be(DesignWorkStatus.Sketching);
        work.RelationshipType.Should().Be(DesignRelationshipType.Original);
    }

    [Test]
    public async Task Handle_ValidRequest_CreatesInitialDesignLog()
    {
        SeedCustomer();
        var command = new CreateDesignWorkCommand { Name = "Tượng rồng" };

        await _handler.Handle(command, CancellationToken.None);

        var log = await _context.DesignLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.Content.Should().Contain("Tượng rồng");
    }

    [Test]
    public async Task Handle_WithInitialFileUrl_CreatesVersionHistory()
    {
        SeedCustomer();
        var command = new CreateDesignWorkCommand
        {
            Name = "Thiết kế có file",
            InitialFileUrl = "https://storage.test/init.stl",
            VersionTitle = "Phiên bản đầu"
        };

        await _handler.Handle(command, CancellationToken.None);

        var version = await _context.DesignVersionHistorys.FirstOrDefaultAsync();
        version.Should().NotBeNull();
        version!.FileUrl.Should().Be("https://storage.test/init.stl");
        version.Tilte.Should().Be("Phiên bản đầu");
        version.VersionNumber.Should().Be(1);
    }

    [Test]
    public async Task Handle_WithoutInitialFileUrl_DoesNotCreateVersionHistory()
    {
        SeedCustomer();
        var command = new CreateDesignWorkCommand { Name = "Không có file" };

        await _handler.Handle(command, CancellationToken.None);

        var versionCount = await _context.DesignVersionHistorys.CountAsync();
        versionCount.Should().Be(0);
    }

    [Test]
    public void Handle_NoCustomerRecord_ThrowsDataNotFoundException()
    {
        _user.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        var command = new CreateDesignWorkCommand { Name = "Test" };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Handle_NullUserId_ThrowsUnauthorizedAccessException()
    {
        _user.Setup(u => u.Id).Returns((string?)null);
        var command = new CreateDesignWorkCommand { Name = "Test" };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
