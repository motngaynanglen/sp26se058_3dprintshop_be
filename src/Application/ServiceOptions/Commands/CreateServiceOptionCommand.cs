using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.ServiceOptions.Commands;
public record CreateServiceOptionCommand : IRequest<object>
{
    [DefaultValue("CodeAutoGenSau")]
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    [DefaultValue("ADDON Hoặc CONFIG")]
    public string OptionType { get; init; } = null!; // ADDON | CONFIG
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

        RuleFor(v => v.OptionType)
            .Must(x => x == "ADDON" || x == "CONFIG")
            .WithMessage("Loại tùy chọn phải là ADDON hoặc CONFIG");
    }
}
public class CreateServiceOptionCommandHandler : IRequestHandler<CreateServiceOptionCommand, object>
{
    private readonly IApplicationDbContext _context;

    public CreateServiceOptionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<object> Handle(CreateServiceOptionCommand request, CancellationToken ct)
    {
        // Kiểm tra trùng mã Code (Tránh lỗi Unique Index ở DB)
        var exists = await _context.ServiceOptions
            .AnyAsync(x => x.Code == request.Code, ct);

        if (exists)
        {
            throw new Exception($"Mã tùy chọn '{request.Code}' đã tồn tại trong hệ thống.");
        }

        var entity = new ServiceOption
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            OptionType = request.OptionType,
            DefaultPrice = request.DefaultPrice,
            IsActive = true
        };

        _context.ServiceOptions.Add(entity);
        await _context.SaveChangesAsync(ct);

        return request;
    }
}
