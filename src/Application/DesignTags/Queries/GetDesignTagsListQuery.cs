using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.DesignTags.Queries;

public class GetDesignTagsListQuery : IRequest<IEnumerable<DesignTagDTO>>
{
    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid DesignTemplateId { get; init; }

    public class GetDesignTaskListQueryHandler : IRequestHandler<GetDesignTagsListQuery, IEnumerable<DesignTagDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        public GetDesignTaskListQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<IEnumerable<DesignTagDTO>> Handle(GetDesignTagsListQuery request, CancellationToken cancellationToken)
        {
            var designTags = await _context.DesignTags
                .AsNoTracking()
                .Where(dt => dt.DesignTemplateId == request.DesignTemplateId && dt.IsActive && dt.ConceptTag.IsActive)
                .OrderBy(dt => dt.Id)
                .ProjectTo<DesignTagDTO>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
            return designTags;
        }
    }
}
