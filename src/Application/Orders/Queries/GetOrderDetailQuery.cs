using Microsoft.Extensions.Options;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Feedbacks;
using sp26se058_3dprintshop_be.Application.Mainflow2;
using sp26se058_3dprintshop_be.Application.Orders;

namespace sp26se058_3dprintshop_be.Application.Orders.Queries;

public record GetOrderDetailQuery : IRequest<OrderDTO>
{
    public Guid Id { get; init; }
}

public class GetOrderDetailQueryHandler : IRequestHandler<GetOrderDetailQuery, OrderDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;
    private readonly PaymentOptions _paymentOptions;

    public GetOrderDetailQueryHandler(
        IApplicationDbContext context,
        IMapper mapper,
        IUser user,
        IOptions<PaymentOptions> paymentOptions)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
        _paymentOptions = paymentOptions.Value;
    }

    public async Task<OrderDTO> Handle(GetOrderDetailQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer).ThenInclude(c => c.Account)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.DesignVariant!).ThenInclude(dv => dv.DesignTemplate)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.DesignWork)
            .Include(o => o.Invoice!).ThenInclude(i => i.Transactions)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        var dto = _mapper.Map<OrderDTO>(order);

        var shipment = await _context.Shipments
            .AsNoTracking()
            .Include(s => s.ShippingAddress)
            .Where(s => s.OrderId == order.Id)
            .OrderByDescending(s => s.Created)
            .FirstOrDefaultAsync(cancellationToken);
        if (shipment != null)
            dto.Shipment = _mapper.Map<OrderShipmentSummaryDTO>(shipment);

        if (order.Invoice != null)
        {
            dto.Invoice = _mapper.Map<OrderInvoiceSummaryDTO>(order.Invoice);
            dto.Invoice.PaymentMethod = OrderPaymentHelper.ResolvePaymentMethod(order.Invoice);
            dto.Invoice.IsCod = OrderPaymentHelper.IsCodOrder(order.Invoice);

            if (OrderPaymentHelper.HasCustomProductionItems(order))
            {
                var designFee = await Mainflow2QuoteFeeHelper.ResolveDesignFeeForOrderAsync(
                    _context, order, cancellationToken);
                dto.Invoice.DesignFeeAmount = designFee;
                dto.Invoice.CustomDepositAmount = designFee > 0
                    ? OrderPaymentHelper.GetDepositAmount(designFee, _paymentOptions.CustomOrderDepositPercent)
                    : OrderPaymentHelper.GetDepositAmount(
                        order.Invoice.TotalAmount, _paymentOptions.CustomOrderDepositPercent);
                dto.Invoice.RemainingBalance = OrderPaymentHelper.GetRemainingBalance(order.Invoice);
            }
        }

        var itemIds = order.OrderItems.Select(i => i.Id).ToList();
        var feedbackByItem = await _context.Feedbacks
            .AsNoTracking()
            .Include(f => f.FeedbackImages)
            .Where(f => itemIds.Contains(f.OrderItemId))
            .ToDictionaryAsync(f => f.OrderItemId, cancellationToken);

        var shipmentStatus = shipment?.ShipmentStatus;
        var reviewable = OrderFeedbackRules.IsOrderReviewable(
            order.OrderStatus, shipmentStatus, order.CompletedAt);

        var isOwner = false;
        if (!string.IsNullOrWhiteSpace(_user.Id))
        {
            try
            {
                var customerId = await FeedbackCustomerHelper.GetCurrentCustomerIdAsync(
                    _context, _user, cancellationToken);
                isOwner = order.CustomerId == customerId;
            }
            catch (UnauthorizedAccessException)
            {
                isOwner = false;
            }
        }

        foreach (var itemDto in dto.OrderItems)
        {
            if (feedbackByItem.TryGetValue(itemDto.Id, out var fb))
            {
                itemDto.Feedback = new OrderItemFeedbackDto
                {
                    Id = fb.Id,
                    Rating = fb.Rating,
                    Comment = fb.Comment,
                    StaffReply = fb.StaffReply,
                    Created = fb.Created,
                    ImageUrls = fb.FeedbackImages.Select(x => x.ImageUrl).ToList(),
                };
            }

            itemDto.CanSubmitFeedback = isOwner && reviewable && itemDto.Feedback == null;
        }

        return dto;
    }
}
