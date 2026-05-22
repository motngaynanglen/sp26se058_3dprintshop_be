using sp26se058_3dprintshop_be.Application.Feedbacks.Commands;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Feedbacks.Commands;

[TestFixture]
public class ReplyFeedbackCommandValidatorTests
{
    private ReplyFeedbackCommandHandler.ReplyFeedbackCommandValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new ReplyFeedbackCommandHandler.ReplyFeedbackCommandValidator();

    [Test]
    public async Task ShouldPass_WithValidReplyContent()
    {
        var command = new ReplyFeedbackCommand { Id = Guid.NewGuid(), StaffReply = "Cảm ơn bạn đã ủng hộ!" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ShouldFail_WhenStaffReplyIsEmpty()
    {
        var command = new ReplyFeedbackCommand { Id = Guid.NewGuid(), StaffReply = "" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReplyFeedbackCommand.StaffReply));
    }

    [Test]
    public async Task ShouldFail_WhenStaffReplyExceeds1000Characters()
    {
        var longReply = new string('A', 1001);
        var command = new ReplyFeedbackCommand { Id = Guid.NewGuid(), StaffReply = longReply };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ReplyFeedbackCommand.StaffReply) &&
            e.ErrorMessage.Contains("1000"));
    }

    [Test]
    public async Task ShouldPass_WhenStaffReplyIsExactly1000Characters()
    {
        var maxReply = new string('A', 1000);
        var command = new ReplyFeedbackCommand { Id = Guid.NewGuid(), StaffReply = maxReply };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
