using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.ServiceOptions.Commands;
[Authorize(Roles = Roles.STAFF + "," + Roles.MANAGER)]
public record UpdateServiceOptionCommand : IRequest<object>
{
    [JsonIgnore]
    public Guid Id { get; init; }
    public string? Code { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? GroupCode { get; init; }
    public string? GroupName { get; init; }
    public string? SelectionType { get; init; }
    public decimal? DefaultPrice { get; init; }
    public int? MinQuantity { get; init; }
    public int? MaxQuantity { get; init; }
    public int? AdjustmentRoundDelta { get; init; }
    public int? SortOrder { get; init; }
    public bool? IsActive { get; init; }
    public bool ClearDescription { get; init; }
    public bool ClearMaxQuantity { get; init; }
    public bool ClearAdjustmentRoundDelta { get; init; }
}
public class UpdateServiceOptionCommandValidator : AbstractValidator<UpdateServiceOptionCommand>
{
    public UpdateServiceOptionCommandValidator()
    {
        RuleFor(v => v.Code)
            .MaximumLength(50)
            .When(v => !string.IsNullOrEmpty(v.Code));

        RuleFor(v => v.Name)
            .MaximumLength(100)
            .When(v => !string.IsNullOrEmpty(v.Name));

        RuleFor(v => v.Description)
            .MaximumLength(500)
            .When(v => v.Description != null);

        RuleFor(v => v)
            .Must(v => !(v.ClearDescription && v.Description != null))
            .WithMessage("Không thể vừa xóa mô tả vừa cập nhật mô tả mới.");

        RuleFor(v => v.GroupCode)
            .MaximumLength(50)
            .When(v => !string.IsNullOrEmpty(v.GroupCode));

        RuleFor(v => v.GroupName)
            .MaximumLength(100)
            .When(v => !string.IsNullOrEmpty(v.GroupName));

        RuleFor(v => v.SelectionType)
            .Must(ServiceOptionSelectionTypes.IsValid)
            .WithMessage($"Loại lựa chọn không hợp lệ. Chỉ chấp nhận: {string.Join(", ", ServiceOptionSelectionTypes.All.Select(x => x.Value))}")
            .When(v => !string.IsNullOrEmpty(v.SelectionType));

        RuleFor(v => v.DefaultPrice)
            .GreaterThanOrEqualTo(0)
            .When(v => v.DefaultPrice.HasValue)
            .WithMessage("Giá mặc định không được nhỏ hơn 0");

        RuleFor(v => v.MinQuantity)
            .GreaterThanOrEqualTo(1)
            .When(v => v.MinQuantity.HasValue)
            .WithMessage("Số lượng tối thiểu phải lớn hơn hoặc bằng 1");

        RuleFor(v => v.SortOrder)
            .GreaterThanOrEqualTo(0)
            .When(v => v.SortOrder.HasValue)
            .WithMessage("Thứ tự hiển thị không được nhỏ hơn 0");

        RuleFor(v => v.AdjustmentRoundDelta)
            .GreaterThanOrEqualTo(0)
            .When(v => v.AdjustmentRoundDelta.HasValue)
            .WithMessage("Số lượt hiệu chỉnh cộng thêm không được nhỏ hơn 0");

        RuleFor(v => v)
            .Must(v => !(v.ClearMaxQuantity && v.MaxQuantity.HasValue))
            .WithMessage("Không thể vừa xóa số lượng tối đa vừa cập nhật số lượng tối đa mới.");

        RuleFor(v => v)
            .Must(v => !(v.ClearAdjustmentRoundDelta && v.AdjustmentRoundDelta.HasValue))
            .WithMessage("Không thể vừa xóa số lượt hiệu chỉnh vừa cập nhật số lượt hiệu chỉnh mới.");
    }
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

        var normalizedCode = request.Code?.Trim().ToUpper();

        if (!string.IsNullOrEmpty(normalizedCode) && normalizedCode != entity.Code)
        {
            var checkResult = await _context.ServiceOptions.GetDuplicateResultAsync(
                x => x.Code == normalizedCode,
                nameof(ServiceOption),
                nameof(request.Code),
                normalizedCode,
                excludeId: request.Id,
                ct: ct);

            checkResult.ThrowIfDuplicate();

            entity.Code = normalizedCode;
        }

        // Kiểm tra trùng mã Code (Tránh lỗi Unique Index ở DB)

        entity.Name = string.IsNullOrWhiteSpace(request.Name) ? entity.Name : request.Name.Trim();
        entity.Description = request.ClearDescription ? null : request.Description ?? entity.Description;
        entity.GroupCode = string.IsNullOrWhiteSpace(request.GroupCode) ? entity.GroupCode : request.GroupCode.Trim().ToUpper();
        entity.GroupName = string.IsNullOrWhiteSpace(request.GroupName) ? entity.GroupName : request.GroupName.Trim();
        entity.SelectionType = string.IsNullOrWhiteSpace(request.SelectionType) ? entity.SelectionType : request.SelectionType.Trim().ToUpper();
        entity.DefaultPrice = request.DefaultPrice ?? entity.DefaultPrice;
        entity.MinQuantity = request.MinQuantity ?? entity.MinQuantity;
        entity.MaxQuantity = request.ClearMaxQuantity ? null : request.MaxQuantity ?? entity.MaxQuantity;
        entity.AdjustmentRoundDelta = request.ClearAdjustmentRoundDelta ? null : request.AdjustmentRoundDelta ?? entity.AdjustmentRoundDelta;
        entity.SortOrder = request.SortOrder ?? entity.SortOrder;
        entity.IsActive = request.IsActive ?? entity.IsActive;

        if (entity.MaxQuantity.HasValue && entity.MaxQuantity.Value < entity.MinQuantity)
        {
            throw new BusinessException("Số lượng tối đa phải lớn hơn hoặc bằng số lượng tối thiểu.");
        }

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
