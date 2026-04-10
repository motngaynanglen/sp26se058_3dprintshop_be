using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.PackageOptions.Queries;
using sp26se058_3dprintshop_be.Application.ServicePackages.Commands;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.PackageOptions.Commands;
public record CreatePackageOptionCommand : IRequest<PackageOptionDTO>
{
    public Guid ServicePackageId { get; set; }
    public Guid ServiceOptionId { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool DefaultSelected { get; set; } = false;
    public decimal? PriceOverride { get; set; } = null;
    public int MinQuantity { get; set; } = 0;
    public int MaxQuantity { get; set; } = 1;
}
public class CreatePackageOptionCommandHandler : IRequestHandler<CreatePackageOptionCommand, PackageOptionDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public CreatePackageOptionCommandHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }
    public async Task<PackageOptionDTO> Handle(CreatePackageOptionCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra cặp trùng lặp (Unique Constraint)
        var exists = await _context.PackageOptions
            .AnyAsync(po => po.ServicePackageId == request.ServicePackageId
                         && po.ServiceOptionId == request.ServiceOptionId);

        if (exists) throw new Exception("Option này đã tồn tại trong Package.");

        // 2. Map và Lưu
        var entity = new PackageOption
        {
            Id = Guid.NewGuid(),
            ServicePackageId = request.ServicePackageId,
            ServiceOptionId = request.ServiceOptionId,
            IsRequired = request.IsRequired,
            DefaultSelected = request.DefaultSelected,
            PriceOverride = request.PriceOverride,
            MinQuantity = request.MinQuantity,
            MaxQuantity = request.MaxQuantity,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };

        _context.PackageOptions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PackageOptionDTO>(entity);
    }
}
