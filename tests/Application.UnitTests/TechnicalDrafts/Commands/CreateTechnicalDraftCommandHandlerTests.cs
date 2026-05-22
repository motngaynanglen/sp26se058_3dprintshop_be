using sp26se058_3dprintshop_be.Application.TechnicalDrafts.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.UnitTests.TechnicalDrafts.Commands;

[TestFixture]
public class CreateTechnicalDraftCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private CreateTechnicalDraftCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _user.Setup(u => u.Role).Returns(Roles.STAFF);
        _handler = new CreateTechnicalDraftCommandHandler(_context, _user.Object, _mapper);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private (DesignVersionHistory version, Material material) SeedData(
        string workStatus = DesignWorkStatus.InProgress,
        bool materialIsActive = true,
        bool hasMaterialPrice = true)
    {
        var customerId = Guid.NewGuid();
        _context.Customers.Add(new Customer { Id = customerId, AccountId = Guid.NewGuid() });

        var work = new DesignWork
        {
            Id = Guid.NewGuid(),
            Name = "Test Design",
            CustomerId = customerId,
            Status = workStatus,
            IsLocked = false,
            RelationshipType = DesignRelationshipType.Original
        };
        _context.DesignWorks.Add(work);

        var uploaderAccountId = Guid.NewGuid();
        var uploader = new Account
        {
            Id = uploaderAccountId,
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
            FileUrl = "https://storage.test/design.stl",
            VersionNumber = 1,
            UploaderId = uploaderAccountId,
            IsApproved = false
        };
        _context.DesignVersionHistorys.Add(version);

        var material = new Material { Name = "PLA Draft", Description = "Test", IsActive = materialIsActive };
        _context.Materials.Add(material);

        if (hasMaterialPrice)
        {
            _context.MaterialPriceHistories.Add(new MaterialPriceHistory
            {
                Material = material,
                BaseCostPerGram = 2.0m,
                TotalServiceCostPerGram = 5.0m,
                EffectiveDate = DateTime.Today,
                IsCurrent = true
            });
        }

        _context.SaveChanges();
        return (version, material);
    }

    [Test]
    public async Task Handle_ValidRequest_CreatesTechnicalDraft()
    {
        var (version, material) = SeedData();
        var command = new CreateTechnicalDraftCommand
        {
            DesignVersionHistoryId = version.Id,
            MaterialId = material.Id,
            InfillDensity = 20,
            LayerHeight = 0.2m,
            EstimatedWeightPerUnit = 150.5m,
            MarkupPercentage = 10
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        var draft = await _context.TechnicalDrafts.FirstOrDefaultAsync(
            d => d.DesignVersionHistoryId == version.Id);
        draft.Should().NotBeNull();
        draft!.InfillDensity.Should().Be(20);
    }

    [Test]
    public async Task Handle_InProgress_TransitionsWorkToReviewing()
    {
        var (version, material) = SeedData(workStatus: DesignWorkStatus.InProgress);
        var command = new CreateTechnicalDraftCommand
        {
            DesignVersionHistoryId = version.Id,
            MaterialId = material.Id,
            InfillDensity = 15,
            LayerHeight = 0.3m,
            EstimatedWeightPerUnit = 100.0m,
            MarkupPercentage = 5
        };

        await _handler.Handle(command, CancellationToken.None);

        var work = await _context.DesignWorks.FirstAsync(w => w.Id == version.DesignWorkId);
        work.Status.Should().Be(DesignWorkStatus.Reviewing);
    }

    [Test]
    public async Task Handle_ValidRequest_CalculatesUnitPriceFromMaterial()
    {
        var (version, material) = SeedData(); // serviceCost = 5.0 per gram
        var command = new CreateTechnicalDraftCommand
        {
            DesignVersionHistoryId = version.Id,
            MaterialId = material.Id,
            InfillDensity = 20,
            LayerHeight = 0.2m,
            EstimatedWeightPerUnit = 100m, // 100g * 5.0 = 500
            MarkupPercentage = 0
        };

        await _handler.Handle(command, CancellationToken.None);

        var draft = await _context.TechnicalDrafts.FirstAsync(d => d.DesignVersionHistoryId == version.Id);
        draft.UnitPrice.Should().Be(500m); // 100g * 5.0/g
    }

    [Test]
    public void Handle_VersionNotFound_ThrowsDataNotFoundException()
    {
        var (_, material) = SeedData();
        var command = new CreateTechnicalDraftCommand
        {
            DesignVersionHistoryId = Guid.NewGuid(),
            MaterialId = material.Id,
            InfillDensity = 20,
            LayerHeight = 0.2m,
            EstimatedWeightPerUnit = 100m,
            MarkupPercentage = 0
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public void Handle_MaterialNotActiveOrNoPriceHistory_ThrowsBusinessException()
    {
        var (version, material) = SeedData(hasMaterialPrice: false);
        var command = new CreateTechnicalDraftCommand
        {
            DesignVersionHistoryId = version.Id,
            MaterialId = material.Id,
            InfillDensity = 20,
            LayerHeight = 0.2m,
            EstimatedWeightPerUnit = 100m,
            MarkupPercentage = 0
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*chưa có giá hiện hành*");
    }

    [Test]
    public void Handle_DesignWorkNotInAllowedStatus_ThrowsBusinessException()
    {
        var (version, material) = SeedData(workStatus: DesignWorkStatus.Sketching);
        var command = new CreateTechnicalDraftCommand
        {
            DesignVersionHistoryId = version.Id,
            MaterialId = material.Id,
            InfillDensity = 20,
            LayerHeight = 0.2m,
            EstimatedWeightPerUnit = 100m,
            MarkupPercentage = 0
        };

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        act.Should().ThrowAsync<BusinessException>();
    }
}
