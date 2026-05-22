using sp26se058_3dprintshop_be.Application.Orders.Commands;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Orders.Commands;

[TestFixture]
public class CheckoutCommandValidatorTests
{
    private CheckoutCommandValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CheckoutCommandValidator();

    private static CheckoutCommand ValidCommand() => new()
    {
        ShippingAddressId = Guid.NewGuid(),
        SourceType = SourceTypes.InStock,
        Items = new List<CheckoutItemRequest>
        {
            new() { DesignVariantId = Guid.NewGuid(), Quantity = 1 }
        }
    };

    [Test]
    public async Task ShouldPass_WithValidInStockCheckout()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ShouldPass_WithPreOrderSourceType()
    {
        var cmd = ValidCommand() with { SourceType = SourceTypes.PreOrder };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ShouldFail_WhenShippingAddressEmpty()
    {
        var cmd = ValidCommand() with { ShippingAddressId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutCommand.ShippingAddressId));
    }

    [Test]
    public async Task ShouldFail_WhenSourceTypeInvalid()
    {
        var cmd = ValidCommand() with { SourceType = "INVALID_TYPE" };
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutCommand.SourceType));
    }

    [Test]
    public async Task ShouldFail_WhenItemsListEmpty()
    {
        var cmd = ValidCommand() with { Items = new List<CheckoutItemRequest>() };
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutCommand.Items));
    }

    [Test]
    public async Task ShouldFail_WhenItemQuantityIsZero()
    {
        var cmd = ValidCommand() with
        {
            Items = new List<CheckoutItemRequest>
            {
                new() { DesignVariantId = Guid.NewGuid(), Quantity = 0 }
            }
        };
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Quantity"));
    }

    [Test]
    public async Task ShouldFail_WhenItemQuantityIsNegative()
    {
        var cmd = ValidCommand() with
        {
            Items = new List<CheckoutItemRequest>
            {
                new() { DesignVariantId = Guid.NewGuid(), Quantity = -1 }
            }
        };
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public async Task ShouldFail_WhenItemVariantIdNull()
    {
        var cmd = ValidCommand() with
        {
            Items = new List<CheckoutItemRequest>
            {
                new() { DesignVariantId = null, Quantity = 1 }
            }
        };
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    [TestCase(SourceTypes.InStock)]
    [TestCase(SourceTypes.PreOrder)]
    public async Task ShouldPass_WithAllValidSourceTypes(string sourceType)
    {
        var cmd = ValidCommand() with { SourceType = sourceType };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Test]
    [TestCase(SourceTypes.DesignService)]
    [TestCase("UNKNOWN")]
    [TestCase("")]
    public async Task ShouldFail_WithInvalidSourceTypes(string sourceType)
    {
        var cmd = ValidCommand() with { SourceType = sourceType };
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutCommand.SourceType));
    }
}
