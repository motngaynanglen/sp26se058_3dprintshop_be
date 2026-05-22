using sp26se058_3dprintshop_be.Application.Materials.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Materials.Commands;

[TestFixture]
public class CreateMaterialCommandValidatorTests
{
    private ApplicationDbContext _context = null!;
    private CreateMaterialCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _validator = new CreateMaterialCommandValidator(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private static CreateMaterialCommand ValidCommand(bool withPriceInfo = false) => new()
    {
        Name = "PLA",
        Description = "Nhựa PLA phổ biến cho in 3D.",
        BaseCostPerGram = withPriceInfo ? 2.5m : null,
        TotalServiceCostPerGram = withPriceInfo ? 5.0m : null,
        EffectiveDate = withPriceInfo ? DateTime.Today : null
    };

    [Test]
    public async Task ShouldPass_WithNoPriceInfo()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ShouldPass_WithFullPriceInfo()
    {
        var result = await _validator.ValidateAsync(ValidCommand(withPriceInfo: true));
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ShouldFail_WhenNameIsEmpty()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMaterialCommand.Name));
    }

    [Test]
    public async Task ShouldFail_WhenNameExceedsMaxLength()
    {
        var cmd = ValidCommand() with { Name = new string('A', 101) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMaterialCommand.Name));
    }

    [Test]
    public async Task ShouldFail_WhenNameAlreadyExists()
    {
        _context.Materials.Add(new Material { Name = "PLA", IsActive = true });
        await _context.SaveChangesAsync();

        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMaterialCommand.Name));
    }

    [Test]
    public async Task ShouldFail_WhenDescriptionIsEmpty()
    {
        var cmd = ValidCommand() with { Description = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMaterialCommand.Description));
    }

    [Test]
    public async Task ShouldFail_WhenPartialPriceInfoProvided()
    {
        var cmd = ValidCommand() with { BaseCostPerGram = 2.5m }; // only base cost, missing service cost and date
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public async Task ShouldFail_WhenServiceCostLessThanBaseCost()
    {
        var cmd = ValidCommand() with
        {
            BaseCostPerGram = 5.0m,
            TotalServiceCostPerGram = 2.5m,
            EffectiveDate = DateTime.Today
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMaterialCommand.TotalServiceCostPerGram));
    }

    [Test]
    public async Task ShouldFail_WhenEffectiveDateIsInPast()
    {
        var cmd = ValidCommand() with
        {
            BaseCostPerGram = 2.5m,
            TotalServiceCostPerGram = 5.0m,
            EffectiveDate = DateTime.Today.AddDays(-1)
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMaterialCommand.EffectiveDate));
    }
}
