using sp26se058_3dprintshop_be.Application.Materials.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Materials.Commands;

[TestFixture]
public class ChangeMaterialActiveStatusCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IUser> _user = null!;
    private MaterialActiveStatusService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("manager01");
        _service = new MaterialActiveStatusService(_context, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Material SeedMaterial(bool isActive = true, bool withCurrentPrice = false)
    {
        var material = new Material { Name = "PLA", Description = "Test", IsActive = isActive };
        _context.Materials.Add(material);

        if (withCurrentPrice)
        {
            _context.MaterialPriceHistories.Add(new MaterialPriceHistory
            {
                Material = material,
                BaseCostPerGram = 2.0m,
                TotalServiceCostPerGram = 4.0m,
                EffectiveDate = DateTime.Today,
                IsCurrent = true
            });
        }

        _context.SaveChanges();
        return material;
    }

    [Test]
    public async Task SetActive_True_WithCurrentPrice_ActivatesMaterial()
    {
        var material = SeedMaterial(isActive: false, withCurrentPrice: true);

        var result = await _service.SetActiveAsync(material.Id, true, CancellationToken.None);

        result.IsActive.Should().BeTrue();
        var updated = await _context.Materials.FirstAsync(m => m.Id == material.Id);
        updated.IsActive.Should().BeTrue();
    }

    [Test]
    public void SetActive_True_WithoutCurrentPrice_ThrowsBusinessException()
    {
        var material = SeedMaterial(isActive: false, withCurrentPrice: false);

        Func<Task> act = async () => await _service.SetActiveAsync(material.Id, true, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*chưa có giá hiện hành*");
    }

    [Test]
    public async Task SetActive_False_WhenNotUsedByPublishedVariant_DeactivatesMaterial()
    {
        var material = SeedMaterial(isActive: true, withCurrentPrice: true);

        var result = await _service.SetActiveAsync(material.Id, false, CancellationToken.None);

        result.IsActive.Should().BeFalse();
        var updated = await _context.Materials.FirstAsync(m => m.Id == material.Id);
        updated.IsActive.Should().BeFalse();
    }

    [Test]
    public void SetActive_False_WhenUsedByPublishedVariant_ThrowsBusinessException()
    {
        var material = SeedMaterial(isActive: true, withCurrentPrice: true);

        _context.DesignVariants.Add(new DesignVariant
        {
            Code = "VAR-001",
            Name = "Test Variant",
            Price = 100_000,
            MaterialId = material.Id,
            DesignTemplateId = Guid.NewGuid(),
            CatalogStatus = CatalogStatuses.Published,
            IsActive = true
        });
        _context.SaveChanges();

        Func<Task> act = async () => await _service.SetActiveAsync(material.Id, false, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đang được dùng bởi biến thể*");
    }

    [Test]
    public void SetActive_MaterialNotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _service.SetActiveAsync(Guid.NewGuid(), true, CancellationToken.None);
        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public async Task SetActive_True_UpdatesLastModifiedBy()
    {
        var material = SeedMaterial(isActive: false, withCurrentPrice: true);

        await _service.SetActiveAsync(material.Id, true, CancellationToken.None);

        var updated = await _context.Materials.FirstAsync(m => m.Id == material.Id);
        updated.LastModifiedBy.Should().Be("manager01");
    }
}
