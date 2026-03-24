using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Commands;
public record ReplyFeedbackCommand : IRequest<Guid>
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    [JsonIgnore]
    public Guid Id { get; set; }
    [DefaultValue("Cảm ơn đã ủng hộ!")]
    public string StaffReply { get; set; } = null!;
}
public class ReplyFeedbackCommandHandler : IRequestHandler<ReplyFeedbackCommand,Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public ReplyFeedbackCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Guid> Handle(ReplyFeedbackCommand request, CancellationToken ct)
    {
        var entity = await _context.Feedbacks.FindAsync(new object[] { request.Id }, ct);

        if (entity == null) throw new NotFoundException(nameof(Feedback), request.Id.ToString());

        entity.StaffReply = request.StaffReply;
        // Có thể thêm ngày phản hồi nếu sau này cần: entity.RepliedDate = DateTime.Now;

        await _context.SaveChangesAsync(ct);
        return entity.Id;
    }
}
