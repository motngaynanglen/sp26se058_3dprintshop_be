using sp26se058_3dprintshop_be.Application.DesignVariants.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.DesignVariants.Commands;

[TestFixture]
public class CreateDesignVariantCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private CreateDesignVariantCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _handler = new CreateDesignVariantCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (Guid templateId, Guid materialId) SeedDependencies()
    {
        var template = new DesignTemplate
        {
            Id = Guid.NewGuid(),
            Code = "TPL-001",
            Name = "Test Template",
            FileUrl = "https://storage.test/template.stl",
            CatalogStatus = CatalogStatuses.Published,
            IsActive = true
        };
        var material = new Material { Name = "PLA", Description = "Test", IsActive = true };
        _context.DesignTemplates.Add(template);
        _context.Materials.Add(material);
        _context.SaveChanges();
        return (template.Id, material.Id);
    }

    private CreateDesignVariantCommand ValidCommand(Guid templateId, Guid materialId) => new()
    {
        DesignTemplateId = templateId,
        MaterialId = materialId,
        Code = "VAR-TEST-001",
        Name = "Test Variant",
        Price = 50_000,
        StockQuantity = 10,
        SizeScale = 1.0m,
        EstimatedWeightPerUnit = 150m,
        EstimatedPrintTimePerUnit = 120m,
        MarkupPercentage = 0,
        CatalogStatus = CatalogStatuses.Draft
    };

    [Test]
    public async Task Handle_ValidRequest_CreatesDesignVariant()
    {
        var (templateId, materialId) = SeedDependencies();
        var command = ValidCommand(templateId, materialId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Code.Should().Be("VAR-TEST-001");
        var saved = await _context.DesignVariants.FirstAsync(v => v.Code == "VAR-TEST-001");
        saved.Price.Should().Be(50_000);
    }

    [Test]
    public async Task Handle_DraftStatus_SetsIsActiveFalse()
    {
        var (templateId, materialId) = SeedDependencies();
        var command = ValidCommand(templateId, materialId) with { CatalogStatus = CatalogStatuses.Draft };

        await _handler.Handle(command, CancellationToken.None);

        var saved = await _context.DesignVariants.FirstAsync(v => v.Code == "VAR-TEST-001");
        saved.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task Handle_PublishedStatus_SetsIsActiveTrue()
    {
        var (templateId, materialId) = SeedDependencies();
        var command = ValidCommand(templateId, materialId) with
        {
            Code = "VAR-TEST-002",
            CatalogStatus = CatalogStatuses.Published
        };

        await _handler.Handle(command, CancellationToken.None);

        var saved = await _context.DesignVariants.FirstAsync(v => v.Code == "VAR-TEST-002");
        saved.IsActive.Should().BeTrue();
    }

    [Test]
    public void Handle_TemplateNotFound_ThrowsDataNotFoundException()
    {
        var (_, materialId) = SeedDependencies();
        var command = ValidCommand(Guid.NewGuid(), materialId);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Handle_MaterialNotFound_ThrowsDataNotFoundException()
    {
        var (templateId, _) = SeedDependencies();
        var command = ValidCommand(templateId, Guid.NewGuid());

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Handle_DuplicateCode_ThrowsDuplicateException()
    {
        var (templateId, materialId) = SeedDependencies();
        var command = ValidCommand(templateId, materialId);

        // Seed existing variant with same code
        _context.DesignVariants.Add(new DesignVariant
        {
            Code = "VAR-TEST-001",
            Name = "Existing",
            Price = 10_000,
            MaterialId = materialId,
            DesignTemplateId = templateId,
            CatalogStatus = CatalogStatuses.Draft
        });
        _context.SaveChanges();

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<DuplicateException>();
    }

    [Test]
    public async Task Handle_SetsCreatedByFromUser()
    {
        var (templateId, materialId) = SeedDependencies();
        await _handler.Handle(ValidCommand(templateId, materialId), CancellationToken.None);

        var saved = await _context.DesignVariants.FirstAsync(v => v.Code == "VAR-TEST-001");
        saved.CreatedBy.Should().Be("staff01");
    }
}
