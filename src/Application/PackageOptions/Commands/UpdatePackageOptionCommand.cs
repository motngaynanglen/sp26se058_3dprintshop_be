using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.PackageOptions.Queries;
using sp26se058_3dprintshop_be.Application.ServicePackages.Commands;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.PackageOptions.Commands;
public record UpdatePackageOptionCommand : IRequest<PackageOptionDTO>
{
    [JsonIgnore]
    public Guid Id { get; set; }
    public bool? IsRequired { get; set; }
    public bool? DefaultSelected { get; set; }
    public decimal? PriceOverride { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
}
public class UpdatePackageOptionCommandHandler : IRequestHandler<UpdatePackageOptionCommand, PackageOptionDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public UpdatePackageOptionCommandHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }
    public async Task<PackageOptionDTO> Handle(UpdatePackageOptionCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra cặp trùng lặp (Unique Constraint)
        var entity = await _context.PackageOptions.FindAsync(request.Id, cancellationToken);

        if (entity == null)
        {
            throw new Exception("Option này đã tồn tại trong Package.");
        }

        entity.IsRequired = request.IsRequired ?? entity.IsRequired;
        entity.DefaultSelected = request.DefaultSelected ?? entity.DefaultSelected;
        entity.PriceOverride = request.PriceOverride ?? entity.PriceOverride;
        entity.MinQuantity = request.MinQuantity ?? entity.MinQuantity;
        entity.MaxQuantity = request.MaxQuantity ?? entity.MaxQuantity;

        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PackageOptionDTO>(entity);
    }
}
