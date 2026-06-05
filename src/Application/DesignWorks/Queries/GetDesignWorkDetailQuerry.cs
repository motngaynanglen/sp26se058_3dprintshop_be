using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Queries;

[Authorize(Roles = Roles.CustomerStaffManager)]
public record GetDesignWorkDetailQuerry : IRequest<DesignWorkDetailDTO>
{
    [JsonIgnore]
    public Guid Id { get; init; }
}

public class GetDesignWorkDetailQuerryHandler : IRequestHandler<GetDesignWorkDetailQuerry, DesignWorkDetailDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;
    public GetDesignWorkDetailQuerryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }
    public async Task<DesignWorkDetailDTO> Handle(GetDesignWorkDetailQuerry request, CancellationToken cancellationToken)
    {
        var query = _context.DesignWorks
            .AsNoTracking()
            .Include(dw => dw.Customer)
                .ThenInclude(c => c.Account)
            .Include(dw => dw.MainAssignedStaff)
                .ThenInclude(s => s!.Account)
            .Include(dw => dw.ServiceSelections)
                .ThenInclude(ss => ss.ServiceSelectedOptions)
            .Include(dw => dw.ChildDesignWorks)
            .AsQueryable();
        bool isStaffOrManager = _user.Role == Roles.STAFF || _user.Role == Roles.MANAGER;
        if (!isStaffOrManager)
        {
            var userId = _user.Id.ToGuid();
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.AccountId == userId, cancellationToken);

            if (customer == null)
            {
                throw new ForbiddenAccessException();
            }

            query = query.Where(dv => dv.CustomerId.Equals(customer.Id));
        }
        var designWork = await query.FirstOrDefaultAsync(dv => dv.Id.Equals(request.Id), cancellationToken);

        if (designWork == null) {
            throw new DataNotFoundException("Không tìm thấy công việc thiết kế.");
        }
        var dto = _mapper.Map<DesignWorkDetailDTO>(designWork);

        var designServiceOrder = await _context.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
                .ThenInclude(o => o.Invoice)
            .Where(oi => oi.DesignWorkId == request.Id && oi.SourceType == SourceTypes.DesignService)
            .OrderByDescending(oi => oi.Created)
            .Select(oi => new
            {
                oi.Order.Id,
                oi.Order.Code,
                oi.Order.OrderStatus,
                PaymentStatus = oi.Order.Invoice != null ? oi.Order.Invoice.PaymentStatus : null,
                TotalAmount = oi.Order.Invoice != null ? oi.Order.Invoice.TotalAmount : oi.Order.TotalPrice
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (designServiceOrder != null)
        {
            dto.DesignServiceOrderId = designServiceOrder.Id;
            dto.DesignServiceOrderCode = designServiceOrder.Code;
            dto.DesignServiceOrderStatus = designServiceOrder.OrderStatus;
            dto.DesignServicePaymentStatus = designServiceOrder.PaymentStatus;
            dto.DesignServiceTotalAmount = designServiceOrder.TotalAmount;
        }

        return dto;
    }
}
