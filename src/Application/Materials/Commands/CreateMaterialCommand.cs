using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Application.Materials.Commands;

public record CreateMaterialCommand : IRequest<Guid>
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    public decimal BaseCostPerGram { get; set; }
    public decimal TotalServiceCostPerGram { get; set; }
    public DateTime EffectiveDate { get; set; }

    public class CreateMaterialCommandHandler : IRequestHandler<CreateMaterialCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        public CreateMaterialCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<Guid> Handle(CreateMaterialCommand request, CancellationToken cancellationToken)
        {
            // Valid
            if (request.BaseCostPerGram <= 0)            {
                throw new ValidationException("Đơn giá không hợp lệ.");
            }

            var newMaterial = new Domain.Entities.Material
            {
                Name = request.Name,
                Description = request.Description,
                //BaseCostPerGram = request.BaseCostPerGram,
                //TotalServiceCostPerGram = request.TotalServiceCostPerGram,
                //EffectiveDate = request.EffectiveDate,
                IsActive = true,
                Created = DateTime.UtcNow
            };
            _context.Materials.Add(newMaterial);

            var newMaterialPriceHistory = new Domain.Entities.MaterialPriceHistory
            {
                Material = newMaterial,
                BaseCostPerGram = request.BaseCostPerGram,
                TotalServiceCostPerGram = request.TotalServiceCostPerGram,
                EffectiveDate = request.EffectiveDate,
                Created = DateTime.UtcNow
            };

            _context.MaterialPriceHistories.Add(newMaterialPriceHistory);
            await _context.SaveChangesAsync(cancellationToken);
            return newMaterial.Id;
        }
    }
}
