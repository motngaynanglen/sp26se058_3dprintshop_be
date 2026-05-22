using sp26se058_3dprintshop_be.Application.Materials.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Materials.Commands;

[TestFixture]
public class UpdateMaterialCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private UpdateMaterialCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("manager01");
        _handler = new UpdateMaterialCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private Material SeedMaterial(bool withCurrentPrice = false)
    {
        var material = new Material
        {
            Id = Guid.NewGuid(),
            Name = "PLA",
            IsActive = false
        };
        _context.Materials.Add(material);

        if (withCurrentPrice)
        {
            _context.MaterialPriceHistories.Add(new MaterialPriceHistory
            {
                Material = material,
                BaseCostPerGram = 2.0m,
                TotalServiceCostPerGram = 5.0m,
                IsCurrent = true,
                EffectiveDate = DateTime.Today.AddDays(-7)
            });
        }

        _context.SaveChanges();
        return material;
    }

    [Test]
    public async Task Handle_ValidNameUpdate_UpdatesName()
    {
        var material = SeedMaterial();
        var command = new UpdateMaterialCommand
        {
            Id = material.Id,
            Name = "PLA Premium"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var updated = await _context.Materials.FirstAsync(m => m.Id == material.Id);
        updated.Name.Should().Be("PLA Premium");
    }

    [Test]
    public async Task Handle_ValidDescriptionUpdate_UpdatesDescription()
    {
        var material = SeedMaterial();
        var command = new UpdateMaterialCommand
        {
            Id = material.Id,
            Description = "Mô tả mới"
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.Materials.FirstAsync(m => m.Id == material.Id);
        updated.Description.Should().Be("Mô tả mới");
    }

    [Test]
    public async Task Handle_ValidUpdate_SetsLastModifiedBy()
    {
        var material = SeedMaterial();
        await _handler.Handle(
            new UpdateMaterialCommand { Id = material.Id, Name = "Changed" },
            CancellationToken.None);

        var updated = await _context.Materials.FirstAsync(m => m.Id == material.Id);
        updated.LastModifiedBy.Should().Be("manager01");
    }

    [Test]
    public void Handle_DuplicateName_ThrowsDuplicateException()
    {
        SeedMaterial();
        var material2 = new Material { Id = Guid.NewGuid(), Name = "ABS", IsActive = false };
        _context.Materials.Add(material2);
        _context.SaveChanges();

        var command = new UpdateMaterialCommand
        {
            Id = material2.Id,
            Name = "PLA" // name of material1
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DuplicateException>();
    }

    [Test]
    public async Task Handle_ActivateWithCurrentPrice_Succeeds()
    {
        var material = SeedMaterial(withCurrentPrice: true);
        var command = new UpdateMaterialCommand
        {
            Id = material.Id,
            IsActive = true
        };

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _context.Materials.FirstAsync(m => m.Id == material.Id);
        updated.IsActive.Should().BeTrue();
    }

    [Test]
    public void Handle_ActivateWithoutCurrentPrice_ThrowsBusinessException()
    {
        var material = SeedMaterial(withCurrentPrice: false);
        var command = new UpdateMaterialCommand
        {
            Id = material.Id,
            IsActive = true
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*giá hiện hành*");
    }

    [Test]
    public void Handle_MaterialNotFound_ThrowsDataNotFoundException()
    {
        var command = new UpdateMaterialCommand
        {
            Id = Guid.NewGuid(),
            Name = "Ghost Material"
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
