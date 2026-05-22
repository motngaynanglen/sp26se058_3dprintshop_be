using sp26se058_3dprintshop_be.Application.Accounts.Commands;
using sp26se058_3dprintshop_be.Application.Common.Behaviours;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private Mock<ILogger<CreateAccountCommand>> _logger = null!;
    private Mock<IUser> _user = null!;
    private Mock<IIdentityService> _identityService = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<CreateAccountCommand>>();
        _user = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
    }

    [Test]
    public async Task ShouldCallGetUserNameAsyncOnceIfAuthenticated()
    {
        _user.Setup(x => x.Id).Returns(Guid.NewGuid().ToString());

        var requestLogger = new LoggingBehaviour<CreateAccountCommand>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Process(new CreateAccountCommand
        {
            Username = "testuser",
            Password = "Pass@123",
            Fullname = "Test",
            Email = "test@test.com",
            Role = "CUSTOMER"
        }, new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ShouldNotCallGetUserNameAsyncOnceIfUnauthenticated()
    {
        var requestLogger = new LoggingBehaviour<CreateAccountCommand>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Process(new CreateAccountCommand
        {
            Username = "testuser",
            Password = "Pass@123",
            Fullname = "Test",
            Email = "test@test.com",
            Role = "CUSTOMER"
        }, new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Never);
    }
}
