using sp26se058_3dprintshop_be.Application.TechnicalDrafts.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.UnitTests.TechnicalDrafts.Commands;

[TestFixture]
public class DeleteTechnicalDraftCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IUser> _user = null!;
    private DeleteTechnicalDraftCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _handler = new DeleteTechnicalDraftCommandHandler(_context, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private TechnicalDraft SeedDraft(bool isConfirmed = false, bool workIsLocked = false)
    {
        var customer = new Customer { Id = Guid.NewGuid(), AccountId = Guid.NewGuid() };
        _context.Customers.Add(customer);

        var work = new DesignWork
        {
            Id = Guid.NewGuid(),
            Name = "Test Work",
            CustomerId = customer.Id,
            Status = DesignWorkStatus.InProgress,
            IsLocked = workIsLocked,
            RelationshipType = DesignRelationshipType.Original
        };
        _context.DesignWorks.Add(work);

        var uploader = new Account
        {
            Id = Guid.NewGuid(),
            Username = "uploader",
            Email = "up@test.com",
            PasswordHash = "hash",
            Fullname = "Uploader"
        };
        _context.Accounts.Add(uploader);

        var version = new DesignVersionHistory
        {
            Id = Guid.NewGuid(),
            DesignWorkId = work.Id,
            DesignWork = work,
            FileUrl = "https://storage.test/v1.stl",
            VersionNumber = 1,
            UploaderId = uploader.Id
        };
        _context.DesignVersionHistorys.Add(version);

        var material = new Material { Id = Guid.NewGuid(), Name = "PLA", IsActive = true };
        _context.Materials.Add(material);

        var draft = new TechnicalDraft
        {
            Id = Guid.NewGuid(),
            DesignVersionHistoryId = version.Id,
            DesignVersionHistory = version,
            MaterialId = material.Id,
            InfillDensity = 20,
            LayerHeight = 0.2m,
            EstimatedWeightPerUnit = 100m,
            UnitPrice = 500m,
            MarkupPercentage = 10m,
            IsConfirmed = isConfirmed
        };
        _context.TechnicalDrafts.Add(draft);
        _context.SaveChanges();

        return draft;
    }

    [Test]
    public async Task Handle_ValidDraft_SoftDeletesDraft()
    {
        var draft = SeedDraft();
        var command = new DeleteTechnicalDraftCommand(draft.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        var deleted = await _context.TechnicalDrafts
            .IgnoreQueryFilters()
            .FirstAsync(d => d.Id == draft.Id);
        deleted.Deleted.Should().NotBeNull();
        deleted.DeletedBy.Should().Be("staff01");
    }

    [Test]
    public void Handle_DraftIsConfirmed_ThrowsBusinessException()
    {
        var draft = SeedDraft(isConfirmed: true);

        Func<Task> act = async () =>
            await _handler.Handle(new DeleteTechnicalDraftCommand(draft.Id), CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được xác nhận*");
    }

    [Test]
    public void Handle_WorkIsLocked_ThrowsBusinessException()
    {
        var draft = SeedDraft(workIsLocked: true);

        Func<Task> act = async () =>
            await _handler.Handle(new DeleteTechnicalDraftCommand(draft.Id), CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã khóa*");
    }

    [Test]
    public void Handle_DraftNotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () =>
            await _handler.Handle(new DeleteTechnicalDraftCommand(Guid.NewGuid()), CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public async Task Handle_ValidDraft_SetsLastModifiedBy()
    {
        var draft = SeedDraft();

        await _handler.Handle(new DeleteTechnicalDraftCommand(draft.Id), CancellationToken.None);

        var updated = await _context.TechnicalDrafts
            .IgnoreQueryFilters()
            .FirstAsync(d => d.Id == draft.Id);
        updated.LastModifiedBy.Should().Be("staff01");
    }
}
