using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.ConceptTags.Queries;

namespace sp26se058_3dprintshop_be.Application.DesignTags.Queries;

public class GetDesignTagsByTemplateIdQuery : IRequest<IEnumerable<DesignTagDTO>>
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid TemplateId { get; set; }
    public class GetDesignTagsByTemplateIdQueryHandler : IRequestHandler<GetDesignTagsByTemplateIdQuery, IEnumerable<DesignTagDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetDesignTagsByTemplateIdQueryHandler(
            IApplicationDbContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<DesignTagDTO>> Handle(GetDesignTagsByTemplateIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.DesignTags.Include(x => x.ConceptTag)
                .AsNoTracking()
                .Where(x => x.DesignTemplateId == request.TemplateId && x.IsActive && x.ConceptTag.IsActive)
                .OrderBy(x => x.ConceptTag.Name)
                .ProjectTo<DesignTagDTO>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

    }
}
