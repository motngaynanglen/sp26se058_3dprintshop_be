using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.DesignTemplates.Queries.GetDesignTemplatesWithPagination;

namespace sp26se058_3dprintshop_be.Application.DesignTemplates.Queries;

public class GetDesignTemplateDetailQuery : IRequest<DesignTemplateDTO>
{
    public Guid Id { get; init; }

    public class GetDesignTemplateDetailQueryHandler : IRequestHandler<GetDesignTemplateDetailQuery, DesignTemplateDTO>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        public GetDesignTemplateDetailQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<DesignTemplateDTO> Handle(GetDesignTemplateDetailQuery request, CancellationToken cancellationToken)
        {
            var designTemplate = await _context.DesignTemplates
                .AsNoTracking()
                .ProjectTo<DesignTemplateDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(dt => dt.Id == request.Id, cancellationToken);
            if (designTemplate == null)
            {
                throw new Exception("Không tìm thấy mẫu thiết kế.");
            }
            return designTemplate;
        }
    }
}
