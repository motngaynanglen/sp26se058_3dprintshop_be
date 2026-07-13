using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Feedbacks.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Commands;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record SwitchFeedbackStatusCommand : IRequest<FeedbackDTO>
{
    [JsonInclude]
    public Guid Id { get; init; }

}
public class SwitchFeedbackStatusCommandHandler : IRequestHandler<SwitchFeedbackStatusCommand, FeedbackDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public SwitchFeedbackStatusCommandHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public async Task<FeedbackDTO> Handle(SwitchFeedbackStatusCommand request, CancellationToken ct)
    {
        var entity = await _context.Feedbacks
            .FirstOrDefaultAsync(f => f.Id == request.Id, ct)
            ?? throw new DataNotFoundException(nameof(Feedback), request.Id);

        entity.IsHidden = !entity.IsHidden;
        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException(nameof(Feedback), ex.InnerException?.Message ?? ex.Message);
        }

        return _mapper.Map<FeedbackDTO>(entity);
    }
}
