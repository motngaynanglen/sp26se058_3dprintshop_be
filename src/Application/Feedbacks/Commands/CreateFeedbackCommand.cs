using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayOS.Exceptions;
using sp26se058_3dprintshop_be.Application.Feedbacks.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Commands;
[Authorize(Roles = Roles.CUSTOMER)]
public record CreateFeedbackCommand : IRequest<FeedbackDTO>
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid OrderItemId { get; init; }
    [DefaultValue(5)]
    public int Rating { get; init; }
    [DefaultValue("Sản phẩm ổn đấy chứ!!!")]
    public string? Comment { get; init; }
    public List<string>? ImageUrls { get; init; } = ["image1URL", "image2URL", "image3URL"];
}
public class CreateFeedbackCommandValidator : AbstractValidator<CreateFeedbackCommand>
{
    public CreateFeedbackCommandValidator()
    {
        RuleFor(v => v.OrderItemId).NotEmpty();

        RuleFor(v => v.Rating)
            .InclusiveBetween(1, 5).WithMessage("Đánh giá phải từ 1 đến 5 sao.");

        RuleFor(v => v.Comment)
            .MaximumLength(1000).WithMessage("Nội dung đánh giá không quá 1000 ký tự.");

        RuleFor(v => v.ImageUrls)
            .Must(list => list == null || list.Count <= 5)
            .WithMessage("Bạn chỉ có thể tải lên tối đa 5 hình ảnh.");
    }
}
public class CreateFeedbackCommandHandler : IRequestHandler<CreateFeedbackCommand, FeedbackDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMapper _mapper;

    public CreateFeedbackCommandHandler(IApplicationDbContext context, IUser user, IMapper mapper)
    {
        _context = context;
        _user = user;
        _mapper = mapper;
    }

    public async Task<FeedbackDTO> Handle(CreateFeedbackCommand request, CancellationToken ct)
    {
        Guid userId = _user.Id.ToGuid();
        // 1. Lấy OrderItem và kiểm tra quyền sở hữu + Trạng thái đơn hàng
        var item = await _context.OrderItems
            .Include(oi => oi.Order).ThenInclude(o=>o.Customer)
            .Include(oi => oi.DesignVariant)
            .FirstOrDefaultAsync(oi => oi.Id == request.OrderItemId, ct);

        if (item == null)
            throw new DataNotFoundException(nameof(OrderItem), request.OrderItemId);
        if (item.Order.Customer.AccountId != userId)
            throw new ForbiddenAccessException("Bạn không có quyền đánh giá sản phẩm này.");

        if (item.Order.OrderStatus != OrderStatuses.Completed)
            throw new BusinessException("Bạn chỉ có thể đánh giá sau khi nhận hàng.");

        // Kiểm tra xem đã đánh giá chưa (Tránh spam)
        if (await _context.Feedbacks.AnyAsync(f => f.OrderItemId == request.OrderItemId, ct))
            throw new BusinessException("Sản phẩm này đã được bạn đánh giá rồi.");

        // 2. Xác định DesignTemplateId (Có thể null nếu là hàng Custom)
        if (item.DesignVariant == null)
            throw new BusinessException("Hiện tại hệ thống chỉ hỗ trợ đánh giá sản phẩm theo mẫu có sẵn.");

        var entity = new Feedback
        {
            CustomerId = item.Order.CustomerId,
            OrderItemId = request.OrderItemId,
            DesignTemplateId = item.DesignVariant.DesignTemplateId,
            Rating = request.Rating,
            Comment = request.Comment,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified= CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username,
            FeedbackImages = request.ImageUrls?.Select(url => new FeedbackImage
            {
                ImageUrl = url

            }).ToList() ?? new()
        };

        _context.Feedbacks.Add(entity);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            throw new CreateFailureException(nameof(Feedback), $"{ex.InnerException?.Message ?? ex.Message}");
        }

        return _mapper.Map<FeedbackDTO>(entity);
    }
}
