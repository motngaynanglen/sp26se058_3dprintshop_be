using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;

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
        public GetOrdersWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<PaginatedList<OrderDTO>> Handle(GetOrdersWithPaginationQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Orders.AsNoTracking();
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
            return await query
                .ProjectTo<OrderDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }
    }
}
