using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Commands;
[Authorize(Roles =  Roles.MANAGER + "," + Roles.STAFF + "," + Roles.CUSTOMER)]
public record DeleteFeedbackStatusCommand : IRequest<bool>
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; set; }

}
public class DeleteFeedbackStatusCommandHandler : IRequestHandler<DeleteFeedbackStatusCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteFeedbackStatusCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<bool> Handle(DeleteFeedbackStatusCommand request, CancellationToken ct)
    {
        var userRole = _user.Role;
        var userId = _user.Id.ToGuid();

        var entity = await _context.Feedbacks
            .Include(f => f.OrderItem) // Để lấy thông tin Order -> Customer nếu cấu hình của Bách đi theo đường này
                .ThenInclude(oi => oi.Order)
            .FirstOrDefaultAsync(f => f.Id == request.Id, ct);

        // 2. Check tồn tại
        if (entity == null)
            throw new DataNotFoundException(nameof(Feedback), request.Id);
        if (userRole == Roles.CUSTOMER)
        {
            // Kiểm tra UserId từ Token có khớp với AccountId của chủ đơn hàng không
            if (entity.OrderItem.Order.CustomerId != userId)
            {
                throw new ForbiddenAccessException("Bạn không có quyền xóa đánh giá của người khác.");
            }
        }

        entity.Deleted = CoreHelper.SystemTimeNow;
        entity.DeletedBy = _user.Username;
        //_context.Feedbacks.Remove(entity);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new DeleteFailureException(nameof(Feedback), $"{ex.InnerException?.Message ?? ex.Message}");
        }
        return true;
    }
}
