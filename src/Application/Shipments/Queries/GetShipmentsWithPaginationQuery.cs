using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace sp26se058_3dprintshop_be.Application.Shipments.Queries;
public class GetShipmentsWithPaginationQuery : PaginationRequest,IRequest<PaginatedList<ShipmentDTO>>
{
    [DefaultValue("PENDING")]
    public string? Status { get; init; }

    [DefaultValue("SPX123")]
    public string? Search { get; init; }

    // Paging
    [DefaultValue("Created")]
    public string? SortBy { get; init; } // "Tracking", "Fee", "Created", "Shipped"
    public bool SortDescending { get; init; } = false;

    public class GetShipmentsWithPaginationQueryHandler : IRequestHandler<GetShipmentsWithPaginationQuery, PaginatedList<ShipmentDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        public GetShipmentsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<PaginatedList<ShipmentDTO>> Handle(GetShipmentsWithPaginationQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Shipments.Include(x => x.ShippingAddress).AsNoTracking();
            if (!string.IsNullOrEmpty(request.Search))
            {
                var s = request.Search.ToLower();
                query = query.Where(x =>
                    (x.TrackingNumber != null && x.TrackingNumber.ToLower().Contains(s)) ||
                    x.ShippingAddress.ReceiverName.ToLower().Contains(s));
            }
            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(x => x.ShipmentStatus == request.Status.ToUpper());
            }
            query = request.SortBy switch
            {
                "Tracking" => request.SortDescending ? query.OrderByDescending(x => x.TrackingNumber) : query.OrderBy(x => x.TrackingNumber),
                "Fee" => request.SortDescending ? query.OrderByDescending(x => x.ShippingFee) : query.OrderBy(x => x.ShippingFee),
                "Created" => request.SortDescending ? query.OrderByDescending(x => x.Created) : query.OrderBy(x => x.Created),
                "Shipped" => request.SortDescending ? query.OrderByDescending(x => x.ShippedAt) : query.OrderBy(x => x.ShippedAt),
                "Delivered" => request.SortDescending ? query.OrderByDescending(x => x.DeliveredAt) : query.OrderBy(x => x.DeliveredAt),
                _ => request.SortDescending ? query.OrderByDescending(x => x.Created) : query.OrderBy(x => x.Created)
            };
            // Map in-memory: ProjectTo fails on nested FullAddress formatting.
            var page = await query.PaginatedListAsync(request.PageNumber, request.PageSize);
            var items = _mapper.Map<List<ShipmentDTO>>(page.Items);
            return new PaginatedList<ShipmentDTO>(
                items,
                page.Metadata.TotalCount,
                page.Metadata.PageNumber,
                page.Metadata.PageSize);

        }
    }
}
