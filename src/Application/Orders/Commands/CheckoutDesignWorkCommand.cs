using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PayOS.Exceptions;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;
[Authorize(Roles =Roles.CUSTOMER)]

public record CheckoutDesignWorkCommand : IRequest<OrderDTO>
{
    //[DefaultValue("00000000-0000-0000-0000-000000000001")]
    //public Guid ShippingAddressId { get; init; }

    [DefaultValue(null)]
    public Guid? DesignWorkId { get; init; }
    [DefaultValue(null)]
    public Guid? SourceLogId { get; init; }
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
        var userId = _user.Id.ToGuid();
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();

        // Lấy Customer dựa trên AccountId liên kết
        var customer = await _context.Customers.FirstOrDefaultAsync(x => x.AccountId == userId);
        if (customer == null) throw new ForbiddenAccessException();


        DesignWork designWork;
        Guid newWorkId = Guid.NewGuid();


        // --- XỬ LÝ 3 TRƯỜNG HỢP DESIGN WORK ---

        // TRƯỜNG HỢP 1 & 2: Có gửi DesignWorkId
        if (request.DesignWorkId.HasValue && request.DesignWorkId != Guid.Empty)
        {
            // Lấy thông tin Work cũ và chỉ đếm các Log do AI tạo ra
            var existingWork = await _context.DesignWorks
            .Include(dw => dw.DesignLogs)
            .FirstOrDefaultAsync(dw => dw.Id == request.DesignWorkId && dw.CustomerId == customer.Id);

            if (existingWork == null) throw new DataNotFoundException(nameof(DesignWork), request.DesignWorkId);

            int aiLogCount = existingWork.DesignLogs.Count(l => l.IsAI);

            // CASE 1: Dùng tiếp bản gốc (SourceLogId = null)
            if (!request.SourceLogId.HasValue || request.SourceLogId == Guid.Empty)
            {
                if (aiLogCount >= 2)
                {
                    throw new BusinessException("Phát hiện nhiều phiên bản AI. Vui lòng chọn một phiên bản (log) cụ thể để tiếp tục.");
                }

                // Lấy luôn bản gốc
                designWork = existingWork;
                designWork.Status = DesignWorkStatus.Pending;
                _context.DesignWorks.Update(designWork);
            }
            // CASE 2: Tạo nhánh (SourceLogId có giá trị)
            else
            {
                var sourceLog = existingWork.DesignLogs.FirstOrDefault(l => l.Id == request.SourceLogId);
                if (sourceLog == null) throw new DataNotFoundException(nameof(DesignLog), request.SourceLogId);

                designWork = new DesignWork
                {
                    Id = newWorkId,
                    Name = request.NewDesignName ?? $"Nhánh mới từ {existingWork.Name}",
                    RootDesignWorkId = existingWork.RootDesignWorkId,
                    ParentDesignWorkId = existingWork.Id,
                    RelationshipType = DesignRelationshipType.Branch,
                    CustomerId = customer.Id,
                    BaseImageUrl = existingWork.BaseImageUrl,
                    Status = DesignWorkStatus.Pending,
                    Created = CoreHelper.SystemTimeNow,
                    CreatedBy = _user.Username ?? "SYSTEM",
                    LastModified = CoreHelper.SystemTimeNow,
                    LastModifiedBy = _user.Username ?? "SYSTEM"
                };

                // SYSTEM LOG: Thông báo tạo nhánh
                var branchInfoLog = new DesignLog
                {
                    Id = Guid.NewGuid(),
                    DesignWorkId = newWorkId,
                    Content = $"Dự án được phân nhánh từ phiên bản của dự án gốc (ID: {existingWork.Id})",
                    LogType = DesignLogType.System,
                    Created = CoreHelper.SystemTimeNow,
                    CreatedBy = _user.Username ?? "SYSTEM",
                    LastModified = CoreHelper.SystemTimeNow,
                    LastModifiedBy = _user.Username ?? "SYSTEM"
                };

                // CLONE LOG: Sao chép nội dung từ Log được chọn sang dự án mới
                var clonedLog = new DesignLog
                {
                    Id = Guid.NewGuid(),
                    DesignWorkId = newWorkId,
                    AccountId = sourceLog.AccountId,
                    Content = sourceLog.Content,
                    Metadata = sourceLog.Metadata,
                    IsAI = sourceLog.IsAI,
                    LogType = sourceLog.LogType,
                    Created = CoreHelper.SystemTimeNow.AddMilliseconds(10),
                    CreatedBy = _user.Username ?? "SYSTEM",
                    LastModified = CoreHelper.SystemTimeNow,
                    LastModifiedBy = _user.Username ?? "SYSTEM"
                };

                _context.DesignWorks.Add(designWork);
                _context.DesignLogs.AddRange(clonedLog, branchInfoLog);

            }


            /*designWork = existingWork;
            // Phần này là lấy từ design work cũ đúng không, Nếu chọn log thì là 'Branch' nhưng vẫn nên new DesignWork nha
            // Với lại, nếu chỉ đơn thuần get data lên thì nên thêm AsNoTracking() để nhẹ
            designWork.Id = newId;
            designWork.Name = request.NewDesignName ?? existingWork.Name;
            designWork.RootDesignWorkId = existingWork.RootDesignWorkId;
            designWork.ParentDesignWorkId = existingWork.Id;
            designWork.RelationshipType = DesignRelationshipType.Branch;
            designWork.Status = DesignWorkStatus.Pending;
            designWork.Created = CoreHelper.SystemTimeNow;
            designWork.CreatedBy = _user.Username ?? "SYSTEM";
            designWork.LastModified = CoreHelper.SystemTimeNow;
            designWork.LastModifiedBy = _user.Username ?? "SYSTEM";*/
        }
        // TRƯỜNG HỢP 3: Không gửi gì -> Tạo mới hoàn toàn
        else
        {
            designWork = new DesignWork
            {
                Id = newWorkId,
                RootDesignWorkId = newWorkId,
                RelationshipType = DesignRelationshipType.Original,
                Name = request.NewDesignName ?? "Yêu cầu thiết kế mới",
                CustomerId = customer.Id,
                BaseImageUrl = request.BaseImageUrl,
                Status = DesignWorkStatus.Pending,
                Created = CoreHelper.SystemTimeNow
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
            CreatedBy = _user.Username ?? "SYSTEM",
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
            CreatedBy = _user.Username ?? "SYSTEM",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "SYSTEM"
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
                Quantity = 1,
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username ?? "SYSTEM",
                LastModified = CoreHelper.SystemTimeNow,
                LastModifiedBy = _user.Username ?? "SYSTEM"
            });
        }

        // 6. Tạo Log trao đổi đầu tiên (Dùng chung cho cả Staff và Customer)
        if (!string.IsNullOrEmpty(request.Note) || !string.IsNullOrEmpty(request.BaseImageUrl))
        {
            var initialLog = new DesignLog
            {
                Id = Guid.NewGuid(),
                DesignWorkId = designWork.Id,
                AccountId = userId,
                Content = !string.IsNullOrEmpty(request.Note) ? request.Note : "Khởi tạo yêu cầu thiết kế từ đơn hàng.",
                LogType = DesignLogType.Communication,
                IsAI = false,
                //IsRead = false,
                Metadata = !string.IsNullOrEmpty(request.BaseImageUrl)
                    ? JsonSerializer.Serialize(new List<string> { request.BaseImageUrl })
                    : null,
                Created = CoreHelper.SystemTimeNow.AddMicroseconds(100),
                CreatedBy = _user.Username ?? "SYSTEM",
                LastModified = CoreHelper.SystemTimeNow,
                LastModifiedBy = _user.Username ?? "SYSTEM"
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
            FulfillmentStatus = OrderItemStatuses.Pending,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "SYSTEM",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "SYSTEM"
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
            CreatedBy = _user.Username ?? "SYSTEM",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "SYSTEM"
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
