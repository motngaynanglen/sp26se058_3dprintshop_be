using sp26se058_3dprintshop_be.Application.Auths.Commands.Register;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Auths.Commands;

[TestFixture]
public class RegisterCommandValidatorTests
{
    private RegisterCommandValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new RegisterCommandValidator();

    private static RegisterCommand ValidCommand() => new()
    {
        Username = "validuser",
        Password = "secure123",
        Fullname = "Nguyen Van A",
        Email = "valid@email.com",
        ContactPhone = "0901234567"
    };

    [Test]
    public async Task ShouldPass_WithAllValidFields()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ShouldFail_WhenUsernameTooShort()
    {
        var cmd = ValidCommand() with { Username = "ab" };
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Username));
    }

    [Test]
    public async Task ShouldFail_WhenPasswordTooShort()
    {
        var cmd = ValidCommand() with { Password = "123" };
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Test]
    public async Task ShouldFail_WhenEmailInvalidFormat()
    {
        var cmd = ValidCommand() with { Email = "not-an-email" };
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Test]
    public async Task ShouldFail_WhenFullnameEmpty()
    {
        var cmd = ValidCommand() with { Fullname = "" };
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Fullname));
    }

    [Test]
    public async Task ShouldFail_WhenContactPhoneEmpty()
    {
        var cmd = ValidCommand() with { ContactPhone = "" };
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.ContactPhone));
    }

    [Test]
    [TestCase("")]
    [TestCase(null)]
    public async Task ShouldFail_WhenUsernameNullOrEmpty(string? username)
    {
        var cmd = ValidCommand() with { Username = username! };
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
    }
}
