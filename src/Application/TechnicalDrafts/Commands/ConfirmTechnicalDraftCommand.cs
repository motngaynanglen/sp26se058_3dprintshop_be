using sp26se058_3dprintshop_be.Application.TechnicalDrafts.Queries;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.TechnicalDrafts.Commands;

[Authorize(Roles = Roles.CUSTOMER)]
public record ConfirmTechnicalDraftCommand(Guid Id) : IRequest<TechnicalDraftDTO>;

public class ConfirmTechnicalDraftCommandHandler : IRequestHandler<ConfirmTechnicalDraftCommand, TechnicalDraftDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public ConfirmTechnicalDraftCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<TechnicalDraftDTO> Handle(ConfirmTechnicalDraftCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id.ToGuid();
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.AccountId == userId, cancellationToken);

        if (customer == null)
        {
            throw new ForbiddenAccessException();
        }

        var draft = await _context.TechnicalDrafts
            .Include(x => x.Material)
                .ThenInclude(m => m.PriceHistories)
            .Include(x => x.DesignVersionHistory)
                .ThenInclude(v => v.DesignWork)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.Deleted == null, cancellationToken);

        if (draft == null)
        {
            throw new DataNotFoundException(nameof(TechnicalDraft), request.Id);
        }

        var designWork = draft.DesignVersionHistory.DesignWork;
        if (designWork.CustomerId != customer.Id)
        {
            throw new ForbiddenAccessException();
        }

        var otherConfirmedDrafts = await _context.TechnicalDrafts
            .Where(x => x.Id != draft.Id
                && x.DesignVersionHistory.DesignWorkId == draft.DesignVersionHistory.DesignWorkId
                && x.IsConfirmed
                && x.Deleted == null)
            .ToListAsync(cancellationToken);

        foreach (var otherDraft in otherConfirmedDrafts)
        {
            otherDraft.IsConfirmed = false;
            otherDraft.LastModified = CoreHelper.SystemTimeNow;
            otherDraft.LastModifiedBy = _user.Username;
        }

        draft.IsConfirmed = true;
        draft.LastModified = CoreHelper.SystemTimeNow;
        draft.LastModifiedBy = _user.Username;

        draft.DesignVersionHistory.IsApproved = true;

        designWork.ResultDraftId = draft.Id;
        designWork.Status = DesignWorkStatus.Completed;
        designWork.LastModified = CoreHelper.SystemTimeNow;
        designWork.LastModifiedBy = _user.Username;

        // Hoàn thành đơn phí dịch vụ thiết kế gắn với DesignWork này: khách duyệt file 3D
        // = dịch vụ thiết kế đã giao xong. (Đơn này không có giao hàng vật lý.)
        var designServiceOrder = await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.OrderItems.Any(oi =>
                oi.DesignWorkId == designWork.Id
                && oi.SourceType == SourceTypes.DesignService))
            .OrderByDescending(o => o.Created)
            .FirstOrDefaultAsync(cancellationToken);

        if (designServiceOrder != null && designServiceOrder.OrderStatus == OrderStatuses.Processing)
        {
            designServiceOrder.OrderStatus = OrderStatuses.Completed;
            designServiceOrder.CompletedAt = CoreHelper.SystemTimeNow;
            designServiceOrder.LastModified = CoreHelper.SystemTimeNow;
            designServiceOrder.LastModifiedBy = _user.Username;

            foreach (var oi in designServiceOrder.OrderItems
                .Where(oi => oi.SourceType == SourceTypes.DesignService
                    && oi.FulfillmentStatus != OrderItemStatuses.Cancelled))
            {
                oi.FulfillmentStatus = OrderItemStatuses.Finished;
                oi.LastModified = CoreHelper.SystemTimeNow;
                oi.LastModifiedBy = _user.Username;
            }
        }

        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = draft.DesignVersionHistory.DesignWorkId,
            AccountId = userId,
            LogType = DesignLogType.StatusChange,
            Content = "Khách hàng đã duyệt báo giá kỹ thuật. Cuộc trò chuyện vẫn mở cho đến khi được khóa thủ công.",
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "SYSTEM"
        });

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<TechnicalDraftDTO>(draft);
    }
}
