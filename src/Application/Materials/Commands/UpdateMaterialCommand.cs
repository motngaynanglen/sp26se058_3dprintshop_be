using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Materials.Queries;

namespace sp26se058_3dprintshop_be.Application.Materials.Commands;

public record UpdateMaterialCommand : IRequest<MaterialDTO>
{
    [JsonIgnore]
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; init; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    public decimal BaseCostPerGram { get; set; }
    public decimal TotalServiceCostPerGram { get; set; }
    public DateTime EffectiveDate { get; set; }

    public class UpdateMaterialCommandHandler : IRequestHandler<UpdateMaterialCommand, MaterialDTO>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;                    // ← Thêm IMapper

        public UpdateMaterialCommandHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<MaterialDTO> Handle(UpdateMaterialCommand request, CancellationToken cancellationToken)
        {
            // Validation
            if (request.BaseCostPerGram <= 0)
            {
                throw new ValidationException("Đơn giá không hợp lệ.");
            }

            var material = await _context.Materials
                .IgnoreQueryFilters()
                .Include(m => m.PriceHistories)           // ← Nên Include để mapping DTO sau này chính xác
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

            if (material == null)
            {
                throw new Exception("Material not found");   // Nên dùng NotFoundException nếu có
            }

            // Cập nhật thông tin Material
            material.Name = request.Name;
            material.Description = request.Description;
            // material.LastModified = DateTime.UtcNow;   // Nếu bạn có trường này thì nên cập nhật

            // Tạo bản ghi giá mới
            var newMaterialPriceHistory = new Domain.Entities.MaterialPriceHistory
            {
                Material = material,
                BaseCostPerGram = request.BaseCostPerGram,
                TotalServiceCostPerGram = request.TotalServiceCostPerGram,
                EffectiveDate = request.EffectiveDate,
                Created = DateTime.UtcNow,
                IsCurrent = true                     // ← Rất quan trọng nếu bạn đang dùng IsCurrent
            };

            _context.MaterialPriceHistories.Add(newMaterialPriceHistory);

            // Nếu entity MaterialPriceHistory có IsCurrent, nên tắt IsCurrent của các bản ghi cũ
            foreach (var history in material.PriceHistories)
            {
                if (history.IsCurrent)
                {
                    history.IsCurrent = false;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Map sang DTO và trả về
            var materialDto = _mapper.Map<MaterialDTO>(material);

            return materialDto;
        }
    }
}
