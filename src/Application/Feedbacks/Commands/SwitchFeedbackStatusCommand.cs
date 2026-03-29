using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Commands;
public record SwitchFeedbackStatusCommand : IRequest<Guid>
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; set; }
    
}
public class SwitchFeedbackStatusCommandHandler : IRequestHandler<SwitchFeedbackStatusCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public SwitchFeedbackStatusCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(SwitchFeedbackStatusCommand request, CancellationToken ct)
    {
        var entity = await _context.Feedbacks.FirstOrDefaultAsync(f => f.Id == request.Id, ct);
        if (entity == null) throw new Exception(nameof(Feedback) +" not found "+ request.Id.ToString());

        entity.IsHidden = !entity.IsHidden;

        await _context.SaveChangesAsync(ct);
        return entity.Id;
    }
}
