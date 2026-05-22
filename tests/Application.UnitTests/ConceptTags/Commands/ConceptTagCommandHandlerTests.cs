using sp26se058_3dprintshop_be.Application.ConceptTags.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.ConceptTags.Commands;

[TestFixture]
public class ConceptTagCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IUser> _user = null!;
    private CreateConceptTagCommandHandler _createHandler = null!;
    private UpdateConceptTagCommandHandler _updateHandler = null!;
    private DeleteConceptTagCommandHandler _deleteHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("manager01");
        _createHandler = new CreateConceptTagCommandHandler(_context);
        _updateHandler = new UpdateConceptTagCommandHandler(_context, _user.Object);
        _deleteHandler = new DeleteConceptTagCommandHandler(_context, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private ConceptTag SeedTag(string name = "PLA Tag")
    {
        var tag = new ConceptTag
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test tag",
            IsActive = true
        };
        _context.ConceptTags.Add(tag);
        _context.SaveChanges();
        return tag;
    }

    // Create tests

    [Test]
    public async Task Create_ValidTag_ReturnsNewId()
    {
        var command = new CreateConceptTagCommand
        {
            Name = "Resin",
            Description = "Resin material tag",
            IsActive = true
        };

        var result = await _createHandler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        var tag = await _context.ConceptTags.FirstOrDefaultAsync(t => t.Name == "Resin");
        tag.Should().NotBeNull();
    }

    [Test]
    public void Create_DuplicateName_ThrowsDuplicateException()
    {
        SeedTag("ABS");
        var command = new CreateConceptTagCommand { Name = "ABS", Description = "Dup" };

        Func<Task> act = async () => await _createHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DuplicateException>();
    }

    // Update tests

    [Test]
    public async Task Update_ValidTag_UpdatesFields()
    {
        var tag = SeedTag("Old Name");
        var command = new UpdateConceptTagCommand
        {
            Id = tag.Id,
            Name = "New Name",
            IsActive = false
        };

        var result = await _updateHandler.Handle(command, CancellationToken.None);

        result.Should().Be(tag.Id);
        var updated = await _context.ConceptTags.FindAsync(tag.Id);
        updated!.Name.Should().Be("New Name");
        updated.IsActive.Should().BeFalse();
        updated.LastModifiedBy.Should().Be("manager01");
    }

    [Test]
    public void Update_TagNotFound_ThrowsDataNotFoundException()
    {
        var command = new UpdateConceptTagCommand { Id = Guid.NewGuid(), Name = "Ghost" };

        Func<Task> act = async () => await _updateHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Update_DuplicateName_ThrowsDuplicateException()
    {
        SeedTag("Tag A");
        var tagB = SeedTag("Tag B");
        var command = new UpdateConceptTagCommand { Id = tagB.Id, Name = "Tag A" };

        Func<Task> act = async () => await _updateHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DuplicateException>();
    }

    // Delete tests

    [Test]
    public async Task Delete_ExistingTag_SoftDeletesIt()
    {
        var tag = SeedTag();

        var result = await _deleteHandler.Handle(
            new DeleteConceptTagCommand { Id = tag.Id }, CancellationToken.None);

        result.Should().BeTrue();
        var deleted = await _context.ConceptTags
            .IgnoreQueryFilters()
            .FirstAsync(t => t.Id == tag.Id);
        deleted.Deleted.Should().NotBeNull();
        deleted.DeletedBy.Should().Be("manager01");
    }

    [Test]
    public void Delete_TagNotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _deleteHandler.Handle(
            new DeleteConceptTagCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
