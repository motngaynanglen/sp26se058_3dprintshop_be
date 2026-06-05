using sp26se058_3dprintshop_be.Application.DesignVariants.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignVariants.Commands;

[TestFixture]
public class UpdateDesignVariantCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private UpdateDesignVariantCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _handler = new UpdateDesignVariantCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private DesignVariant SeedVariant(string code = "VAR-001", string catalogStatus = CatalogStatuses.Published)
    {
        var material = new Material { Id = Guid.NewGuid(), Name = "PLA", IsActive = true };
        _context.Materials.Add(material);

        var template = new DesignTemplate
        {
            Id = Guid.NewGuid(),
            Code = $"TPL-{Guid.NewGuid():N}",
            Name = "Template",
            FileUrl = "https://storage.test/tpl.stl"
        };
        _context.DesignTemplates.Add(template);

        var variant = new DesignVariant
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = "Original Variant",
            Price = 50_000,
            MaterialId = material.Id,
            DesignTemplateId = template.Id,
            CatalogStatus = catalogStatus,
            IsActive = catalogStatus == CatalogStatuses.Published,
            StockQuantity = 10,
            EstimatedWeightPerUnit = 100m
        };
        _context.DesignVariants.Add(variant);
        _context.SaveChanges();

        return variant;
    }

    [Test]
    public async Task Handle_ValidUpdate_UpdatesFields()
    {
        var variant = SeedVariant();
        var command = new UpdateDesignVariantCommand
        {
            Id = variant.Id,
            Name = "Updated Variant",
            Price = 75_000m,
            StockQuantity = 20
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.DesignVariants.FirstAsync(v => v.Id == variant.Id);
        updated.Name.Should().Be("Updated Variant");
        updated.Price.Should().Be(75_000m);
        updated.StockQuantity.Should().Be(20);
    }

    [Test]
    public async Task Handle_ValidUpdate_SetsLastModifiedBy()
    {
        var variant = SeedVariant();
        await _handler.Handle(
            new UpdateDesignVariantCommand { Id = variant.Id, Name = "Changed" },
            CancellationToken.None);

        var updated = await _context.DesignVariants.FirstAsync(v => v.Id == variant.Id);
        updated.LastModifiedBy.Should().Be("staff01");
    }

    [Test]
    public async Task Handle_UpdateCodeToNew_ChangesCode()
    {
        var variant = SeedVariant("VAR-001");
        var command = new UpdateDesignVariantCommand
        {
            Id = variant.Id,
            Code = "VAR-NEW"
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.DesignVariants.FirstAsync(v => v.Id == variant.Id);
        updated.Code.Should().Be("VAR-NEW");
    }

    [Test]
    public void Handle_DuplicateCode_ThrowsDuplicateException()
    {
        var variant1 = SeedVariant("VAR-001");
        var material = _context.Materials.First();
        var template = _context.DesignTemplates.First();
        _context.DesignVariants.Add(new DesignVariant
        {
            Id = Guid.NewGuid(),
            Code = "VAR-002",
            Name = "Another Variant",
            Price = 60_000,
            MaterialId = material.Id,
            DesignTemplateId = template.Id,
            CatalogStatus = CatalogStatuses.Published,
            IsActive = true
        });
        _context.SaveChanges();

        var command = new UpdateDesignVariantCommand
        {
            Id = variant1.Id,
            Code = "VAR-002" // already exists on another variant
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DuplicateException>();
    }

    [Test]
    public void Handle_MaterialNotFound_ThrowsDataNotFoundException()
    {
        var variant = SeedVariant();
        var command = new UpdateDesignVariantCommand
        {
            Id = variant.Id,
            MaterialId = Guid.NewGuid()
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Handle_VariantNotFound_ThrowsDataNotFoundException()
    {
        var command = new UpdateDesignVariantCommand
        {
            Id = Guid.NewGuid(),
            Name = "Ghost Variant"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public async Task Handle_CatalogStatusPublished_SetsIsActiveTrue()
    {
        var variant = SeedVariant(catalogStatus: CatalogStatuses.Draft);
        var command = new UpdateDesignVariantCommand
        {
            Id = variant.Id,
            CatalogStatus = CatalogStatuses.Published
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.DesignVariants.FirstAsync(v => v.Id == variant.Id);
        updated.IsActive.Should().BeTrue();
        updated.CatalogStatus.Should().Be(CatalogStatuses.Published);
    }

    [Test]
    public async Task Handle_CatalogStatusDraft_SetsIsActiveFalse()
    {
        var variant = SeedVariant(catalogStatus: CatalogStatuses.Published);
        var command = new UpdateDesignVariantCommand
        {
            Id = variant.Id,
            CatalogStatus = CatalogStatuses.Draft
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.DesignVariants.FirstAsync(v => v.Id == variant.Id);
        updated.IsActive.Should().BeFalse();
        updated.CatalogStatus.Should().Be(CatalogStatuses.Draft);
    }

    [Test]
    public async Task Handle_UpdateMaterialId_ChangesMaterial()
    {
        var variant = SeedVariant();
        var newMaterial = new Material { Id = Guid.NewGuid(), Name = "ABS", IsActive = true };
        _context.Materials.Add(newMaterial);
        _context.SaveChanges();

        var command = new UpdateDesignVariantCommand
        {
            Id = variant.Id,
            MaterialId = newMaterial.Id
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.DesignVariants.FirstAsync(v => v.Id == variant.Id);
        updated.MaterialId.Should().Be(newMaterial.Id);
    }
}
