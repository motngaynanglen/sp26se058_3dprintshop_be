using sp26se058_3dprintshop_be.Application.DesignTemplates.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignTemplates.Commands;

[TestFixture]
public class CreateCatalogProductCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private CreateCatalogProductCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _handler = new CreateCatalogProductCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Material SeedActiveMaterial(string name = "PLA")
    {
        var material = new Material
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = true
        };
        _context.Materials.Add(material);
        _context.SaveChanges();
        return material;
    }

    private ConceptTag SeedActiveTag(string name = "Miniature")
    {
        var tag = new ConceptTag
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Tag mô tả",
            IsActive = true
        };
        _context.ConceptTags.Add(tag);
        _context.SaveChanges();
        return tag;
    }

    private static CatalogVariantRequest BuildVariant(
        Guid materialId,
        string code = "VAR-001",
        string catalogStatus = CatalogStatuses.Draft) =>
        new()
        {
            MaterialId = materialId,
            Code = code,
            Name = "Biến thể PLA",
            Price = 150_000m,
            StockQuantity = 10,
            CatalogStatus = catalogStatus
        };

    // ── Happy paths ───────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_ValidDraftCommand_CreatesTemplateAndVariant()
    {
        var material = SeedActiveMaterial();
        var command = new CreateCatalogProductCommand
        {
            Code = "TPL-001",
            Name = "Mô hình PLA",
            FileUrl = "https://example.com/file.stl",
            CatalogStatus = CatalogStatuses.Draft,
            Variants = [BuildVariant(material.Id)]
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Code.Should().Be("TPL-001");

        var template = await _context.DesignTemplates.FirstOrDefaultAsync(t => t.Code == "TPL-001");
        template.Should().NotBeNull();
        template!.IsActive.Should().BeFalse(); // Draft → not active

        var variants = _context.DesignVariants.Where(v => v.DesignTemplateId == template.Id).ToList();
        variants.Should().HaveCount(1);
        variants[0].Code.Should().Be("VAR-001");
    }

    [Test]
    public async Task Handle_PublishedStatus_SetsIsActiveTrue()
    {
        var material = SeedActiveMaterial();
        var command = new CreateCatalogProductCommand
        {
            Code = "TPL-PUB",
            Name = "Sản phẩm đang bán",
            FileUrl = "https://example.com/file.stl",
            CatalogStatus = CatalogStatuses.Published,
            Variants = [BuildVariant(material.Id, "VAR-PUB", CatalogStatuses.Published)]
        };

        await _handler.Handle(command, CancellationToken.None);

        var template = await _context.DesignTemplates.FirstAsync(t => t.Code == "TPL-PUB");
        template.IsActive.Should().BeTrue();

        var variant = await _context.DesignVariants.FirstAsync(v => v.Code == "VAR-PUB");
        variant.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task Handle_ValidCommandWithTags_CreatesDesignTags()
    {
        var material = SeedActiveMaterial();
        var tag = SeedActiveTag();
        var command = new CreateCatalogProductCommand
        {
            Code = "TPL-TAG",
            Name = "Sản phẩm có tag",
            FileUrl = "https://example.com/file.stl",
            Variants = [BuildVariant(material.Id, "VAR-TAG")],
            Tags = [new CatalogTagRequest { ConceptTagId = tag.Id, IsMainTag = true }]
        };

        await _handler.Handle(command, CancellationToken.None);

        var template = await _context.DesignTemplates.FirstAsync(t => t.Code == "TPL-TAG");
        var designTags = _context.DesignTags.Where(dt => dt.DesignTemplateId == template.Id).ToList();
        designTags.Should().HaveCount(1);
        designTags[0].ConceptTagId.Should().Be(tag.Id);
        designTags[0].IsMainTag.Should().BeTrue();
    }

    [Test]
    public async Task Handle_SetsCreatedByFromUser()
    {
        var material = SeedActiveMaterial();
        var command = new CreateCatalogProductCommand
        {
            Code = "TPL-AUDIT",
            Name = "Kiểm tra audit",
            FileUrl = "https://example.com/file.stl",
            Variants = [BuildVariant(material.Id, "VAR-AUDIT")]
        };

        await _handler.Handle(command, CancellationToken.None);

        var template = await _context.DesignTemplates.FirstAsync(t => t.Code == "TPL-AUDIT");
        template.CreatedBy.Should().Be("staff01");
        template.LastModifiedBy.Should().Be("staff01");
    }

    // ── Duplicate guards ──────────────────────────────────────────────────────

    [Test]
    public void Handle_DuplicateTemplateCode_ThrowsDuplicateException()
    {
        var material = SeedActiveMaterial();
        _context.DesignTemplates.Add(new DesignTemplate
        {
            Id = Guid.NewGuid(),
            Code = "TPL-DUP",
            Name = "Existing",
            FileUrl = "https://example.com/existing.stl"
        });
        _context.SaveChanges();

        var command = new CreateCatalogProductCommand
        {
            Code = "TPL-DUP",
            Name = "New Product",
            FileUrl = "https://example.com/new.stl",
            Variants = [BuildVariant(material.Id)]
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DuplicateException>();
    }

    [Test]
    public void Handle_DuplicateVariantCode_ThrowsDuplicateException()
    {
        var material = SeedActiveMaterial();
        _context.DesignVariants.Add(new DesignVariant
        {
            Id = Guid.NewGuid(),
            DesignTemplateId = Guid.NewGuid(),
            MaterialId = material.Id,
            Code = "VAR-EXISTS",
            Name = "Existing Variant",
            Price = 100_000m,
            StockQuantity = 5,
            CatalogStatus = CatalogStatuses.Draft
        });
        _context.SaveChanges();

        var command = new CreateCatalogProductCommand
        {
            Code = "TPL-NEW",
            Name = "New Product",
            FileUrl = "https://example.com/new.stl",
            Variants = [BuildVariant(material.Id, "VAR-EXISTS")]
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DuplicateException>();
    }

    // ── Reference validation ──────────────────────────────────────────────────

    [Test]
    public void Handle_NonExistentMaterial_ThrowsBusinessException()
    {
        var command = new CreateCatalogProductCommand
        {
            Code = "TPL-BADMAT",
            Name = "Bad Material",
            FileUrl = "https://example.com/file.stl",
            Variants = [BuildVariant(Guid.NewGuid(), "VAR-BADMAT")] // random non-existent ID
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*vật liệu*");
    }

    [Test]
    public void Handle_InactiveMaterial_ThrowsBusinessException()
    {
        var inactive = new Material { Id = Guid.NewGuid(), Name = "Inactive Mat", IsActive = false };
        _context.Materials.Add(inactive);
        _context.SaveChanges();

        var command = new CreateCatalogProductCommand
        {
            Code = "TPL-INACT",
            Name = "Inactive Material",
            FileUrl = "https://example.com/file.stl",
            Variants = [BuildVariant(inactive.Id, "VAR-INACT")]
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*vật liệu*");
    }

    [Test]
    public void Handle_NonExistentConceptTag_ThrowsBusinessException()
    {
        var material = SeedActiveMaterial();
        var command = new CreateCatalogProductCommand
        {
            Code = "TPL-BADTAG",
            Name = "Bad Tag",
            FileUrl = "https://example.com/file.stl",
            Variants = [BuildVariant(material.Id, "VAR-BADTAG")],
            Tags = [new CatalogTagRequest { ConceptTagId = Guid.NewGuid(), IsMainTag = true }]
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*tag*");
    }
}
