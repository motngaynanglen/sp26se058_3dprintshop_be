using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Orders;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Feedbacks.Commands;
public record CreateFeedbackCommand : IRequest<Guid>
{
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
        if (request.Rating is < 1 or > 5)
            throw new Exception("Điểm đánh giá phải từ 1 đến 5 sao.");

        if (request.OrderItemId == Guid.Empty)
            throw new Exception("Thiếu mã sản phẩm trong đơn (orderItemId).");

        var customerId = await FeedbackCustomerHelper.GetCurrentCustomerIdAsync(_context, _user, ct);
        var item = await _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.DesignVariant)
            .Include(oi => oi.DesignWork)
            .FirstOrDefaultAsync(oi => oi.Id == request.OrderItemId && oi.Order.CustomerId == customerId, ct);

        if (item == null) throw new Exception("Sản phẩm không tồn tại hoặc không thuộc quyền sở hữu của bạn.");

        var shipmentStatus = await _context.Shipments
            .AsNoTracking()
            .Where(s => s.OrderId == item.OrderId)
            .OrderByDescending(s => s.Created)
            .Select(s => s.ShipmentStatus)
            .FirstOrDefaultAsync(ct);

        if (!OrderFeedbackRules.IsOrderReviewable(
                item.Order.OrderStatus, shipmentStatus, item.Order.CompletedAt))
            throw new Exception("Bạn chỉ có thể đánh giá khi đơn hàng đã hoàn thành.");

        if (await _context.Feedbacks.AnyAsync(f => f.OrderItemId == request.OrderItemId, ct))
            throw new Exception("Sản phẩm này đã được đánh giá trước đó.");

        var templateId = await OrderFeedbackRules.ResolveDesignTemplateIdAsync(_context, item, ct);

        var imageUrls = (request.ImageUrls ?? [])
            .Where(url => !string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out _))
            .Take(5)
            .ToList();

        var entity = new Feedback
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            OrderItemId = request.OrderItemId,
            DesignTemplateId = templateId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            FeedbackImages = imageUrls.Select(url => new FeedbackImage
            {
                Id = Guid.NewGuid(),
                ImageUrl = url
            }).ToList()
        };

        _context.Feedbacks.Add(entity);
        await _context.SaveChangesAsync(ct);

        return entity.Id;
    }
}
