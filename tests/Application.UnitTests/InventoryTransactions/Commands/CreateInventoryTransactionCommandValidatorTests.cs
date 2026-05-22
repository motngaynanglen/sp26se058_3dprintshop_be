using sp26se058_3dprintshop_be.Application.InventoryTransactions.Commands;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.UnitTests.InventoryTransactions.Commands;

[TestFixture]
public class CreateInventoryTransactionCommandValidatorTests
{
    private CreateInventoryTransactionCommandValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateInventoryTransactionCommandValidator();

    private static CreateInventoryTransactionCommand ValidCommand() => new()
    {
        DesignVariantId = Guid.NewGuid(),
        Quantity = 5,
        Type = InventoryTransactionTypes.PurchaseIn
    };

    [Test]
    public async Task ShouldPass_WithValidPurchaseIn()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ShouldPass_WithAdjustmentNegative()
    {
        var cmd = ValidCommand() with { Quantity = -3, Type = InventoryTransactionTypes.Adjustment };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ShouldFail_WhenQuantityIsZero()
    {
        var cmd = ValidCommand() with { Quantity = 0 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInventoryTransactionCommand.Quantity));
    }

    [Test]
    public async Task ShouldFail_WhenPurchaseInQuantityIsNegative()
    {
        var cmd = ValidCommand() with { Quantity = -1, Type = InventoryTransactionTypes.PurchaseIn };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public async Task ShouldFail_WhenTypeIsInvalid()
    {
        var cmd = ValidCommand() with { Type = "INVALID_TYPE" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInventoryTransactionCommand.Type));
    }

    [Test]
    public async Task ShouldFail_WhenVariantIdIsEmpty()
    {
        var cmd = ValidCommand() with { DesignVariantId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public async Task ShouldFail_WhenNoteExceedsMaxLength()
    {
        var cmd = ValidCommand() with { Note = new string('A', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
