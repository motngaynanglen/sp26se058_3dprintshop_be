using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;

public record CheckoutDesignWorkCommand : IRequest<OrderDTO>
{
    //[DefaultValue("00000000-0000-0000-0000-000000000001")]
    //public Guid ShippingAddressId { get; init; }

    [DefaultValue("00000000-0000-0000-0000-000000000001")]
    public Guid? DesignWorkId { get; init; }
    public string? NewDesignName { get; init; }
    public string? Description { get; init; }
    public string? BaseImageUrl { get; init; }

    [DefaultValue("Ghi chú yêu cầu thêm cho đơn thiết kế")]
    public string? Note { get; init; }

    // Danh sách các Option dịch vụ khách hàng chọn (Sơn, Phủ bóng, In cao cấp...)
    public List<Guid> ServiceOptionIds { get; init; } = new();
}

public class CheckoutDesignWorkCommandHandler : IRequestHandler<CheckoutDesignWorkCommand, OrderDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public CheckoutDesignWorkCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<OrderDTO> Handle(CheckoutDesignWorkCommand request, CancellationToken cancellationToken)
    {
        // 1. Xác thực người dùng và lấy thông tin Customer
        var currentUserIdStr = _user.Id;
        if (string.IsNullOrEmpty(currentUserIdStr)) throw new UnauthorizedAccessException();

        var userId = Guid.Parse(currentUserIdStr);

        // Lấy Customer dựa trên AccountId liên kết
        var customer = await _context.Customers
            .FirstOrDefaultAsync(x => x.AccountId == userId, cancellationToken);

        if (customer == null)
            throw new ForbiddenAccessException("Chỉ khách hàng mới có quyền thực hiện chức năng này.");

        DesignWork designWork;
        bool isNewWork = false;

        // 2. Xử lý DesignWork (Dùng lại hoặc tạo mới)
        if (request.DesignWorkId.HasValue && request.DesignWorkId != Guid.Empty)
        {
            var existingWork = await _context.DesignWorks
                .FirstOrDefaultAsync(dw => dw.Id == request.DesignWorkId && dw.CustomerId == customer.Id, cancellationToken);

            if (existingWork == null)
                throw new NotFoundException(nameof(DesignWork), request.DesignWorkId.Value.ToString());

            designWork = existingWork;
            designWork.Status = "InProgress"; // Cập nhật trạng thái sang đang xử lý
        }
        else
        {
            isNewWork = true;
            designWork = new DesignWork
            {
                Id = Guid.NewGuid(),
                Name = request.NewDesignName ?? "Yêu cầu thiết kế mới",
                CustomerId = customer.Id,
                BaseImageUrl = request.BaseImageUrl,
                Status = "InProgress",
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username ?? "System"
            };
            _context.DesignWorks.Add(designWork);
        }

        // 3. Tính toán giá từ các Service Options đã chọn
        var selectedOptions = await _context.ServiceOptions
            .Where(so => request.ServiceOptionIds.Contains(so.Id) && so.IsActive)
            .ToListAsync(cancellationToken);

        decimal totalServicePrice = selectedOptions.Sum(so => so.DefaultPrice);

        // 4. Khởi tạo Order (Gán CustomerId)
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = $"ORD-DSG-{DateTime.Now:yyyyMMddHHmmss}",
            CustomerId = customer.Id,
            OrderStatus = OrderStatuses.Pending,
            TotalPrice = totalServicePrice,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "System",
        };

        // 5. Lưu Snapshot các option đã chọn vào ServiceSelection
        var serviceSelection = new ServiceSelection
        {
            Id = Guid.NewGuid(),
            DesignWorkId = designWork.Id,
            TotalPrice = totalServicePrice,
            IsLocked = true,
            Note = request.Note,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "System"
        };

        foreach (var opt in selectedOptions)
        {
            _context.ServiceSelectedOptions.Add(new ServiceSelectedOption
            {
                Id = Guid.NewGuid(),
                ServiceSelectionId = serviceSelection.Id,
                ServiceOptionId = opt.Id,
                OptionNameSnapshot = opt.Name,
                AppliedPrice = opt.DefaultPrice,
                Quantity = 1
            });
        }

        // 6. Tạo Log trao đổi đầu tiên (Dùng chung cho cả Staff và Customer)
        if (isNewWork || !string.IsNullOrEmpty(request.Note) || !string.IsNullOrEmpty(request.BaseImageUrl))
        {
            var initialLog = new DesignLog
            {
                Id = Guid.NewGuid(),
                DesignWorkId = designWork.Id,
                AccountId = userId,
                Content = !string.IsNullOrEmpty(request.Note) ? request.Note : "Khởi tạo yêu cầu thiết kế từ đơn hàng.",
                LogType = "COMMUNICATION",
                IsAI = false,
                //IsRead = false,
                Metadata = !string.IsNullOrEmpty(request.BaseImageUrl)
                    ? JsonSerializer.Serialize(new List<string> { request.BaseImageUrl })
                    : null,
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username ?? "System"
            };
            _context.DesignLogs.Add(initialLog);
        }

        // 7. Tạo OrderItem và các thực thể liên quan (Sửa lỗi CS9035 bằng cách gán object Order)
        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order, // Bắt buộc nếu là required member
            SourceType = SourceTypes.DesignService,
            DesignWorkId = designWork.Id,
            ServiceSelectionId = serviceSelection.Id,
            ItemName = $"Dịch vụ thiết kế: {designWork.Name}",
            QuantityOrdered = 1,
            UnitPrice = totalServicePrice,
            TotalPrice = totalServicePrice,
            FulfillmentStatus = OrderItemStatuses.Designing,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "System"
        };

        //var shipment = new Shipment
        //{
        //    Id = Guid.NewGuid(),
        //    OrderId = order.Id,
        //    Order = order,
        //    ShippingAddressId = request.ShippingAddressId,
        //    ShipmentStatus = ShipmentStatuses.Preparing,
        //    Created = CoreHelper.SystemTimeNow,
        //    CreatedBy = _user.Username ?? "System"
        //};

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            InvoiceCode = $"INV-DSG-{DateTime.UtcNow.Ticks}",
            TotalAmount = order.TotalPrice,
            PaymentStatus = InvoiceStatuses.Unpaid,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "System"
        };

        // 8. Lưu vào Database
        _context.ServiceSelections.Add(serviceSelection);
        _context.Orders.Add(order);
        _context.OrderItems.Add(orderItem);
        //_context.Shipments.Add(shipment);
        _context.Invoices.Add(invoice);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Log lỗi chi tiết ở đây nếu cần
            throw new CreateFailureException("CheckoutDesign", ex.Message);
        }

        return _mapper.Map<OrderDTO>(order);
    }
}
