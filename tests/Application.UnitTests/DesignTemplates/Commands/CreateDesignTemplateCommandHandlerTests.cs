using sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignTemplates.Commands;

[TestFixture]
public class CreateDesignTemplateCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private CreateDesignTemplateCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _handler = new CreateDesignTemplateCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private static CreateDesignTemplateCommand ValidCommand(string code = "TPL-NEW-001") => new()
    {
        Code = code,
        Name = "Test Template",
        Description = "Mô tả mẫu test",
        FileUrl = "https://storage.test/template.stl",
        ThumbnailUrl = "https://storage.test/thumb.png"
    };

    [Test]
    public async Task Handle_ValidRequest_CreatesTemplate()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Code.Should().Be("TPL-NEW-001");
        var saved = await _context.DesignTemplates.FirstAsync(t => t.Code == "TPL-NEW-001");
        saved.Name.Should().Be("Test Template");
    }

    [Test]
    public void Handle_DuplicateCode_ThrowsDuplicateException()
    {
        _context.DesignTemplates.Add(new DesignTemplate
        {
            Code = "TPL-NEW-001",
            Name = "Existing",
            FileUrl = "https://storage.test/existing.stl"
        });
        _context.SaveChanges();

        Func<Task> act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);
        act.Should().ThrowAsync<DuplicateException>();
    }

    [Test]
    public async Task Handle_SetsCreatedByFromUser()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        var saved = await _context.DesignTemplates.FirstAsync(t => t.Code == "TPL-NEW-001");
        saved.CreatedBy.Should().Be("staff01");
    }
}
