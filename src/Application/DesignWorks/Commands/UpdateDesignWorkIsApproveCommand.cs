using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.DesignWorks.Queries;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Commands;

public record UpdateDesignWorkIsApproveCommand : IRequest<bool>
{
    [JsonIgnore]
    public Guid Id { get; init; }
}

public class UpdateDesignWorkIsApproveCommandHandler : IRequestHandler<UpdateDesignWorkIsApproveCommand, bool>
{
    private readonly IApplicationDbContext _context;
    public UpdateDesignWorkIsApproveCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<bool> Handle(UpdateDesignWorkIsApproveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.DesignVersionHistorys
            .FirstOrDefaultAsync(dw => dw.Id == request.Id, cancellationToken);
        if (entity == null)
        {
            throw new NotFoundException(nameof(DesignWork), "Không tìm thấy công việc thiết kế với id " + request.Id);
        }
        // Cập nhật trạng thái phê duyệt
        entity.IsApproved = true; // Hoặc false tùy theo yêu cầu
        await _context.SaveChangesAsync(cancellationToken);
        return true; // Trả về true nếu cập nhật thành công
    }
}
