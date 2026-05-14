using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Materials.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Materials.Commands;

[Authorize(Roles = Roles.StaffOrManager)]
public record UpdateMaterialCommand : IRequest<MaterialDTO>
{
    [JsonIgnore]
    public Guid Id { get; init; }

    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public bool IsActive { get; set; }
    public decimal BaseCostPerGram { get; set; }
    public decimal TotalServiceCostPerGram { get; set; }
    public DateTime EffectiveDate { get; set; }

    public class UpdateMaterialCommandHandler : IRequestHandler<UpdateMaterialCommand, MaterialDTO>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUser _user;

        public UpdateMaterialCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
        {
            _context = context;
            _mapper = mapper;
            _user = user;
        }

        public async Task<MaterialDTO> Handle(UpdateMaterialCommand request, CancellationToken cancellationToken)
        {
            // Validation
            if (request.BaseCostPerGram <= 0)
            {
                throw new Exception("Đơn giá (Base Cost) phải lớn hơn 0.");
            }

            if (request.TotalServiceCostPerGram < request.BaseCostPerGram)
            {
                throw new Exception("Phí dịch vụ (Total Service Cost) phải cao hơn đơn giá (Base Cost).");
            }

            var material = await _context.Materials
                .Include(m => m.PriceHistories)
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

            if (material == null)
            {
                throw new Exception("Material not found");
            }

            // Cập nhật thông tin cơ bản của Material
            material.Name = request.Name;
            material.Description = request.Description;
            material.IsActive = request.IsActive;
            material.LastModified = DateTime.UtcNow;
            material.LastModifiedBy = _user.Username;

            // TẠO MỚI PriceHistory nhưng KHÔNG thay đổi IsCurrent
            var newPriceHistory = new Domain.Entities.MaterialPriceHistory
            {
                Material = material,                    // hoặc MaterialId = material.Id
                BaseCostPerGram = request.BaseCostPerGram,
                TotalServiceCostPerGram = request.TotalServiceCostPerGram,
                EffectiveDate = request.EffectiveDate,

                // Không set IsCurrent = true
                // Mặc định để false hoặc để giá trị mặc định của entity
                IsCurrent = false,                      // ← Quan trọng: không set thành true

                Created = DateTime.UtcNow,
                CreatedBy = _user.Username
            };

            _context.MaterialPriceHistories.Add(newPriceHistory);

            // KHÔNG tắt IsCurrent của các bản ghi cũ

            await _context.SaveChangesAsync(cancellationToken);

            // Trả về DTO
            var materialDto = _mapper.Map<MaterialDTO>(material);

            return materialDto;
        }
    }
}
