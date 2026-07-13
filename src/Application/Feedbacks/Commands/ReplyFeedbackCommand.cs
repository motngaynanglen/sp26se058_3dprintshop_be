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
public record ReplyFeedbackCommand : IRequest<FeedbackDTO>
{
    [JsonIgnore]
    public Guid Id { get; set; }
    [DefaultValue("Cảm ơn đã ủng hộ!")]
    public string StaffReply { get; set; } = null!;
}
public class ReplyFeedbackCommandHandler : IRequestHandler<ReplyFeedbackCommand, FeedbackDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;
    public ReplyFeedbackCommandHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }
    public class ReplyFeedbackCommandValidator : AbstractValidator<ReplyFeedbackCommand>
    {
        public ReplyFeedbackCommandValidator()
        {
            RuleFor(v => v.StaffReply)
                .NotEmpty().WithMessage("Nội dung phản hồi không được để trống.")
                .MaximumLength(1000).WithMessage("Nội dung phản hồi không quá 1000 ký tự.");
        }
    }
    public async Task<FeedbackDTO> Handle(ReplyFeedbackCommand request, CancellationToken ct)
    {
        var entity = await _context.Feedbacks
            .FirstOrDefaultAsync(f => f.Id == request.Id, ct)
            ?? throw new DataNotFoundException(nameof(Feedback), request.Id);

        entity.StaffReply = request.StaffReply;
        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;
        entity.RepliedDate = CoreHelper.SystemTimeNow;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException(nameof(Feedback), $"{ex.InnerException?.Message ?? ex.Message}");
        }
        return _mapper.Map<FeedbackDTO>(entity);
    }
}
