using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.ServiceOptions.Commands;
public record DeleteServiceOptionCommand(Guid Id) : IRequest<object>;
public class DeleteServiceOptionCommandHandler : IRequestHandler<DeleteServiceOptionCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteServiceOptionCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<object> Handle(DeleteServiceOptionCommand request, CancellationToken ct)
    {
        var entity = await _context.ServiceOptions.FindAsync(new object[] { request.Id }, ct);
        if (entity == null) throw new Exception("Không tìm thấy tùy chọn.");

        // Soft delete: Chỉ tắt hoạt động để không cho khách hàng mới chọn
        entity.IsActive = false;
        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(ct);
        return true;
        //return new { Message = "Đã ngưng hoạt động tùy chọn này" };
    }
    
}
