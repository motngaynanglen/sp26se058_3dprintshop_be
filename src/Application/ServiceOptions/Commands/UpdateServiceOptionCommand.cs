using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.ServiceOptions.Commands;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record UpdateServiceOptionCommand : IRequest<object>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    public string? Code { get; init; }
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

        var entity = await _context.ServiceOptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (entity == null) throw new DataNotFoundException(nameof(ServiceOption), request.Id);

        if (!string.IsNullOrEmpty(request.Code) && request.Code != entity.Code)
        {
            var checkResult = await _context.ServiceOptions.GetDuplicateResultAsync(
                x => x.Code == request.Code,
                nameof(ServiceOption),
                nameof(request.Code),
                request.Code,
                excludeId: request.Id,
                ct: ct);

            checkResult.ThrowIfDuplicate();

            entity.Code = request.Code;
        }

        // Kiểm tra trùng mã Code (Tránh lỗi Unique Index ở DB)

        entity.Name = request.Name ?? entity.Name;
        entity.DefaultPrice = request.DefaultPrice ?? entity.DefaultPrice;
        entity.IsActive = request.IsActive ?? entity.IsActive;
        entity.LastModified = CoreHelper.SystemTimeNow;
        entity.LastModifiedBy = _user.Username;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new UpdateFailureException(nameof(ServiceOption), $"{ex.InnerException?.Message ?? ex.Message}");
        }
        return request;
    }
}
