using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.ServiceOptions.Commands;
public record UpdateServiceOptionCommand : IRequest<object>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? OptionType { get; init; }
    public decimal? DefaultPrice { get; init; }
    public bool? IsActive { get; init; }
}
public class UpdateServiceOptionCommandHandler : IRequestHandler<UpdateServiceOptionCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateServiceOptionCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<object> Handle(UpdateServiceOptionCommand request, CancellationToken ct)
    {
        var entity = await _context.ServiceOptions.Include(s => s.PackageOptions).FirstOrDefaultAsync(s => s.Id == request.Id);

        if (entity == null) throw new Exception("Không tìm thấy tùy chọn dịch vụ.");
        if (entity.PackageOptions.Any() && request.OptionType != null)
        {
            throw new Exception("Không thể cập nhật OptionType vì đã được thêm vào service khác.");
        }
        entity.Name = request.Name ?? entity.Name;
        entity.OptionType = request.OptionType ?? entity.OptionType;
        entity.DefaultPrice = request.DefaultPrice ?? entity.DefaultPrice;
        entity.IsActive = request.IsActive ?? entity.IsActive;
        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(ct);
        return request;
    }
}
