using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.ServiceOptions.Queries;
public record GetServiceOptionsQuery : IRequest<List<ServiceOptionDTO>>;

public class GetServiceOptionsQueryHandler : IRequestHandler<GetServiceOptionsQuery, List<ServiceOptionDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    public GetServiceOptionsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<ServiceOptionDTO>> Handle(GetServiceOptionsQuery request, CancellationToken ct)
    {
        return await _context.ServiceOptions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.GroupCode)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ProjectTo<ServiceOptionDTO>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}

[Authorize(Roles = Roles.StaffOrManager)]
public class GetServiceOptionsWithPaginationQuery : PaginationRequest, IRequest<PaginatedList<ServiceOptionDTO>>
{
    [DefaultValue(null)]
    public string? Search { get; init; }

    [DefaultValue(null)]
    public string? GroupCode { get; init; }

    [DefaultValue(null)]
    public string? SelectionType { get; init; }

    [DefaultValue(null)]
    public bool? IsActive { get; init; }

    [DefaultValue("Group")]
    public string? SortBy { get; init; }

    [DefaultValue(false)]
    public bool SortDescending { get; init; }
}

public class GetServiceOptionsWithPaginationQueryHandler : IRequestHandler<GetServiceOptionsWithPaginationQuery, PaginatedList<ServiceOptionDTO>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetServiceOptionsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<ServiceOptionDTO>> Handle(GetServiceOptionsWithPaginationQuery request, CancellationToken ct)
    {
        var query = _context.ServiceOptions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(search) ||
                x.Name.ToLower().Contains(search) ||
                (x.Description != null && x.Description.ToLower().Contains(search)) ||
                x.GroupCode.ToLower().Contains(search) ||
                x.GroupName.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.GroupCode))
        {
            var groupCode = request.GroupCode.Trim().ToUpper();
            query = query.Where(x => x.GroupCode == groupCode);
        }

        if (!string.IsNullOrWhiteSpace(request.SelectionType))
        {
            var selectionType = request.SelectionType.Trim().ToUpper();
            query = query.Where(x => x.SelectionType == selectionType);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        query = request.SortBy switch
        {
            "Code" => request.SortDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "Name" => request.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "Price" => request.SortDescending ? query.OrderByDescending(x => x.DefaultPrice) : query.OrderBy(x => x.DefaultPrice),
            "Created" => request.SortDescending ? query.OrderByDescending(x => x.Created) : query.OrderBy(x => x.Created),
            "Active" => request.SortDescending ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
            _ => request.SortDescending
                ? query.OrderByDescending(x => x.GroupCode).ThenByDescending(x => x.SortOrder).ThenByDescending(x => x.Name)
                : query.OrderBy(x => x.GroupCode).ThenBy(x => x.SortOrder).ThenBy(x => x.Name)
        };

        return await query
            .ProjectTo<ServiceOptionDTO>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
