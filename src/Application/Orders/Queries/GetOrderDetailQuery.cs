using System.ComponentModel;
using System.Text.Json;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.Orders.Queries;

[Authorize(Roles = Roles.MANAGER + "," + Roles.STAFF + "," + Roles.CUSTOMER)]
public record GetOrderDetailQuery : IRequest<OrderDTO>
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; set; }
}

public class GetOrderDetailQueryHandler : IRequestHandler<GetOrderDetailQuery, OrderDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public GetOrderDetailQueryHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<OrderDTO> Handle(GetOrderDetailQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Orders
            .Include(o => o.Customer).ThenInclude(c => c.Account)
            .Include(o => o.Invoice).ThenInclude(i => i!.Transactions)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.DesignVariant).ThenInclude(dv => dv!.DesignTemplate)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.DesignVariant).ThenInclude(dv => dv!.Material)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Feedbacks)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (entity == null)
            throw new DataNotFoundException(nameof(Order), request.Id);

        if (_user.Role == Roles.CUSTOMER)
        {
            var userId = _user.Id.ToGuid();
            if (entity.Customer.AccountId != userId)
                throw new ForbiddenAccessException("Bạn không có quyền xem chi tiết đơn hàng này.");
        }

        var dto = _mapper.Map<OrderDTO>(entity);

        // Enrich Invoice summary
        if (entity.Invoice != null)
        {
            var activeTx = entity.Invoice.Transactions
                ?.Where(t => t.TransactionStatus is not ("FAILED" or "CANCELLED"))
                .OrderByDescending(t => t.Created)
                .FirstOrDefault();

            dto.Invoice = new OrderInvoiceSummaryDTO
            {
                Id = entity.Invoice.Id,
                InvoiceCode = entity.Invoice.InvoiceCode,
                PaymentStatus = entity.Invoice.PaymentStatus,
                TotalAmount = entity.Invoice.TotalAmount,
                DueDate = entity.Invoice.DueDate,
                PaymentMethod = activeTx?.PaymentMethod,
            };
        }

        // Enrich Shipment summary
        var shipment = await _context.Shipments
            .AsNoTracking()
            .Include(s => s.ShippingAddress)
            .FirstOrDefaultAsync(s => s.OrderId == entity.Id, cancellationToken);

        if (shipment != null)
        {
            dto.Shipment = new OrderShipmentSummaryDTO
            {
                Id = shipment.Id,
                ShipmentStatus = shipment.ShipmentStatus,
                CarrierName = shipment.CarrierName,
                Carrier = shipment.Carrier,
                CarrierOrderCode = shipment.CarrierOrderCode,
                TrackingNumber = shipment.TrackingNumber,
                ShippingFee = shipment.ShippingFee,
                EstimatedDeliveryTime = shipment.EstimatedDeliveryTime,
                ShippedAt = shipment.ShippedAt,
                DeliveredAt = shipment.DeliveredAt,
            };

            // Build full address string
            if (shipment.ShippingAddress != null)
            {
                var sa = shipment.ShippingAddress;
                dto.ShippingAddress = string.Join(", ",
                    new[] { sa.AddressLine, sa.Ward, sa.District, sa.City, sa.Province }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                dto.Shipment.FullAddress = dto.ShippingAddress;
            }
            else
            {
                // Fallback to snapshot fields on shipment
                dto.ShippingAddress = string.Join(", ",
                    new[] { shipment.AddressLine, shipment.Ward, shipment.District, shipment.City, shipment.Province }
                        .Where(s => !string.IsNullOrWhiteSpace(s) && s != "N/A"));
            }
        }

        // Enrich items with feedback
        var isCompleted = entity.OrderStatus == OrderStatuses.Completed;
        foreach (var itemDto in dto.Items)
        {
            var itemEntity = entity.OrderItems.FirstOrDefault(oi => oi.Id == itemDto.Id);
            if (itemEntity == null) continue;

            // Feedback
            var fb = itemEntity.Feedbacks?.FirstOrDefault(f => !f.IsHidden);
            if (fb != null)
            {
                itemDto.Feedback = new OrderItemFeedbackDto
                {
                    Id = fb.Id,
                    Rating = fb.Rating,
                    Comment = fb.Comment,
                    StaffReply = fb.StaffReply,
                    Created = fb.Created,
                    ImageUrls = fb.FeedbackImages?.Select(fi => fi.ImageUrl).ToList() ?? new(),
                };
            }

            itemDto.CanSubmitFeedback = isCompleted && fb == null && itemDto.DesignVariantId.HasValue;
        }

        return dto;
    }
}
