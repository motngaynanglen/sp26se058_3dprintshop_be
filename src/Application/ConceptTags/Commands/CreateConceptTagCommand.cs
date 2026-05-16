using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.ConceptTags.Commands;

public class CreateConceptTagCommand : IRequest<Guid>
{
    [DefaultValue("Resin")]
    public string Name { get; init; } = null!;
    [DefaultValue("Sản phẩm được in từ Resin")]
    public string Description { get; init; } = null!;
    public bool IsActive { get; init; } = false;
    public bool IsMainTag { get; init; } = false;

}

public class CreateConceptTagCommandHandler : IRequestHandler<CreateConceptTagCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    public CreateConceptTagCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<Guid> Handle(CreateConceptTagCommand request, CancellationToken cancellationToken)
    {
        var exists = _context.ConceptTags.Any(ct => ct.Name == request.Name);
        if (exists)
        { 
            throw new DuplicateException("Đã tồn tại tag ý tưởng với tên " + request.Name + ".");
        }
            var newConceptTag = new ConceptTag
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive
        };
        _context.ConceptTags.Add(newConceptTag);
        await _context.SaveChangesAsync(cancellationToken);
        return newConceptTag.Id;
    }
}
