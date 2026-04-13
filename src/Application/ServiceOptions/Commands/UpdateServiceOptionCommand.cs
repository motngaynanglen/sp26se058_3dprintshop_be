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
        /*var failures = new List<ValidationFailure>();
        // Kiểm tra trùng mã Code (Tránh lỗi Unique Index ở DB)
        var exists = await _context.ServiceOptions
            .AnyAsync(x => x.Code == request.Code, ct);

        if (exists)
        {
            failures.AddFailure(nameof(ServiceOption.Code), $"Mã tùy chọn '{request.Code}' đã tồn tại trong hệ thống.");
        }
        failures.ThrowIfAny();*/
        var entity = await _context.ServiceOptions.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == request.Id);

        if (entity == null) throw new DataNotFoundException(nameof(ServiceOption), request.Id);
        if (entity.Deleted != null) throw new DuplicateException(nameof(ServiceOption.Code)+" trùng lặp với dữ liệu đang trong thùng rác.");

        entity.Name = request.Name ?? entity.Name;
        entity.DefaultPrice = request.DefaultPrice ?? entity.DefaultPrice;
        entity.IsActive = request.IsActive ?? entity.IsActive;
        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;

        await _context.SaveChangesAsync(ct);
        return request;
    }
}
