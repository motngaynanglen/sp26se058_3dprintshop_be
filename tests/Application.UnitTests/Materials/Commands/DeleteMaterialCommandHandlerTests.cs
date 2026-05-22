using sp26se058_3dprintshop_be.Application.Materials.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Materials.Commands;

[TestFixture]
public class DeleteMaterialCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IUser> _user = null!;
    private DeleteMaterialCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("manager01");
        _handler = new DeleteMaterialCommandHandler(_context, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Material SeedMaterial(bool isActive = true)
    {
        var material = new Material
        {
            Id = Guid.NewGuid(),
            Name = "PLA",
            IsActive = isActive
        };
        _context.Materials.Add(material);
        _context.SaveChanges();
        return material;
    }

    [Test]
    public async Task Handle_UnusedMaterial_SoftDeletesMaterial()
    {
        var material = SeedMaterial();
        var command = new DeleteMaterialCommand { Id = material.Id };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        var deleted = await _context.Materials
            .IgnoreQueryFilters()
            .FirstAsync(m => m.Id == material.Id);
        deleted.Deleted.Should().NotBeNull();
        deleted.DeletedBy.Should().Be("manager01");
        deleted.IsActive.Should().BeFalse();
    }

    [Test]
    public void Handle_UsedByPublishedVariant_ThrowsBusinessException()
    {
        var material = SeedMaterial();
        _context.DesignVariants.Add(new DesignVariant
        {
            Id = Guid.NewGuid(),
            Code = "VAR-DEL-001",
            Name = "Active Variant",
            Price = 50_000,
            MaterialId = material.Id,
            DesignTemplateId = Guid.NewGuid(),
            CatalogStatus = CatalogStatuses.Published,
            IsActive = true
        });
        _context.SaveChanges();

        Func<Task> act = async () => await _handler.Handle(
            new DeleteMaterialCommand { Id = material.Id }, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*biến thể*");
    }

    [Test]
    public void Handle_UsedByTechnicalDraft_ThrowsBusinessException()
    {
        var material = SeedMaterial();

        var customer = new Customer { Id = Guid.NewGuid(), AccountId = Guid.NewGuid() };
        _context.Customers.Add(customer);
        var work = new DesignWork
        {
            Id = Guid.NewGuid(),
            Name = "Work",
            CustomerId = customer.Id,
            Status = DesignWorkStatus.InProgress,
            IsLocked = false,
            RelationshipType = DesignRelationshipType.Original
        };
        _context.DesignWorks.Add(work);
        var uploader = new Account
        {
            Id = Guid.NewGuid(),
            Username = "up",
            Email = "up@test.com",
            PasswordHash = "h",
            Fullname = "Up"
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
        _context.TechnicalDrafts.Add(new TechnicalDraft
        {
            Id = Guid.NewGuid(),
            DesignVersionHistoryId = version.Id,
            MaterialId = material.Id,
            InfillDensity = 20,
            LayerHeight = 0.2m,
            EstimatedWeightPerUnit = 50m,
            UnitPrice = 250m,
            MarkupPercentage = 10m
        });
        _context.SaveChanges();

        Func<Task> act = async () => await _handler.Handle(
            new DeleteMaterialCommand { Id = material.Id }, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*bản nháp kỹ thuật*");
    }

    [Test]
    public void Handle_MaterialNotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _handler.Handle(
            new DeleteMaterialCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
