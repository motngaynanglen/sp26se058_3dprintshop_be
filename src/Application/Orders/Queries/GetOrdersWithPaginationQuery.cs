using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.Orders.Queries;
[Authorize(Roles = Roles.MANAGER + "," + Roles.STAFF + "," + Roles.CUSTOMER)]
public class GetOrdersWithPaginationQuery : PaginationRequest, IRequest<PaginatedList<OrderDTO>>
{
    [DefaultValue("")]
    public string? Search { get; init; }
    [DefaultValue(OrderStatuses.Completed)]
    public string? Status { get; init; }
    [DefaultValue(0)]
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
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Invoice)
                .AsNoTracking();

            if (_user.Role == Roles.CUSTOMER)
            {
                var userId = _user.Id.ToGuid();
                query = query.Where(o => o.Customer.AccountId == userId);
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchLower = request.Search.ToLower();
                query = query.Where(o => o.Customer.Account.Username.Contains(request.Search) 
                                    || o.Code.ToLower().Contains(request.Search));
            }
            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(o => o.OrderStatus == request.Status);
            }
            if (request.Priority.HasValue && request.Priority > 0)
            {
                query = query.Where(o => o.Priority == request.Priority);
            }


            // Sắp xếp
            query = ApplySorting(query, request);

            var result = await query
                .ProjectTo<OrderDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);

            // Enrich tóm tắt hóa đơn (cột TT) + vận đơn mới nhất (cột Vận chuyển) cho từng dòng.
            // OrderDTO mapping vốn Ignore Invoice/Shipment (chỉ enrich ở GetOrderDetail) → list bị trống.
            var orderIds = result.Items.Select(o => o.Id).ToList();
            if (orderIds.Count > 0)
            {
                var invoices = await _context.Invoices.AsNoTracking()
                    .Where(i => orderIds.Contains(i.OrderId))
                    .Select(i => new { i.OrderId, i.Id, i.InvoiceCode, i.PaymentStatus, i.SubTotal, i.ShippingFee, i.TotalAmount, i.DueDate })
                    .ToListAsync(cancellationToken);

                var shipments = await _context.Shipments.AsNoTracking()
                    .Where(s => orderIds.Contains(s.OrderId))
                    .OrderByDescending(s => s.Created)
                    .Select(s => new { s.OrderId, s.Id, s.ShipmentStatus, s.Carrier, s.CarrierName, s.CarrierOrderCode, s.TrackingNumber, s.ShippingFee })
                    .ToListAsync(cancellationToken);

                foreach (var dto in result.Items)
                {
                    var inv = invoices.FirstOrDefault(i => i.OrderId == dto.Id);
                    if (inv != null)
                    {
                        dto.Invoice = new OrderInvoiceSummaryDTO
                        {
                            Id = inv.Id,
                            InvoiceCode = inv.InvoiceCode,
                            PaymentStatus = inv.PaymentStatus,
                            SubTotal = inv.SubTotal,
                            ShippingFee = inv.ShippingFee,
                            TotalAmount = inv.TotalAmount,
                            DueDate = inv.DueDate,
                        };
                    }

                    var ship = shipments.FirstOrDefault(s => s.OrderId == dto.Id); // FirstOrDefault = vận đơn mới nhất
                    if (ship != null)
                    {
                        dto.Shipment = new OrderShipmentSummaryDTO
                        {
                            Id = ship.Id,
                            ShipmentStatus = ship.ShipmentStatus,
                            Carrier = ship.Carrier,
                            CarrierName = ship.CarrierName,
                            CarrierOrderCode = ship.CarrierOrderCode,
                            TrackingNumber = ship.TrackingNumber,
                            ShippingFee = ship.ShippingFee,
                        };
                    }
                }
            }

            return result;
        }
        private IQueryable<Order> ApplySorting(IQueryable<Order> query, GetOrdersWithPaginationQuery request)
        {
            bool descending = request.SortDescending ?? false;
            string sortBy = request.SortBy?.ToLower() ?? "created";

            return sortBy switch
            {
                "created" => descending ? query.OrderByDescending(o => o.Created) : query.OrderBy(o => o.Created),
                "priority" => descending ? query.OrderByDescending(o => o.Priority) : query.OrderBy(o => o.Priority),
                "totalprice" => descending ? query.OrderByDescending(o => o.TotalPrice) : query.OrderBy(o => o.TotalPrice),
                _ => descending ? query.OrderByDescending(o => o.Id) : query.OrderBy(o => o.Id)
            };
        }
    }
}
