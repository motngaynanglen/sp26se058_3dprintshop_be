using sp26se058_3dprintshop_be.Application.ServiceOptions.Commands;
using sp26se058_3dprintshop_be.Application.ServiceOptions.Queries;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

namespace sp26se058_3dprintshop_be.Application.UnitTests.ServiceOptions.Commands;

[TestFixture]
public class ServiceOptionCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private CreateServiceOptionCommandHandler _createHandler = null!;
    private UpdateServiceOptionCommandHandler _updateHandler = null!;
    private DeleteServiceOptionCommandHandler _deleteHandler = null!;
    private ActiveServiceOptionCommandHandler _activeHandler = null!;
    private DeactiveServiceOptionCommandHandler _deactiveHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("manager01");
        _createHandler = new CreateServiceOptionCommandHandler(_context, _user.Object, _mapper);
        _updateHandler = new UpdateServiceOptionCommandHandler(_context, _user.Object);
        _deleteHandler = new DeleteServiceOptionCommandHandler(_context, _user.Object);
        _activeHandler = new ActiveServiceOptionCommandHandler(_context, _user.Object);
        _deactiveHandler = new DeactiveServiceOptionCommandHandler(_context, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private ServiceOption SeedOption(string code = "CODE-A", bool isActive = true)
    {
        var option = new ServiceOption
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = "Thiết kế cơ bản",
            GroupCode = "DESIGN",
            GroupName = "Thiết kế",
            SelectionType = "ADDON",
            DefaultPrice = 100_000m,
            MinQuantity = 1,
            IsActive = isActive
        };
        _context.ServiceOptions.Add(option);
        _context.SaveChanges();
        return option;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Create_ValidOption_CreatesWithIsActiveTrue()
    {
        var command = new CreateServiceOptionCommand
        {
            Code = "OPT-001",
            Name = "Thiết kế quy chuẩn",
            GroupCode = "DESIGN",
            GroupName = "Thiết kế",
            SelectionType = "ADDON",
            DefaultPrice = 200_000m,
            MinQuantity = 1
        };

        await _createHandler.Handle(command, CancellationToken.None);

        var created = await _context.ServiceOptions.FirstOrDefaultAsync(s => s.Code == "OPT-001");
        created.Should().NotBeNull();
        created!.IsActive.Should().BeTrue();
        created.Name.Should().Be("Thiết kế quy chuẩn");
    }

    [Test]
    public async Task Create_SetsCreatedByFromUser()
    {
        var command = new CreateServiceOptionCommand
        {
            Code = "OPT-AUDIT",
            Name = "Option Audit",
            GroupCode = "DESIGN",
            GroupName = "Thiết kế",
            SelectionType = "ADDON",
            DefaultPrice = 50_000m,
            MinQuantity = 1
        };

        await _createHandler.Handle(command, CancellationToken.None);

        var created = await _context.ServiceOptions.FirstAsync(s => s.Code == "OPT-AUDIT");
        created.CreatedBy.Should().Be("manager01");
    }

    [Test]
    public void Create_DuplicateCode_ThrowsDuplicateException()
    {
        SeedOption("DUP-CODE");
        var command = new CreateServiceOptionCommand
        {
            Code = "DUP-CODE",
            Name = "Trùng mã",
            GroupCode = "DESIGN",
            GroupName = "Thiết kế",
            SelectionType = "ADDON",
            DefaultPrice = 100_000m,
            MinQuantity = 1
        };

        Func<Task> act = async () => await _createHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DuplicateException>();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Update_ValidNameChange_UpdatesName()
    {
        var option = SeedOption();
        var command = new UpdateServiceOptionCommand { Id = option.Id, Name = "Tên mới" };

        await _updateHandler.Handle(command, CancellationToken.None);

        var updated = await _context.ServiceOptions.FindAsync(option.Id);
        updated!.Name.Should().Be("Tên mới");
    }

    [Test]
    public async Task Update_ValidCodeChange_NormalizesUpperCase()
    {
        var option = SeedOption("OLD-CODE");
        var command = new UpdateServiceOptionCommand { Id = option.Id, Code = "new-code" };

        await _updateHandler.Handle(command, CancellationToken.None);

        var updated = await _context.ServiceOptions.IgnoreQueryFilters().FirstAsync(s => s.Id == option.Id);
        updated.Code.Should().Be("NEW-CODE");
    }

    [Test]
    public async Task Update_SetsLastModifiedBy()
    {
        var option = SeedOption();
        var command = new UpdateServiceOptionCommand { Id = option.Id, Name = "Changed" };

        await _updateHandler.Handle(command, CancellationToken.None);

        var updated = await _context.ServiceOptions.FindAsync(option.Id);
        updated!.LastModifiedBy.Should().Be("manager01");
    }

    [Test]
    public void Update_DuplicateCode_ThrowsDuplicateException()
    {
        SeedOption("TAKEN");
        var option2 = SeedOption("OTHER");
        var command = new UpdateServiceOptionCommand { Id = option2.Id, Code = "TAKEN" };

        Func<Task> act = async () => await _updateHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DuplicateException>();
    }

    [Test]
    public void Update_RevisionGroupWithoutAdjustmentRound_ThrowsBusinessException()
    {
        var option = SeedOption();
        // Set group to REVISION but provide no AdjustmentRoundDelta
        var command = new UpdateServiceOptionCommand
        {
            Id = option.Id,
            GroupCode = "REVISION",
            ClearAdjustmentRoundDelta = true
        };

        Func<Task> act = async () => await _updateHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*REVISION*");
    }

    [Test]
    public void Update_NotFound_ThrowsDataNotFoundException()
    {
        var command = new UpdateServiceOptionCommand { Id = Guid.NewGuid(), Name = "Ghost" };

        Func<Task> act = async () => await _updateHandler.Handle(command, CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    // ── Delete (deactivate) ───────────────────────────────────────────────────

    [Test]
    public async Task Delete_ExistingOption_SetsIsActiveFalse()
    {
        var option = SeedOption(isActive: true);

        await _deleteHandler.Handle(new DeleteServiceOptionCommand(option.Id), CancellationToken.None);

        var updated = await _context.ServiceOptions.IgnoreQueryFilters().FirstAsync(s => s.Id == option.Id);
        updated.IsActive.Should().BeFalse();
    }

    [Test]
    public void Delete_NotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () =>
            await _deleteHandler.Handle(new DeleteServiceOptionCommand(Guid.NewGuid()), CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    // ── Active / Deactive ─────────────────────────────────────────────────────

    [Test]
    public async Task Active_ExistingOption_SetsIsActiveTrue()
    {
        var option = SeedOption(isActive: false);

        await _activeHandler.Handle(new ActiveServiceOptionCommand(option.Id), CancellationToken.None);

        var updated = await _context.ServiceOptions.IgnoreQueryFilters().FirstAsync(s => s.Id == option.Id);
        updated.IsActive.Should().BeTrue();
    }

    [Test]
    public void Active_NotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () =>
            await _activeHandler.Handle(new ActiveServiceOptionCommand(Guid.NewGuid()), CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    [Test]
    public async Task Deactive_ExistingOption_SetsIsActiveFalse()
    {
        var option = SeedOption(isActive: true);

        await _deactiveHandler.Handle(new DeactiveServiceOptionCommand(option.Id), CancellationToken.None);

        var updated = await _context.ServiceOptions.IgnoreQueryFilters().FirstAsync(s => s.Id == option.Id);
        updated.IsActive.Should().BeFalse();
        updated.LastModifiedBy.Should().Be("manager01");
    }

    [Test]
    public void Deactive_NotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () =>
            await _deactiveHandler.Handle(new DeactiveServiceOptionCommand(Guid.NewGuid()), CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
