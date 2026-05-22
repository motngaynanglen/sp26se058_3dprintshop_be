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
        ThumbnailUrl = "https://storage.test/thumb.png",
        CatalogStatus = CatalogStatuses.Draft
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
    public async Task Handle_DraftStatus_SetsIsActiveFalse()
    {
        var command = ValidCommand() with { CatalogStatus = CatalogStatuses.Draft };
        await _handler.Handle(command, CancellationToken.None);

        var saved = await _context.DesignTemplates.FirstAsync(t => t.Code == "TPL-NEW-001");
        saved.IsActive.Should().BeFalse();
        saved.CatalogStatus.Should().Be(CatalogStatuses.Draft);
    }

    [Test]
    public async Task Handle_PublishedStatus_SetsIsActiveTrue()
    {
        var command = ValidCommand("TPL-PUB-001") with { CatalogStatus = CatalogStatuses.Published };
        await _handler.Handle(command, CancellationToken.None);

        var saved = await _context.DesignTemplates.FirstAsync(t => t.Code == "TPL-PUB-001");
        saved.IsActive.Should().BeTrue();
    }

    [Test]
    public void Handle_DuplicateCode_ThrowsDuplicateException()
    {
        _context.DesignTemplates.Add(new DesignTemplate
        {
            Code = "TPL-NEW-001",
            Name = "Existing",
            FileUrl = "https://storage.test/existing.stl",
            CatalogStatus = CatalogStatuses.Draft,
            IsActive = false
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
