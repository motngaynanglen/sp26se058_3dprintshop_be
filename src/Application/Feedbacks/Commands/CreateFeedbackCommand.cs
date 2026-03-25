using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Commands;
public record CreateFeedbackCommand : IRequest<Guid>
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid OrderItemId { get; init; }
    [DefaultValue(5)]
    public int Rating { get; init; }
    [DefaultValue("Sản phẩm ổn đấy chứ!!!")]
    public string? Comment { get; init; }
    public List<string>? ImageUrls { get; init; } = ["image1URL","image2URL","image3URL"]; 
}
public class CreateFeedbackCommandHandler : IRequestHandler<CreateFeedbackCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateFeedbackCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Guid> Handle(CreateFeedbackCommand request, CancellationToken ct)
    {
        Guid userId = _user.Id.ToGuid();
        // 1. Lấy OrderItem và kiểm tra quyền sở hữu + Trạng thái đơn hàng
        var item = await _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.DesignVariant)
            .FirstOrDefaultAsync(oi => oi.Id == request.OrderItemId && oi.Order.CustomerId == userId, ct);

        if (item == null) throw new Exception("Sản phẩm không tồn tại hoặc không thuộc quyền sở hữu của bạn.");
        if (item.Order.OrderStatus != "COMPLETED") throw new Exception("Bạn chỉ có thể đánh giá sản phẩm sau khi đơn hàng hoàn tất.");

        // Kiểm tra xem đã đánh giá chưa (Tránh spam)
        if (await _context.Feedbacks.AnyAsync(f => f.OrderItemId == request.OrderItemId, ct))
            throw new Exception("Sản phẩm này đã được đánh giá trước đó.");

        // 2. Xác định DesignTemplateId (Có thể null nếu là hàng Custom)
        if (item.DesignVariant == null) { 
            throw new Exception("Chỉ hỗ trợ Feedback sản phẩm mua trực tiếp");
        }
        Guid templateId = item.DesignVariant.DesignTemplateId;

        // 3. Tạo Entity Feedback
        var entity = new Feedback
        {
            Id = Guid.NewGuid(),
            OrderItemId = request.OrderItemId,
            DesignTemplateId = templateId,
            Rating = request.Rating,
            Comment = request.Comment,
            // Map danh sách ảnh từ chuỗi URL
            FeedbackImages = request.ImageUrls?.Select(url => new FeedbackImage
            {
                Id = Guid.NewGuid(),
                ImageUrl = url
            }).ToList() ?? new()
        };

        _context.Feedbacks.Add(entity);
        await _context.SaveChangesAsync(ct);

        return entity.Id;
    }
}
