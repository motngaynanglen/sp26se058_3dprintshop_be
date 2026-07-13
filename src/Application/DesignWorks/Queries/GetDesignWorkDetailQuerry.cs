using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
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
            .Include(dw => dw.ServiceSelections)
                .ThenInclude(ss => ss.ServiceSelectedOptions)
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

        if (designWork == null)
        {
            throw new DataNotFoundException("Không tìm thấy công việc thiết kế.");
        }

        var dto = _mapper.Map<DesignWorkDetailDTO>(designWork);

        // Lấy hóa đơn đi kèm: đơn phí dịch vụ thiết kế gần nhất gắn với DesignWork này.
        var serviceOrder = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Invoice)
                .ThenInclude(i => i!.Transactions)
            .Where(o => o.OrderItems.Any(oi =>
                oi.DesignWorkId == designWork.Id
                && oi.SourceType == SourceTypes.DesignService))
            .OrderByDescending(o => o.Created)
            .FirstOrDefaultAsync(cancellationToken);

        if (serviceOrder != null)
        {
            dto.DesignServiceOrderId = serviceOrder.Id;
            dto.DesignServiceOrderCode = serviceOrder.Code;
            dto.DesignServiceOrderStatus = serviceOrder.OrderStatus;

            if (serviceOrder.Invoice != null)
            {
                var transactions = serviceOrder.Invoice.Transactions;

                // Ưu tiên giao dịch ĐÃ THANH TOÁN (SUCCESS) để lấy đúng phương thức đã trả.
                // Nếu chưa có (đơn còn PENDING/chưa thanh toán) thì mới lấy giao dịch đang chờ
                // gần nhất để biết khách đang định trả bằng phương thức nào.
                var paidTx = transactions
                    ?.Where(t => t.TransactionStatus == TransactionStatuses.Success)
                    .OrderByDescending(t => t.Created)
                    .FirstOrDefault();

                var activeTx = paidTx ?? transactions
                    ?.Where(t => t.TransactionStatus == TransactionStatuses.Pending)
                    .OrderByDescending(t => t.Created)
                    .FirstOrDefault();

                dto.DesignServicePaymentStatus = serviceOrder.Invoice.PaymentStatus;
                dto.DesignServiceTotalAmount = serviceOrder.Invoice.TotalAmount;

                dto.Invoice = new OrderInvoiceSummaryDTO
                {
                    Id = serviceOrder.Invoice.Id,
                    InvoiceCode = serviceOrder.Invoice.InvoiceCode,
                    PaymentStatus = serviceOrder.Invoice.PaymentStatus,
                    TotalAmount = serviceOrder.Invoice.TotalAmount,
                    DueDate = serviceOrder.Invoice.DueDate,
                    PaymentMethod = activeTx?.PaymentMethod,
                };
            }
        }

        return dto;
    }
}
