using sp26se058_3dprintshop_be.Application.Materials.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Materials.Commands;

[TestFixture]
public class UpdateMaterialPriceCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private UpdateMaterialPriceCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("manager01");
        _handler = new UpdateMaterialPriceCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Material SeedMaterial(bool withCurrentPrice = false, bool isActive = true)
    {
        var material = new Material
        {
            Name = "PLA",
            Description = "Nhựa PLA",
            IsActive = isActive
        };
        _context.Materials.Add(material);

        if (withCurrentPrice)
        {
            _context.MaterialPriceHistories.Add(new MaterialPriceHistory
            {
                Material = material,
                BaseCostPerGram = 2.0m,
                TotalServiceCostPerGram = 4.0m,
                EffectiveDate = DateTime.Today.AddDays(-30),
                IsCurrent = true
            });
        }

        _context.SaveChanges();
        return material;
    }

    [Test]
    public async Task Handle_ValidUpdate_AddsNewCurrentPriceHistory()
    {
        var material = SeedMaterial(withCurrentPrice: false, isActive: false);
        var command = new UpdateMaterialPriceCommand
        {
            MaterialId = material.Id,
            BaseCostPerGram = 3.0m,
            TotalServiceCostPerGram = 6.0m,
            EffectiveDate = DateTime.Today
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var histories = await _context.MaterialPriceHistories
            .Where(p => p.MaterialId == material.Id)
            .ToListAsync();
        histories.Should().HaveCount(1);
        histories.First().IsCurrent.Should().BeTrue();
        histories.First().BaseCostPerGram.Should().Be(3.0m);
    }

    [Test]
    public async Task Handle_ValidUpdate_MarksOldPriceAsNotCurrent()
    {
        var material = SeedMaterial(withCurrentPrice: true, isActive: true);
        var command = new UpdateMaterialPriceCommand
        {
            MaterialId = material.Id,
            BaseCostPerGram = 3.5m,
            TotalServiceCostPerGram = 7.0m,
            EffectiveDate = DateTime.Today
        };

        await _handler.Handle(command, CancellationToken.None);

        var histories = await _context.MaterialPriceHistories
            .Where(p => p.MaterialId == material.Id)
            .ToListAsync();
        histories.Should().HaveCount(2);
        histories.Where(p => p.IsCurrent).Should().HaveCount(1);
        histories.First(p => p.IsCurrent).BaseCostPerGram.Should().Be(3.5m);
    }

    [Test]
    public async Task Handle_ValidUpdate_ActivatesInactiveMaterial()
    {
        var material = SeedMaterial(withCurrentPrice: false, isActive: false);
        var command = new UpdateMaterialPriceCommand
        {
            MaterialId = material.Id,
            BaseCostPerGram = 2.5m,
            TotalServiceCostPerGram = 5.0m,
            EffectiveDate = DateTime.Today
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.Materials.FirstAsync(m => m.Id == material.Id);
        updated.IsActive.Should().BeTrue();
    }

    [Test]
    public void Handle_MaterialNotFound_ThrowsDataNotFoundException()
    {
        var command = new UpdateMaterialPriceCommand
        {
            MaterialId = Guid.NewGuid(),
            BaseCostPerGram = 2.5m,
            TotalServiceCostPerGram = 5.0m,
            EffectiveDate = DateTime.Today
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
