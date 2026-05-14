using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Materials.Commands;

[Authorize(Roles = Roles.MANAGER)]
public record UpdateMaterialPriceCommand : IRequest<MaterialDTO>
{
    [JsonIgnore]
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid MaterialId { get; init; }
    [DefaultValue("10.0")]
    public decimal BaseCostPerGram { get; init; }
    [DefaultValue("10.0")]
    public decimal TotalServiceCostPerGram { get; init; }
    public DateTime EffectiveDate { get; init; }
}

public class UpdateMaterialPriceCommandHandler : IRequestHandler<UpdateMaterialPriceCommand, MaterialDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;
    public UpdateMaterialPriceCommandHandler(IApplicationDbContext context,IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }
    public async Task<MaterialDTO> Handle(UpdateMaterialPriceCommand request, CancellationToken cancellationToken)
    {
        var material = await _context.Materials
            .Include(m => m.PriceHistories)
            .FirstOrDefaultAsync(m => m.Id == request.MaterialId, cancellationToken);

        if (material == null) throw new DataNotFoundException(nameof(Material), request.MaterialId);

        // 1. Tìm và hạ cờ IsCurrent của giá hiện tại
        var currentPrice = material.PriceHistories.FirstOrDefault(p => p.IsCurrent);
        if (currentPrice != null)
        {
            currentPrice.IsCurrent = false;
            currentPrice.LastModified = CoreHelper.SystemTimeNow;
            currentPrice.LastModifiedBy = _user.Username;
        }

        // 2. Thêm bản ghi giá mới
        var newPrice = new MaterialPriceHistory
        {
            MaterialId = request.MaterialId,
            BaseCostPerGram = request.BaseCostPerGram,
            TotalServiceCostPerGram = request.TotalServiceCostPerGram,
            EffectiveDate = request.EffectiveDate,
            IsCurrent = true, // Giá mới sẽ là giá hiện hành
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };

        // 3. Nếu vật liệu đang bị ẩn (do chưa có giá), hãy kích hoạt nó
        if (!material.IsActive) material.IsActive = true;

        _context.MaterialPriceHistories.Add(newPrice);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException(nameof(Material), ex.Message);
        }

        return _mapper.Map<MaterialDTO>(material);
    }
}
