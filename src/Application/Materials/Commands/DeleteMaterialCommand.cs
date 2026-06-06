using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Materials.Commands;

public record DeleteMaterialCommand : IRequest<bool>
{
    [JsonIgnore] // Ẩn khỏi JSON Body và Swagger
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; init; }
}

public class DeleteMaterialCommandHandler : IRequestHandler<DeleteMaterialCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteMaterialCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context; 
        _user = user;
    }

    public async Task<bool> Handle(DeleteMaterialCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id;
        var material = await _context.Materials
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (material == null)
        {
            throw new Exception("Material not found");
        }
        material.IsActive = !material.IsActive;

        if (material.IsActive)
        {
            material.Deleted = null;
            material.DeletedBy = null;
        }

        material.LastModified = DateTimeOffset.UtcNow;
        material.LastModifiedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}


