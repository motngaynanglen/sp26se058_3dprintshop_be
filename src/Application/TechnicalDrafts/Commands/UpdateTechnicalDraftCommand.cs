using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Application.TechnicalDrafts.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.TechnicalDrafts.Commands;

[Authorize(Roles = Roles.StaffOrManager)]
public record UpdateTechnicalDraftCommand : IRequest<TechnicalDraftDTO>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    public Guid? MaterialId { get; init; }
    public int? InfillDensity { get; init; }
    public decimal? LayerHeight { get; init; }
    public decimal? EstimatedWeightPerUnit { get; init; }
    public decimal? EstimatedPrintTimePerUnit { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? MarkupPercentage { get; init; }
    public string? TechnicalNote { get; init; }
}

public class UpdateTechnicalDraftCommandValidator : AbstractValidator<UpdateTechnicalDraftCommand>
{
    public UpdateTechnicalDraftCommandValidator()
    {
        RuleFor(x => x.InfillDensity).InclusiveBetween(0, 100).When(x => x.InfillDensity.HasValue);
        RuleFor(x => x.LayerHeight).GreaterThan(0).When(x => x.LayerHeight.HasValue);
        RuleFor(x => x.EstimatedWeightPerUnit).GreaterThan(0).When(x => x.EstimatedWeightPerUnit.HasValue);
        RuleFor(x => x.MarkupPercentage).GreaterThanOrEqualTo(0).When(x => x.MarkupPercentage.HasValue);
    }
}

public class UpdateTechnicalDraftCommandHandler : IRequestHandler<UpdateTechnicalDraftCommand, TechnicalDraftDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public UpdateTechnicalDraftCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<TechnicalDraftDTO> Handle(UpdateTechnicalDraftCommand request, CancellationToken cancellationToken)
    {
        var draft = await _context.TechnicalDrafts
            .Include(x => x.DesignVersionHistory)
                .ThenInclude(x => x.DesignWork)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (draft == null)
        {
            throw new DataNotFoundException(nameof(TechnicalDraft), request.Id);
        }

        if (draft.IsConfirmed || draft.DesignVersionHistory.DesignWork.IsLocked)
        {
            throw new BusinessException("Bản nháp kỹ thuật đã được xác nhận hoặc công việc thiết kế đã khóa, không thể cập nhật.");
        }

        if (request.MaterialId.HasValue)
        {
            var materialExists = await _context.Materials
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.MaterialId.Value, cancellationToken);

            if (!materialExists)
            {
                throw new DataNotFoundException(nameof(Material), request.MaterialId.Value);
            }

            draft.MaterialId = request.MaterialId.Value;
        }

        if (request.InfillDensity.HasValue) draft.InfillDensity = request.InfillDensity.Value;
        if (request.LayerHeight.HasValue) draft.LayerHeight = request.LayerHeight.Value;
        if (request.EstimatedWeightPerUnit.HasValue) draft.EstimatedWeightPerUnit = request.EstimatedWeightPerUnit.Value;
        if (request.EstimatedPrintTimePerUnit.HasValue) draft.EstimatedPrintTimePerUnit = request.EstimatedPrintTimePerUnit.Value;
        if (request.UnitPrice.HasValue) draft.UnitPrice = request.UnitPrice.Value;
        if (request.MarkupPercentage.HasValue) draft.MarkupPercentage = request.MarkupPercentage.Value;
        if (request.TechnicalNote != null) draft.TechnicalNote = request.TechnicalNote;

        draft.LastModified = CoreHelper.SystemTimeNow;
        draft.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.TechnicalDrafts
            .AsNoTracking()
            .Where(x => x.Id == draft.Id)
            .ProjectTo<TechnicalDraftDTO>(_mapper.ConfigurationProvider)
            .SingleAsync(cancellationToken);
    }
}
