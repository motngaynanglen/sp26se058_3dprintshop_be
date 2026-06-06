using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.InventoryTransactions.Queries;
public class GetInventoryTransactionsWithPaginationQuery : PaginationRequest, IRequest<PaginatedList<InventoryTransactionDTO>>
{
    public Guid? DesignVariantId { get; init; }
    public string? Type { get; init; }
    /// <summary>IN = nhập kho (quantity &gt; 0), OUT = xuất kho (quantity &lt; 0)</summary>
    public string? Direction { get; init; }
    public class GetInventoryTransactionsWithPaginationQueryHandler : IRequestHandler<GetInventoryTransactionsWithPaginationQuery, PaginatedList<InventoryTransactionDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetInventoryTransactionsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<InventoryTransactionDTO>> Handle(GetInventoryTransactionsWithPaginationQuery request, CancellationToken ct)
        {
            var query = _context.InventoryTransactions.AsNoTracking();

            // 1. Filter theo Variant
            if (request.DesignVariantId.HasValue)
            {
                query = query.Where(x => x.DesignVariantId == request.DesignVariantId);
            }

            // 2. Filter theo Type (So sánh string)
            if (!string.IsNullOrEmpty(request.Type))
            {
                query = query.Where(x => x.Type == request.Type);
            }

            // 3. Filter theo hướng nhập/xuất
            if (!string.IsNullOrEmpty(request.Direction))
            {
                if (request.Direction.Equals("IN", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => x.Quantity > 0);
                }
                else if (request.Direction.Equals("OUT", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(x => x.Quantity < 0);
                }
            }
            return await query
                .Include(x => x.DesignVariant)
                .Include(x => x.Staff)
                    .ThenInclude(s => s!.Account)
                .OrderByDescending(x => x.Created)
                // ProjectTo sẽ tự xử lý các logic null-check trong Mapping thành SQL
                .ProjectTo<InventoryTransactionDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }
    }
}


