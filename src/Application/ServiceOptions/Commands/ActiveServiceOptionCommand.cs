using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.ServiceOptions.Commands;
public record ActiveServiceOptionCommand(Guid Id) : IRequest<object>;
public class ActiveServiceOptionCommandHandler : IRequestHandler<ActiveServiceOptionCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public ActiveServiceOptionCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<object> Handle(ActiveServiceOptionCommand request, CancellationToken ct)
    {
        var entity = await _context.ServiceOptions.FindAsync(new object[] { request.Id }, ct);
        if (entity == null) throw new DataNotFoundException(nameof(ServiceOption),request.Id);

        // Chỉ tắt hoạt động để không cho khách hàng mới chọn
        entity.IsActive = true;
        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException(nameof(ServiceOption), $"{ex.InnerException?.Message ?? ex.Message}");
        }
        return true;
        //return new { Message = "Đã ngưng hoạt động tùy chọn này" };
    }

}
