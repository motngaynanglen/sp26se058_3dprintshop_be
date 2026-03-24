using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Mappings;
using sp26se058_3dprintshop_be.Application.Common.Models;

namespace sp26se058_3dprintshop_be.Application.ConceptTags.Queries;

public class GetConceptTagsByNameQuery : PaginationRequest,IRequest<PaginatedList<ConceptTagDTO>>
{
    [DefaultValue("Resin")]
    public string Search { get; set; } = string.Empty;
    public class GetConceptTagsListQueryHandler : IRequestHandler<GetConceptTagsByNameQuery, PaginatedList<ConceptTagDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetConceptTagsListQueryHandler(
            IApplicationDbContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<ConceptTagDTO>> Handle(GetConceptTagsByNameQuery request, CancellationToken cancellationToken)
        {
            var query = _context.ConceptTags.AsNoTracking();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var s = request.Search.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(s) || x.Description!.ToLower().Contains(s));
            } 
            return await query
                .ProjectTo<ConceptTagDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }

    }
}
