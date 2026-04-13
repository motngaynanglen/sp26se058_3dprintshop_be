using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.ServiceOptions.Commands;
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

    public CreateServiceOptionCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<object> Handle(CreateServiceOptionCommand request, CancellationToken ct)
    {
        var failures = new List<ValidationFailure>();

        // Kiểm tra trùng mã Code (Tránh lỗi Unique Index ở DB)
        var exists = await _context.ServiceOptions
            .AnyAsync(x => x.Code == request.Code, ct);

        if (exists)
        {
            failures.AddFailure(nameof(ServiceOption.Code), $"Mã tùy chọn '{request.Code}' đã tồn tại trong hệ thống.");
        }
        failures.ThrowIfAny();
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
        await _context.SaveChangesAsync(ct);

        return request;
    }
}
