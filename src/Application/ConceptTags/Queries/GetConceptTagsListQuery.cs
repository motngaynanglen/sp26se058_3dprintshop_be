using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.ConceptTags.Queries;

public class GetConceptTagsListQuery : IRequest<IEnumerable<ConceptTagDTO>>
{
    public class GetConceptTagsListQueryHandler
    : IRequestHandler<GetConceptTagsListQuery, IEnumerable<ConceptTagDTO>>
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

        public async Task<List<ConceptTagDTO>> Handle(
            GetConceptTagsListQuery request,
            CancellationToken cancellationToken)
        {
            return await _context.ConceptTags
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ProjectTo<ConceptTagDTO>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

        Task<IEnumerable<ConceptTagDTO>> IRequestHandler<GetConceptTagsListQuery, IEnumerable<ConceptTagDTO>>.Handle(GetConceptTagsListQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
