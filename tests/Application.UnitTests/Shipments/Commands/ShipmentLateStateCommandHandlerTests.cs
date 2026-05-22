using sp26se058_3dprintshop_be.Application.Shipments.Commands;
using sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Shipments.Commands;

/// <summary>
/// Tests for the four "late-stage" shipment state transition handlers:
/// MarkAsFailed, MarkAsReturning, ConfirmReturned, MarkAsLostOrDamaged.
/// </summary>
[TestFixture]
public class ShipmentLateStateCommandHandlerTests
{
    private ApplicationDbContext _context = null!;
    private IMapper _mapper = null!;
    private Mock<IUser> _user = null!;
    private MarkShipmentAsFailedCommandHandler _failedHandler = null!;
    private MarkShipmentAsReturningCommandHandler _returningHandler = null!;
    private ConfirmShipmentReturnedCommandHandler _returnedHandler = null!;
    private MarkShipmentAsLostOrDamagedCommandHandler _lostHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContextFactory.Create();
        _mapper = TestDbContextFactory.CreateMapper();
        _user = new Mock<IUser>();
        _user.Setup(u => u.Username).Returns("staff01");
        _failedHandler = new MarkShipmentAsFailedCommandHandler(_context, _mapper, _user.Object);
        _returningHandler = new MarkShipmentAsReturningCommandHandler(_context, _mapper, _user.Object);
        _returnedHandler = new ConfirmShipmentReturnedCommandHandler(_context, _mapper, _user.Object);
        _lostHandler = new MarkShipmentAsLostOrDamagedCommandHandler(_context, _mapper, _user.Object);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    /// <summary>For MarkAsFailed which uses Include(s => s.Order).</summary>
    private Shipment SeedShipmentWithOrder(string status)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = "ORD-LATE-001",
            CustomerId = Guid.NewGuid(),
            OrderStatus = OrderStatuses.Processing
        };
        _context.Orders.Add(order);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            ShipmentStatus = status,
            RecipientName = "Test",
            RecipientPhone = "0909090909",
            AddressLine = "123 Street",
            Ward = "Ward 1",
            District = "District 1",
            City = "HCM",
            Province = "HCM"
        };
        _context.Shipments.Add(shipment);
        _context.SaveChanges();
        return shipment;
    }

    /// <summary>For handlers that do NOT use Include — no Order entity needed.</summary>
    private Shipment SeedShipment(string status)
    {
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            ShipmentStatus = status,
            RecipientName = "Test",
            RecipientPhone = "0909090909",
            AddressLine = "123 Street",
            Ward = "Ward 1",
            District = "District 1",
            City = "HCM",
            Province = "HCM"
        };
        _context.Shipments.Add(shipment);
        _context.SaveChanges();
        return shipment;
    }

    // ── MarkAsFailed ──────────────────────────────────────────────────────────

    [Test]
    public async Task MarkAsFailed_InTransitShipment_SetsStatusFailed()
    {
        var shipment = SeedShipmentWithOrder(ShipmentStatuses.InTransit);

        await _failedHandler.Handle(
            new MarkShipmentAsFailedCommand { Id = shipment.Id, Reason = "Khách không nhận" },
            CancellationToken.None);

        var updated = await _context.Shipments.FirstAsync(s => s.Id == shipment.Id);
        updated.ShipmentStatus.Should().Be(ShipmentStatuses.Failed);
        updated.LastModifiedBy.Should().Be("staff01");
    }

    [Test]
    public void MarkAsFailed_NotInTransit_ThrowsBusinessException()
    {
        var shipment = SeedShipmentWithOrder(ShipmentStatuses.ReadyForPickup);

        Func<Task> act = async () => await _failedHandler.Handle(
            new MarkShipmentAsFailedCommand { Id = shipment.Id, Reason = "Lý do" },
            CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*vận chuyển*");
    }

    [Test]
    public void MarkAsFailed_NotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _failedHandler.Handle(
            new MarkShipmentAsFailedCommand { Id = Guid.NewGuid(), Reason = "Lý do" },
            CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    // ── MarkAsReturning ───────────────────────────────────────────────────────

    [Test]
    public async Task MarkAsReturning_FromInTransit_SetsStatusReturning()
    {
        var shipment = SeedShipment(ShipmentStatuses.InTransit);

        await _returningHandler.Handle(
            new MarkShipmentAsReturningCommand { Id = shipment.Id, Reason = "Giao thất bại nhiều lần" },
            CancellationToken.None);

        var updated = await _context.Shipments.FirstAsync(s => s.Id == shipment.Id);
        updated.ShipmentStatus.Should().Be(ShipmentStatuses.Returning);
        updated.LastModifiedBy.Should().Be("staff01");
    }

    [Test]
    public async Task MarkAsReturning_FromFailed_SetsStatusReturning()
    {
        var shipment = SeedShipment(ShipmentStatuses.Failed);

        await _returningHandler.Handle(
            new MarkShipmentAsReturningCommand { Id = shipment.Id },
            CancellationToken.None);

        var updated = await _context.Shipments.FirstAsync(s => s.Id == shipment.Id);
        updated.ShipmentStatus.Should().Be(ShipmentStatuses.Returning);
    }

    [Test]
    public void MarkAsReturning_FromPreparing_ThrowsBusinessException()
    {
        var shipment = SeedShipment(ShipmentStatuses.Preparing);

        Func<Task> act = async () => await _returningHandler.Handle(
            new MarkShipmentAsReturningCommand { Id = shipment.Id },
            CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*giao*");
    }

    [Test]
    public void MarkAsReturning_NotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _returningHandler.Handle(
            new MarkShipmentAsReturningCommand { Id = Guid.NewGuid() },
            CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    // ── ConfirmReturned ───────────────────────────────────────────────────────

    [Test]
    public async Task ConfirmReturned_FromReturning_SetsStatusReturned()
    {
        var shipment = SeedShipment(ShipmentStatuses.Returning);

        await _returnedHandler.Handle(
            new ConfirmShipmentReturnedCommand { Id = shipment.Id, Note = "Đã nhận hàng" },
            CancellationToken.None);

        var updated = await _context.Shipments.FirstAsync(s => s.Id == shipment.Id);
        updated.ShipmentStatus.Should().Be(ShipmentStatuses.Returned);
        updated.LastModifiedBy.Should().Be("staff01");
    }

    [Test]
    public void ConfirmReturned_NotReturning_ThrowsBusinessException()
    {
        var shipment = SeedShipment(ShipmentStatuses.InTransit);

        Func<Task> act = async () => await _returnedHandler.Handle(
            new ConfirmShipmentReturnedCommand { Id = shipment.Id },
            CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*chuyển hoàn*");
    }

    [Test]
    public void ConfirmReturned_NotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _returnedHandler.Handle(
            new ConfirmShipmentReturnedCommand { Id = Guid.NewGuid() },
            CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }

    // ── MarkAsLostOrDamaged ───────────────────────────────────────────────────

    [Test]
    public async Task MarkAsLostOrDamaged_FromReturning_SetsStatusLostOrDamaged()
    {
        var shipment = SeedShipment(ShipmentStatuses.Returning);

        await _lostHandler.Handle(
            new MarkShipmentAsLostOrDamagedCommand { Id = shipment.Id, Reason = "Hàng bị vỡ" },
            CancellationToken.None);

        var updated = await _context.Shipments.FirstAsync(s => s.Id == shipment.Id);
        updated.ShipmentStatus.Should().Be(ShipmentStatuses.LostOrDamaged);
        updated.LastModifiedBy.Should().Be("staff01");
    }

    [Test]
    public void MarkAsLostOrDamaged_NotReturning_ThrowsBusinessException()
    {
        var shipment = SeedShipment(ShipmentStatuses.InTransit);

        Func<Task> act = async () => await _lostHandler.Handle(
            new MarkShipmentAsLostOrDamagedCommand { Id = shipment.Id, Reason = "Lý do" },
            CancellationToken.None);

        act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*chuyển hoàn*");
    }

    [Test]
    public void MarkAsLostOrDamaged_NotFound_ThrowsDataNotFoundException()
    {
        Func<Task> act = async () => await _lostHandler.Handle(
            new MarkShipmentAsLostOrDamagedCommand { Id = Guid.NewGuid(), Reason = "Lý do" },
            CancellationToken.None);

        act.Should().ThrowAsync<DataNotFoundException>();
    }
}
