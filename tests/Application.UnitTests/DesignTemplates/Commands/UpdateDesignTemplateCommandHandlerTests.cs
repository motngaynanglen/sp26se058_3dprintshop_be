using sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignTemplates.Commands;

[TestFixture]
public class UpdateDesignTemplateCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private UpdateDesignTemplateCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _handler = new UpdateDesignTemplateCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private DesignTemplate SeedTemplate(string code = "TPL-001", string catalogStatus = CatalogStatuses.Draft)
    {
        var template = new DesignTemplate
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = "Original Template",
            FileUrl = "https://storage.test/tpl.stl",
            CatalogStatus = catalogStatus,
            IsActive = catalogStatus == CatalogStatuses.Published
        };
        _context.DesignTemplates.Add(template);
        _context.SaveChanges();
        return template;
    }

    [Test]
    public async Task Handle_ValidUpdate_UpdatesFields()
    {
        var template = SeedTemplate();
        var command = new UpdateDesignTemplateCommand
        {
            Id = template.Id,
            Name = "Updated Template",
            Description = "New description",
            FileUrl = "https://storage.test/updated.stl"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.DesignTemplates.FirstAsync(t => t.Id == template.Id);
        updated.Name.Should().Be("Updated Template");
        updated.Description.Should().Be("New description");
        updated.FileUrl.Should().Be("https://storage.test/updated.stl");
    }

    [Test]
    public async Task Handle_ValidUpdate_SetsLastModifiedBy()
    {
        var template = SeedTemplate();
        await _handler.Handle(
            new UpdateDesignTemplateCommand { Id = template.Id, Name = "Changed" },
            CancellationToken.None);

        var updated = await _context.DesignTemplates.FirstAsync(t => t.Id == template.Id);
        updated.LastModifiedBy.Should().Be("staff01");
    }

    [Test]
    public async Task Handle_UpdateCode_ChangesCode()
    {
        var template = SeedTemplate("TPL-001");
        var command = new UpdateDesignTemplateCommand
        {
            Id = template.Id,
            Code = "TPL-NEW"
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.DesignTemplates.FirstAsync(t => t.Id == template.Id);
        updated.Code.Should().Be("TPL-NEW");
    }

    [Test]
    public void Handle_DuplicateCode_ThrowsDuplicateException()
    {
        var template1 = SeedTemplate("TPL-001");
        _context.DesignTemplates.Add(new DesignTemplate
        {
            Id = Guid.NewGuid(),
            Code = "TPL-002",
            Name = "Another Template",
            FileUrl = "https://storage.test/other.stl",
            CatalogStatus = CatalogStatuses.Draft,
            IsActive = false
        });
        _context.SaveChanges();

        var command = new UpdateDesignTemplateCommand
        {
            Id = template1.Id,
            Code = "TPL-002"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DuplicateException>();
    }

    [Test]
    public void Handle_TemplateNotFound_ThrowsDataNotFoundException()
    {
        var command = new UpdateDesignTemplateCommand
        {
            Id = Guid.NewGuid(),
            Name = "Ghost Template"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public async Task Handle_CatalogStatusPublished_SetsIsActiveTrue()
    {
        var template = SeedTemplate(catalogStatus: CatalogStatuses.Draft);
        var command = new UpdateDesignTemplateCommand
        {
            Id = template.Id,
            CatalogStatus = CatalogStatuses.Published
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.DesignTemplates.FirstAsync(t => t.Id == template.Id);
        updated.IsActive.Should().BeTrue();
        updated.CatalogStatus.Should().Be(CatalogStatuses.Published);
    }

    [Test]
    public async Task Handle_CatalogStatusDraft_SetsIsActiveFalse()
    {
        var template = SeedTemplate(catalogStatus: CatalogStatuses.Published);
        var command = new UpdateDesignTemplateCommand
        {
            Id = template.Id,
            CatalogStatus = CatalogStatuses.Draft
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.DesignTemplates.FirstAsync(t => t.Id == template.Id);
        updated.IsActive.Should().BeFalse();
        updated.CatalogStatus.Should().Be(CatalogStatuses.Draft);
    }
}
