using sp26se058_3dprintshop_be.Application.DesignTags.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignTags.Commands;

[TestFixture]
public class DesignTagCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private CreateDesignTagCommandHandler _createHandler = null!;
    private UpdateDesignTagCommandHandler _updateHandler = null!;
    private DeleteDesignTagCommandHandler _deleteHandler = null!;
    private SyncDesignTagCommandHandler _syncHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _createHandler = new CreateDesignTagCommandHandler(_context);
        _updateHandler = new UpdateDesignTagCommandHandler(_context);
        _deleteHandler = new DeleteDesignTagCommandHandler(_context);
        _syncHandler = new SyncDesignTagCommandHandler(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private DesignTemplate SeedTemplate(string code = "TPL-001")
    {
        var template = new DesignTemplate
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = "Template Test",
            FileUrl = "https://example.com/file.stl"
        };
        _context.DesignTemplates.Add(template);
        _context.SaveChanges();
        return template;
    }

    private ConceptTag SeedConceptTag(string name = "Miniature", bool isActive = true)
    {
        var tag = new ConceptTag
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Tag test",
            IsActive = isActive
        };
        _context.ConceptTags.Add(tag);
        _context.SaveChanges();
        return tag;
    }

    private DesignTag SeedDesignTag(Guid templateId, Guid conceptTagId, bool isMain = false)
    {
        var dt = new DesignTag
        {
            Id = Guid.NewGuid(),
            DesignTemplateId = templateId,
            ConceptTagId = conceptTagId,
            IsMainTag = isMain,
            IsActive = true
        };
        _context.DesignTags.Add(dt);
        _context.SaveChanges();
        return dt;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Create_ValidTag_CreatesDesignTag()
    {
        var template = SeedTemplate();
        var conceptTag = SeedConceptTag();
        var command = new CreateDesignTagCommand
        {
            DesignTemplateId = template.Id,
            ConceptTagId = conceptTag.Id,
            IsMainTag = false
        };

        await _createHandler.Handle(command, CancellationToken.None);

        var created = await _context.DesignTags
            .FirstOrDefaultAsync(dt => dt.DesignTemplateId == template.Id && dt.ConceptTagId == conceptTag.Id);
        created.Should().NotBeNull();
    }

    [Test]
    public void Create_TemplateNotFound_ThrowsDataNotFoundException()
    {
        var conceptTag = SeedConceptTag();
        var command = new CreateDesignTagCommand
        {
            DesignTemplateId = Guid.NewGuid(),
            ConceptTagId = conceptTag.Id
        };

        Func<Task> act = async () => await _createHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Create_ConceptTagNotFound_ThrowsDataNotFoundException()
    {
        var template = SeedTemplate();
        var command = new CreateDesignTagCommand
        {
            DesignTemplateId = template.Id,
            ConceptTagId = Guid.NewGuid()
        };

        Func<Task> act = async () => await _createHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Create_InactiveConceptTag_ThrowsBusinessException()
    {
        var template = SeedTemplate();
        var inactiveTag = SeedConceptTag("Inactive Tag", isActive: false);
        var command = new CreateDesignTagCommand
        {
            DesignTemplateId = template.Id,
            ConceptTagId = inactiveTag.Id
        };

        Func<Task> act = async () => await _createHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*ngưng hoạt động*");
    }

    [Test]
    public void Create_DuplicateTag_ThrowsDuplicateException()
    {
        var template = SeedTemplate();
        var conceptTag = SeedConceptTag();
        SeedDesignTag(template.Id, conceptTag.Id);

        var command = new CreateDesignTagCommand
        {
            DesignTemplateId = template.Id,
            ConceptTagId = conceptTag.Id
        };

        Func<Task> act = async () => await _createHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DuplicateException>();
    }

    [Test]
    public async Task Create_IsMainTag_ClearsOldMain()
    {
        var template = SeedTemplate();
        var tag1 = SeedConceptTag("Tag1");
        var tag2 = SeedConceptTag("Tag2");
        var existingMain = SeedDesignTag(template.Id, tag1.Id, isMain: true);

        var command = new CreateDesignTagCommand
        {
            DesignTemplateId = template.Id,
            ConceptTagId = tag2.Id,
            IsMainTag = true
        };

        await _createHandler.Handle(command, CancellationToken.None);

        var oldMain = await _context.DesignTags.FindAsync(existingMain.Id);
        oldMain!.IsMainTag.Should().BeFalse();

        var newMain = await _context.DesignTags
            .FirstAsync(dt => dt.DesignTemplateId == template.Id && dt.ConceptTagId == tag2.Id);
        newMain.IsMainTag.Should().BeTrue();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Update_SetIsMainTag_ClearsOtherMains()
    {
        var template = SeedTemplate();
        var tag1 = SeedConceptTag("T1");
        var tag2 = SeedConceptTag("T2");
        var dt1 = SeedDesignTag(template.Id, tag1.Id, isMain: true);
        var dt2 = SeedDesignTag(template.Id, tag2.Id, isMain: false);

        var command = new UpdateDesignTagCommand { Id = dt2.Id, IsMainTag = true };

        await _updateHandler.Handle(command, CancellationToken.None);

        var updated = await _context.DesignTags.FindAsync(dt2.Id);
        updated!.IsMainTag.Should().BeTrue();

        var demoted = await _context.DesignTags.FindAsync(dt1.Id);
        demoted!.IsMainTag.Should().BeFalse();
    }

    [Test]
    public void Update_NotFound_ThrowsDataNotFoundException()
    {
        var command = new UpdateDesignTagCommand { Id = Guid.NewGuid(), IsMainTag = true };

        Func<Task> act = async () => await _updateHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Delete_ExistingTag_RemovesIt()
    {
        var template = SeedTemplate();
        var ct = SeedConceptTag();
        var dt = SeedDesignTag(template.Id, ct.Id);

        var result = await _deleteHandler.Handle(
            new DeleteDesignTagCommand { Id = dt.Id }, CancellationToken.None);

        result.Should().BeTrue();
        var removed = await _context.DesignTags.FindAsync(dt.Id);
        removed.Should().BeNull();
    }

    [Test]
    public async Task Delete_TagNotFound_ReturnsFalse()
    {
        var result = await _deleteHandler.Handle(
            new DeleteDesignTagCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    public async Task Delete_MainTag_PromotesNextTagToMain()
    {
        var template = SeedTemplate();
        var tag1 = SeedConceptTag("T1");
        var tag2 = SeedConceptTag("T2");
        var mainDt = SeedDesignTag(template.Id, tag1.Id, isMain: true);
        var otherDt = SeedDesignTag(template.Id, tag2.Id, isMain: false);

        await _deleteHandler.Handle(new DeleteDesignTagCommand { Id = mainDt.Id }, CancellationToken.None);

        var promoted = await _context.DesignTags.FindAsync(otherDt.Id);
        promoted!.IsMainTag.Should().BeTrue();
    }

    // ── Sync ──────────────────────────────────────────────────────────────────

    [Test]
    public async Task Sync_AddsNewTagsAndDeletesOldOnes()
    {
        var template = SeedTemplate();
        var tag1 = SeedConceptTag("T1");
        var tag2 = SeedConceptTag("T2");
        SeedDesignTag(template.Id, tag1.Id); // existing tag to be removed

        var command = new SyncDesignTagCommand
        {
            DesignTemplateId = template.Id,
            Tags = [new SyncDesignTagItem { ConceptTagId = tag2.Id, IsMainTag = true }]
        };

        await _syncHandler.Handle(command, CancellationToken.None);

        var finalTags = _context.DesignTags.Where(dt => dt.DesignTemplateId == template.Id).ToList();
        finalTags.Should().HaveCount(1);
        finalTags[0].ConceptTagId.Should().Be(tag2.Id);
    }

    [Test]
    public async Task Sync_EmptyList_RemovesAllTags()
    {
        var template = SeedTemplate();
        var tag1 = SeedConceptTag("T1");
        SeedDesignTag(template.Id, tag1.Id, isMain: true);

        var command = new SyncDesignTagCommand
        {
            DesignTemplateId = template.Id,
            Tags = []
        };

        await _syncHandler.Handle(command, CancellationToken.None);

        var remaining = _context.DesignTags.Where(dt => dt.DesignTemplateId == template.Id).ToList();
        remaining.Should().BeEmpty();
    }

    [Test]
    public void Sync_TemplateNotFound_ThrowsDataNotFoundException()
    {
        var tag = SeedConceptTag();
        var command = new SyncDesignTagCommand
        {
            DesignTemplateId = Guid.NewGuid(),
            Tags = [new SyncDesignTagItem { ConceptTagId = tag.Id, IsMainTag = true }]
        };

        Func<Task> act = async () => await _syncHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Sync_InvalidConceptTag_ThrowsBusinessException()
    {
        var template = SeedTemplate();
        var command = new SyncDesignTagCommand
        {
            DesignTemplateId = template.Id,
            Tags = [new SyncDesignTagItem { ConceptTagId = Guid.NewGuid(), IsMainTag = true }]
        };

        Func<Task> act = async () => await _syncHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*tag ý tưởng*");
    }

    [Test]
    public void Sync_DuplicateConceptTagIds_ThrowsDuplicateException()
    {
        var template = SeedTemplate();
        var tag = SeedConceptTag();
        var command = new SyncDesignTagCommand
        {
            DesignTemplateId = template.Id,
            Tags =
            [
                new SyncDesignTagItem { ConceptTagId = tag.Id, IsMainTag = true },
                new SyncDesignTagItem { ConceptTagId = tag.Id, IsMainTag = false }
            ]
        };

        Func<Task> act = async () => await _syncHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DuplicateException>();
    }

    [Test]
    public void Sync_MultipleMainTags_ThrowsBusinessException()
    {
        var template = SeedTemplate();
        var tag1 = SeedConceptTag("T1");
        var tag2 = SeedConceptTag("T2");
        var command = new SyncDesignTagCommand
        {
            DesignTemplateId = template.Id,
            Tags =
            [
                new SyncDesignTagItem { ConceptTagId = tag1.Id, IsMainTag = true },
                new SyncDesignTagItem { ConceptTagId = tag2.Id, IsMainTag = true }
            ]
        };

        Func<Task> act = async () => await _syncHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*1 tag chính*");
    }
}
