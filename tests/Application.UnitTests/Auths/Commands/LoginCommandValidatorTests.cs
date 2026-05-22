using sp26se058_3dprintshop_be.Application.Auths.Commands.Login;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Auths.Commands;

[TestFixture]
public class LoginCommandValidatorTests
{
    private LoginCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new LoginCommandValidator();
    }

    [Test]
    public async Task ShouldPass_WhenBothUsernameAndPasswordProvided()
    {
        var command = new LoginCommand { Username = "user@123", Password = "secret" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ShouldFail_WhenUsernameIsEmpty()
    {
        var command = new LoginCommand { Username = "", Password = "secret" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Username));
    }

    [Test]
    public async Task ShouldFail_WhenPasswordIsEmpty()
    {
        var command = new LoginCommand { Username = "user@123", Password = "" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }

    [Test]
    public async Task ShouldFail_WhenBothFieldsEmpty()
    {
        var command = new LoginCommand { Username = "", Password = "" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
