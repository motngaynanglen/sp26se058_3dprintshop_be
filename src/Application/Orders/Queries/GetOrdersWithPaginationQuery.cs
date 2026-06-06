using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Orders.Queries;

public class GetOrdersWithPaginationQuery : PaginationRequest, IRequest<PaginatedList<OrderDTO>>
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int? Priority { get; init; }
    [DefaultValue(false)]
    public bool? SortDescending { get; init; }
    [DefaultValue("created")]
    public string? SortBy { get; init; }

    public class GetOrdersWithPaginationQueryHandler : IRequestHandler<GetOrdersWithPaginationQuery, PaginatedList<OrderDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUser _user;

        public GetOrdersWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
        {
            _context = context;
            _mapper = mapper;
            _user = user;
        }

        public async Task<PaginatedList<OrderDTO>> Handle(GetOrdersWithPaginationQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Orders.Include(o=>o.OrderItems).AsNoTracking();

            if (_user.Role == Roles.CUSTOMER)
            {
                if (string.IsNullOrWhiteSpace(_user.Id))
                    throw new UnauthorizedAccessException("Cần đăng nhập.");

                var accountId = _user.Id.ToGuid();
                var customerId = await _context.Customers
                    .AsNoTracking()
                    .Where(c => c.AccountId == accountId)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (customerId == Guid.Empty)
                    throw new UnauthorizedAccessException("Không xác định được khách hàng.");

                query = query.Where(o => o.CustomerId == customerId);
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(o => o.Customer.Account.Username.Contains(request.Search) || o.Code.Contains(request.Search));
            }
            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(o => o.OrderStatus == request.Status);
            }
            if (request.Priority > 0)
            {
                query = query.Where(o => o.Priority == request.Priority);
            }
            // Sắp xếp
            if (request.SortDescending.HasValue)
            {
                query = request.SortBy?.ToLower() switch
                {
                    "created" => query.OrderByDescending(o => o.Created),
                    "priority" => query.OrderByDescending(o => o.Priority),
                    _ => query.OrderByDescending(o => o.Id)
                };
            }
            else
            {
                query = request.SortBy?.ToLower() switch
                {
                    "created" => query.OrderBy(o => o.Created),
                    "priority" => query.OrderBy(o => o.Priority),
                    _ => query.OrderBy(o => o.Id)
                };
            }
            var result = await query
                .ProjectTo<OrderDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);

            var orderIds = result.Items.Select(o => o.Id).ToList();
            if (orderIds.Count == 0)
                return result;

            var shipments = await _context.Shipments
                .AsNoTracking()
                .Include(s => s.ShippingAddress)
                .Where(s => orderIds.Contains(s.OrderId))
                .ToListAsync(cancellationToken);

            var shipmentByOrder = shipments
                .GroupBy(s => s.OrderId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.Created).First());

            var invoices = await _context.Invoices
                .AsNoTracking()
                .Where(i => orderIds.Contains(i.OrderId))
                .ToListAsync(cancellationToken);

            var invoiceByOrder = invoices.ToDictionary(i => i.OrderId);

            foreach (var order in result.Items)
            {
                if (shipmentByOrder.TryGetValue(order.Id, out var shipment))
                    order.Shipment = _mapper.Map<OrderShipmentSummaryDTO>(shipment);

                if (invoiceByOrder.TryGetValue(order.Id, out var invoice))
                    order.Invoice = _mapper.Map<OrderInvoiceSummaryDTO>(invoice);
            }

            return result;
        }
    }
}
