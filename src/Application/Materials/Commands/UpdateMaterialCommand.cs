using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.Materials.Commands;

public record UpdateMateialCommand : IRequest<Guid>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    public decimal BaseCostPerGram { get; set; }
    public decimal TotalServiceCostPerGram { get; set; }
    public DateTime EffectiveDate { get; set; }

    public class UpdateMaterialCommandHandler : IRequestHandler<UpdateMateialCommand, Guid>
        {
            private readonly IApplicationDbContext _context;
            public UpdateMaterialCommandHandler(IApplicationDbContext context)
            {
                _context = context;
            }
            public async Task<Guid> Handle(UpdateMateialCommand request, CancellationToken cancellationToken)
            {

                // Valid
                if (request.BaseCostPerGram <= 0)            {
                    throw new ValidationException("Đơn giá không hợp lệ.");
                }

                var material = await _context.Materials.FindAsync(request.Id, cancellationToken);
                if (material == null)
                {
                    throw new Exception("Material not found");
                }
    
                material.Name = request.Name;
                material.Description = request.Description;
    
                var newMaterialPriceHistory = new Domain.Entities.MaterialPriceHistory
                {
                    Material = material,
                    BaseCostPerGram = request.BaseCostPerGram,
                    TotalServiceCostPerGram = request.TotalServiceCostPerGram,
                    EffectiveDate = request.EffectiveDate,
                    Created = DateTime.UtcNow
                };
    
                _context.MaterialPriceHistories.Add(newMaterialPriceHistory);
                await _context.SaveChangesAsync(cancellationToken);
                return material.Id;
            }
    }
}
