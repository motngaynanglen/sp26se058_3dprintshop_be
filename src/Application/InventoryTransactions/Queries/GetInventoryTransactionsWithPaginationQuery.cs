using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.InventoryTransactions.Queries;
[Authorize(Roles = Roles.MANAGER + "," + Roles.STAFF)]
public class GetInventoryTransactionsWithPaginationQuery : PaginationRequest, IRequest<PaginatedList<InventoryTransactionDTO>>
{
    public Guid? DesignVariantId { get; set; }
    [SwaggerConstant(typeof(InventoryTransactionTypes))]
    public string? Type { get; set; }
}
public class GetInventoryTransactionsWithPaginationQueryValidator : AbstractValidator<GetInventoryTransactionsWithPaginationQuery>
{
    public GetInventoryTransactionsWithPaginationQueryValidator()
    {
        // 1. Validate DesignVariantId (nếu có giá trị thì không được là Guid trống)
        RuleFor(x => x.DesignVariantId)
            .NotEqual(Guid.Empty)
            .When(x => x.DesignVariantId.HasValue)
            .WithMessage("Mã biến thể thiết kế không hợp lệ.");

        // 2. Validate Type (Phải nằm trong danh sách định nghĩa sẵn ở InventoryTransactionTypes)
        var validTypes = typeof(InventoryTransactionTypes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy)
            .Select(f => f.GetValue(null)?.ToString())
            .ToList();

        RuleFor(x => x.Type)
            .Must(type => string.IsNullOrEmpty(type) || validTypes.Contains(type))
            .WithMessage($"Loại giao dịch không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validTypes)}");

        // 3. Validate Pagination (Kế thừa từ PaginationRequest)
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Số trang phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Kích thước trang phải lớn hơn hoặc bằng 1.");
    }
}
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
        if (request.DesignVariantId.HasValue && request.DesignVariantId != Guid.Empty)
        {
            query = query.Where(x => x.DesignVariantId == request.DesignVariantId);
        }

        // 2. Filter theo Type (So sánh string)
        if (!string.IsNullOrEmpty(request.Type))
        {
            query = query.Where(x => x.Type.ToUpper() == request.Type);
        }

        return await query
            .OrderByDescending(x => x.Created)
            // ProjectTo sẽ tự xử lý các logic null-check trong Mapping thành SQL
            .ProjectTo<InventoryTransactionDTO>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}



