using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.ServiceOptions.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.ServiceOptions.Commands;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]

public record CreateServiceOptionCommand : IRequest<object>
{
    [DefaultValue("CODE_A")]
    public string Code { get; init; } = null!;
    [DefaultValue("Thiết kế quy chuẩn")]
    public string Name { get; init; } = null!;
    [DefaultValue(100)]
    public decimal DefaultPrice { get; init; }
}
public class CreateServiceOptionCommandValidator : AbstractValidator<CreateServiceOptionCommand>
{
    public CreateServiceOptionCommandValidator()
    {
        RuleFor(v => v.Code)
            .NotEmpty().WithMessage("Mã tùy chọn không được để trống")
            .MaximumLength(50);

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Tên tùy chọn không được để trống")
            .MaximumLength(100);

        RuleFor(v => v.DefaultPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Giá mặc định không được nhỏ hơn 0");

    }
}
public class CreateServiceOptionCommandHandler : IRequestHandler<CreateServiceOptionCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public CreateServiceOptionCommandHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public async Task<object> Handle(CreateServiceOptionCommand request, CancellationToken ct)
    {
        var checkResult = await _context.ServiceOptions.GetDuplicateResultAsync(
                 x => x.Code == request.Code,
                 nameof(ServiceOption),
                 nameof(request.Code),
                 request.Code,
                 ct: ct);
        checkResult.ThrowIfDuplicate();

        var entity = new ServiceOption
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            DefaultPrice = request.DefaultPrice,
            IsActive = true,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
        };

        _context.ServiceOptions.Add(entity);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new CreateFailureException(nameof(ServiceOption), $"{ex.InnerException?.Message ?? ex.Message}");
        }

        return _mapper.Map<ServiceOptionDTO>(entity);
    }
}
