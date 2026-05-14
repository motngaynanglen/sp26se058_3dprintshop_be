using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Commands;

[Authorize(Roles = Roles.StaffOrManager)]
public record UpdateDesignVersionHistoryIsPrintableCommand : IRequest<bool>
{
    [JsonIgnore]
    public Guid Id { get; init; }
}

public class UpdateDesignVersionHistoryIsPrintableCommandHandler : IRequestHandler<UpdateDesignVersionHistoryIsPrintableCommand, bool>
{
    private readonly IApplicationDbContext _context;
    public UpdateDesignVersionHistoryIsPrintableCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<bool> Handle(UpdateDesignVersionHistoryIsPrintableCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.DesignVersionHistorys
            .FirstOrDefaultAsync(dw => dw.Id == request.Id, cancellationToken);
        if (entity == null)
        {
            throw new DataNotFoundException(nameof(DesignWork),  request.Id);
        }
        // Cập nhật trạng thái có thể in
        entity.IsPrintable = true; // Hoặc false tùy theo yêu cầu
        await _context.SaveChangesAsync(cancellationToken);
        return true; // Trả về true nếu cập nhật thành công
    }
}
