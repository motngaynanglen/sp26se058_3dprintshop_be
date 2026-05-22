using sp26se058_3dprintshop_be.Application.DesignVariants.Commands;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignVariants.Commands;

[TestFixture]
public class CreateDesignVariantCommandValidatorTests
{
    private CreateDesignVariantCommandValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateDesignVariantCommandValidator();

    private static CreateDesignVariantCommand ValidCommand() => new()
    {
        DesignTemplateId = Guid.NewGuid(),
        MaterialId = Guid.NewGuid(),
        Code = "VAR-001",
        Name = "Biến thể test",
        Price = 50_000,
        StockQuantity = 10,
        SizeScale = 1.0m,
        EstimatedWeightPerUnit = 150m,
        EstimatedPrintTimePerUnit = 120m,
        MarkupPercentage = 0,
        CatalogStatus = CatalogStatuses.Draft
    };

    [Test]
    public async Task ShouldPass_WithValidCommand()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ShouldFail_WhenCodeIsEmpty()
    {
        var cmd = ValidCommand() with { Code = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateDesignVariantCommand.Code));
    }

    [Test]
    public async Task ShouldFail_WhenPriceIsZero()
    {
        var cmd = ValidCommand() with { Price = 0 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateDesignVariantCommand.Price));
    }

    [Test]
    public async Task ShouldFail_WhenStockQuantityIsNegative()
    {
        var cmd = ValidCommand() with { StockQuantity = -1 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateDesignVariantCommand.StockQuantity));
    }

    [Test]
    public async Task ShouldFail_WhenCatalogStatusIsInvalid()
    {
        var cmd = ValidCommand() with { CatalogStatus = "INVALID_STATUS" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateDesignVariantCommand.CatalogStatus));
    }

    [Test]
    public async Task ShouldFail_WhenEstimatedWeightIsZero()
    {
        var cmd = ValidCommand() with { EstimatedWeightPerUnit = 0 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateDesignVariantCommand.EstimatedWeightPerUnit));
    }

    [Test]
    public async Task ShouldFail_WhenTemplateIdIsEmpty()
    {
        var cmd = ValidCommand() with { DesignTemplateId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateDesignVariantCommand.DesignTemplateId));
    }
}
