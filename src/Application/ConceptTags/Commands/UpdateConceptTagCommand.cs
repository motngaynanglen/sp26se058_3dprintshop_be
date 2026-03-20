using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.ConceptTags.Commands;

public record UpdateConceptTagCommand : IRequest<Guid>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    [DefaultValue("Resin")]
    public string Name { get; init; } = null!;
    [DefaultValue("Sản phẩm được in từ Resin")]
    public string Description { get; init; } = null!;
    public bool IsActive { get; init; } = false;
}

public class UpdateConceptTagCommandHandler : IRequestHandler<UpdateConceptTagCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    public UpdateConceptTagCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<Guid> Handle(UpdateConceptTagCommand request, CancellationToken cancellationToken)
    {
        var conceptTag = await _context.ConceptTags.FindAsync(new object[] { request.Id }, cancellationToken);
        if (conceptTag == null)
        {
            throw new Exception("Không tìm thấy Concept tag với Id " + request.Id);
        }
        var exists = _context.ConceptTags.Any(ct => ct.Name == request.Name && ct.Id != request.Id);
        if (exists)
        {
            throw new Exception("Đã tồn tại Concept tag với tên " + request.Name + ".");
        }
        conceptTag.Name = request.Name;
        conceptTag.Description = request.Description;
        conceptTag.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return conceptTag.Id;
    }
}
