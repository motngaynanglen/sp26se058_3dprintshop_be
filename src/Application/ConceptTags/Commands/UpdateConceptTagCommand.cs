using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.ConceptTags.Commands;

public record UpdateConceptTagCommand : IRequest<Guid>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    [DefaultValue("Resin")]
    public string? Name { get; init; }
    [DefaultValue("Sản phẩm được in từ Resin")]
    public string? Description { get; init; }
    [DefaultValue(false)]
    public bool? IsActive { get; init; }
    [DefaultValue(false)]
    public bool? IsMainTag { get; init; }

}

public class UpdateConceptTagCommandHandler : IRequestHandler<UpdateConceptTagCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateConceptTagCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }
    public async Task<Guid> Handle(UpdateConceptTagCommand request, CancellationToken cancellationToken)
    {
        var conceptTag = await _context.ConceptTags.FindAsync(new object[] { request.Id }, cancellationToken);
        if (conceptTag == null)
        {
            throw new DataNotFoundException(nameof(ConceptTag), request.Id);
        }
        var exists = _context.ConceptTags.Any(ct => ct.Name == request.Name && ct.Id != request.Id);
        if (exists)
        {
            throw new DuplicateException("Đã tồn tại tag ý tưởng với tên " + request.Name + ".");
        }

        //UpdateAllChildTags(conceptTag, request.IsActive ?? conceptTag.IsActive);

        conceptTag.Name = request.Name ?? conceptTag.Name;
        conceptTag.Description = request.Description ?? conceptTag.Description;
        conceptTag.IsActive = request.IsActive ?? conceptTag.IsActive;
        conceptTag.IsMainTag = request.IsMainTag ?? conceptTag.IsMainTag;

        conceptTag.LastModified = CoreHelper.SystemTimeNow;
        conceptTag.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return conceptTag.Id;
    }
    private bool UpdateAllChildTags(ConceptTag conceptTag, bool isActive)
    {
        if (conceptTag.DesignTags.Any())
        {
            foreach (var tag in conceptTag.DesignTags)
            {
                if (tag.IsActive != isActive)
                {
                    tag.IsActive = isActive;
                    tag.LastModified = CoreHelper.SystemTimeNow;
                    tag.LastModifiedBy = _user.Username;
                }
            }
        }
        return true;
    }
}
