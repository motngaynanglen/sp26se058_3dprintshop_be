using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Materials.Queries;

namespace sp26se058_3dprintshop_be.Application.Materials.Commands;

public record CreateMaterialCommand : IRequest<MaterialDTO>
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    public decimal BaseCostPerGram { get; set; }
    public decimal TotalServiceCostPerGram { get; set; }
    public DateTime EffectiveDate { get; set; }

    public class CreateMaterialCommandHandler : IRequestHandler<CreateMaterialCommand, MaterialDTO>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;   // ← thêm IMapper

        public CreateMaterialCommandHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<MaterialDTO> Handle(CreateMaterialCommand request, CancellationToken cancellationToken)
        {
            // Validation
            if (request.BaseCostPerGram <= 0)
            {
                throw new ValidationException("Đơn giá không hợp lệ.");
            }

            var newMaterial = new Domain.Entities.Material
            {
                Name = request.Name,
                Description = request.Description,
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
                Created = DateTime.UtcNow,
                // Nếu entity MaterialPriceHistory có trường IsCurrent thì set = true
                IsCurrent = true
            };

            _context.MaterialPriceHistories.Add(newMaterialPriceHistory);

            await _context.SaveChangesAsync(cancellationToken);

            // Map sang DTO (sẽ dùng mapping bạn đã định nghĩa)
            var materialDto = _mapper.Map<MaterialDTO>(newMaterial);

            return materialDto;
        }
    }
}
