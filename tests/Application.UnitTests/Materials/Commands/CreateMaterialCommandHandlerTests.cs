using sp26se058_3dprintshop_be.Application.Materials.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Materials.Commands;

[TestFixture]
public class CreateMaterialCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private CreateMaterialCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("manager01");
        _handler = new CreateMaterialCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    [Test]
    public async Task Handle_WithoutPriceInfo_CreatesMaterialAsInactive()
    {
        var command = new CreateMaterialCommand
        {
            Name = "PLA Test",
            Description = "Vật liệu PLA test"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsActive.Should().BeFalse();
        var saved = await _context.Materials.FirstAsync(m => m.Name == "PLA Test");
        saved.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task Handle_WithFullPriceInfo_CreatesMaterialAsActive()
    {
        var command = new CreateMaterialCommand
        {
            Name = "ABS Test",
            Description = "Vật liệu ABS test",
            BaseCostPerGram = 3.0m,
            TotalServiceCostPerGram = 6.0m,
            EffectiveDate = DateTime.Today
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsActive.Should().BeTrue();
        var saved = await _context.Materials
            .Include(m => m.PriceHistories)
            .FirstAsync(m => m.Name == "ABS Test");
        saved.IsActive.Should().BeTrue();
        saved.PriceHistories.Should().HaveCount(1);
        saved.PriceHistories.First().IsCurrent.Should().BeTrue();
        saved.PriceHistories.First().BaseCostPerGram.Should().Be(3.0m);
    }

    [Test]
    public async Task Handle_SetsCreatedByFromUser()
    {
        var command = new CreateMaterialCommand
        {
            Name = "PETG Test",
            Description = "Vật liệu PETG"
        };

        await _handler.Handle(command, CancellationToken.None);

        var saved = await _context.Materials.FirstAsync(m => m.Name == "PETG Test");
        saved.CreatedBy.Should().Be("manager01");
    }

    [Test]
    public async Task Handle_WithoutPriceInfo_DoesNotCreatePriceHistory()
    {
        var command = new CreateMaterialCommand
        {
            Name = "TPU Test",
            Description = "Vật liệu TPU"
        };

        await _handler.Handle(command, CancellationToken.None);

        var saved = await _context.Materials
            .Include(m => m.PriceHistories)
            .FirstAsync(m => m.Name == "TPU Test");
        saved.PriceHistories.Should().BeEmpty();
    }
}
