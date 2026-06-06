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

public record DeleteConceptTagCommand : IRequest<bool>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    //public bool IsActive { get; init; } = false;
}

public class DeleteConceptTagCommandHandler : IRequestHandler<DeleteConceptTagCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    public DeleteConceptTagCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }
    public async Task<bool> Handle(DeleteConceptTagCommand request, CancellationToken cancellationToken)
    {
        var conceptTag = await _context.ConceptTags.Include(c => c.DesignTags).FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (conceptTag == null)
        {
            throw new Exception("Không tìm thấy Concept tag với Id " + request.Id);
        }

        DeleteAllChildTags(conceptTag);

        conceptTag.Deleted = CoreHelper.SystemTimeNow;
        conceptTag.DeletedBy = _user.Username;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
    private bool DeleteAllChildTags(ConceptTag conceptTag)
    {
        if (conceptTag.DesignTags.Any())
        {
            foreach (var tag in conceptTag.DesignTags)
            {
                if (tag.Deleted != null)
                {
                    tag.Deleted = CoreHelper.SystemTimeNow;
                    tag.DeletedBy = _user.Username;
                }
            }
        }
        return true;
    }
}
