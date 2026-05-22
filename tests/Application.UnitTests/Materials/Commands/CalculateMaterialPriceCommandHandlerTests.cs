using sp26se058_3dprintshop_be.Application.Materials.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Materials.Commands;

[TestFixture]
public class CalculateMaterialPriceCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private CalculateMaterialPriceCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _handler = new CalculateMaterialPriceCommandHandler(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Material SeedMaterial(
        bool isActive = true,
        bool withCurrentPrice = true,
        decimal baseCost = 2.0m,
        decimal serviceCost = 5.0m)
    {
        var material = new Material { Name = "PLA Calc", Description = "Test", IsActive = isActive };
        _context.Materials.Add(material);

        if (withCurrentPrice)
        {
            _context.MaterialPriceHistories.Add(new MaterialPriceHistory
            {
                Material = material,
                BaseCostPerGram = baseCost,
                TotalServiceCostPerGram = serviceCost,
                EffectiveDate = DateTime.Today,
                IsCurrent = true
            });
        }

        _context.SaveChanges();
        return material;
    }

    [Test]
    public async Task Handle_ValidRequest_ReturnsCorrectCalculation()
    {
        var material = SeedMaterial(baseCost: 2.0m, serviceCost: 5.0m);
        var command = new CalculateMaterialPriceCommand
        {
            MaterialId = material.Id,
            WeightInGrams = 10.0
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.MaterialId.Should().Be(material.Id);
        result.WeightInGrams.Should().Be(10.0);
        result.TotalBaseCost.Should().Be(20.0m);  // 2.0 * 10
        result.FinalPrice.Should().Be(50.0m);     // 5.0 * 10
    }

    [Test]
    public async Task Handle_ValidRequest_ReturnsCorrectMaterialName()
    {
        var material = SeedMaterial();
        var command = new CalculateMaterialPriceCommand
        {
            MaterialId = material.Id,
            WeightInGrams = 5.0
        };

        var result = await _handler.Handle(command, CancellationToken.None);
        result.MaterialName.Should().Be("PLA Calc");
    }

    [Test]
    public void Handle_MaterialNotFound_ThrowsDataNotFoundException()
    {
        var command = new CalculateMaterialPriceCommand
        {
            MaterialId = Guid.NewGuid(),
            WeightInGrams = 10.0
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Handle_InactiveMaterial_ThrowsBusinessException()
    {
        var material = SeedMaterial(isActive: false, withCurrentPrice: true);
        var command = new CalculateMaterialPriceCommand
        {
            MaterialId = material.Id,
            WeightInGrams = 10.0
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*tạm ngưng*");
    }

    [Test]
    public void Handle_NoCurrentPrice_ThrowsBusinessException()
    {
        var material = SeedMaterial(isActive: true, withCurrentPrice: false);
        var command = new CalculateMaterialPriceCommand
        {
            MaterialId = material.Id,
            WeightInGrams = 10.0
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*chưa có giá hiện hành*");
    }
}
