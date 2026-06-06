using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Commands;
public record DeleteFeedbackStatusCommand : IRequest<Guid>
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; set; }

}
public class DeleteFeedbackStatusCommandHandler : IRequestHandler<DeleteFeedbackStatusCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteFeedbackStatusCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Guid> Handle(DeleteFeedbackStatusCommand request, CancellationToken ct)
    {
        var userRole = _user.Role ?? "";
        var userId = _user.Id.ToGuid();
        var entity = await _context.Feedbacks
            .Include(f => f.Customer)
            .FirstOrDefaultAsync(f => f.Id == request.Id 
                 && (userRole == Roles.CUSTOMER ? (f.Customer.AccountId == userId) : true), ct);
        if (entity == null) throw new Exception(nameof(Feedback) + " not found " + request.Id.ToString());

        entity.Deleted = DateTimeOffset.Now;
        entity.DeletedBy = _user.Username;
        //_context.Feedbacks.Remove(entity);

        await _context.SaveChangesAsync(ct);
        return entity.Id;
    }
}
