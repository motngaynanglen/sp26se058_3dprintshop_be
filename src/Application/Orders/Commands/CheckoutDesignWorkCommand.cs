using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation.Results;
using PayOS.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Constants;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using Microsoft.Extensions.Logging;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Orders.Commands;
[Authorize(Roles =Roles.CUSTOMER)]

public record CheckoutServiceOptionSelectionRequest
{
    public Guid ServiceOptionId { get; init; }
    public int Quantity { get; init; } = 1;
}

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

    // Danh sách id option cũ, giữ lại để FE hiện tại không bị vỡ.
    public List<Guid> ServiceOptionIds { get; init; } = new();

    // Payload mới: hỗ trợ option có số lượng.
    public List<CheckoutServiceOptionSelectionRequest> ServiceOptions { get; init; } = new();
}

public class CheckoutDesignWorkCommandValidator : AbstractValidator<CheckoutDesignWorkCommand>
{
    public CheckoutDesignWorkCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.ServiceOptions?.Any() == true || x.ServiceOptionIds?.Any() == true)
            .WithMessage("Vui lòng chọn ít nhất một tùy chọn dịch vụ thiết kế.");

        RuleForEach(x => x.ServiceOptions!).ChildRules(option =>
        {
            option.RuleFor(x => x.ServiceOptionId)
                .NotEmpty().WithMessage("Tùy chọn dịch vụ không hợp lệ.");

            option.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng tùy chọn phải lớn hơn 0.");
        }).When(x => x.ServiceOptions != null);
    }
}

public class CheckoutDesignWorkCommandHandler : IRequestHandler<CheckoutDesignWorkCommand, OrderDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;
    private readonly IOrderPendingService _orderPendingService;
    private readonly ICodeGeneratorService _codeGenerator;

    public CheckoutDesignWorkCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user, IOrderPendingService orderPendingService, ICodeGeneratorService codeGenerator)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
        _orderPendingService = orderPendingService;
        _codeGenerator = codeGenerator;
    }

    public async Task<OrderDTO> Handle(CheckoutDesignWorkCommand request, CancellationToken cancellationToken)
    {
        // 1. Xác thực người dùng và lấy thông tin Customer
        var userId = _user.Id.ToGuid();
        if (userId == Guid.Empty) throw new UnauthorizedAccessException();

        // Lấy Customer dựa trên AccountId liên kết
        var customer = await _context.Customers.FirstOrDefaultAsync(x => x.AccountId == userId);
        if (customer == null) throw new ForbiddenAccessException();
        await _orderPendingService.EnsureCustomerHasNoPendingOrderAsync(customer.Id, cancellationToken);


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

            int aiLogCount = existingWork.DesignLogs.Count(l => l.IsAI || l.LogType == DesignLogType.AiGen);

            // CASE 1: Dùng tiếp bản gốc (SourceLogId = null)
            if (!request.SourceLogId.HasValue || request.SourceLogId == Guid.Empty)
            {
                if (aiLogCount >= 2)
                {
                    throw new BusinessException("Phát hiện nhiều phiên bản AI. Vui lòng chọn một phiên bản (log) cụ thể để tiếp tục.");
                }

                // Lấy luôn bản gốc
                designWork = existingWork;
                designWork.RootDesignWorkId = existingWork.RootDesignWorkId == Guid.Empty ? existingWork.Id : existingWork.RootDesignWorkId;
                // Backfill WorkType for existing works that pre-date the WorkType field
                if (string.IsNullOrEmpty(designWork.WorkType))
                    designWork.WorkType = DesignWorkTypes.DesignService;
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
                    RootDesignWorkId = existingWork.RootDesignWorkId == Guid.Empty ? existingWork.Id : existingWork.RootDesignWorkId,
                    ParentDesignWorkId = existingWork.Id,
                    RelationshipType = DesignRelationshipType.Branch,
                    CustomerId = customer.Id,
                    BaseImageUrl = existingWork.BaseImageUrl,
                    WorkType = DesignWorkTypes.DesignService,
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

                var sourceVersions = await _context.DesignVersionHistorys
                    .AsNoTracking()
                    .Where(v => v.DesignLogId == sourceLog.Id)
                    .ToListAsync(cancellationToken);

                var nextVersionNumber = 1;
                foreach (var sourceVersion in sourceVersions)
                {
                    _context.DesignVersionHistorys.Add(new DesignVersionHistory
                    {
                        Id = Guid.NewGuid(),
                        DesignWorkId = newWorkId,
                        DesignLogId = clonedLog.Id,
                        UploaderId = sourceVersion.UploaderId,
                        Tilte = sourceVersion.Tilte,
                        FileUrl = sourceVersion.FileUrl,
                        VersionNumber = nextVersionNumber++,
                        IsPreviewable = sourceVersion.IsPreviewable,
                        IsApproved = false,
                        IsPrintable = sourceVersion.IsPrintable,
                        Created = CoreHelper.SystemTimeNow.AddMilliseconds(20),
                        CreatedBy = _user.Username ?? "SYSTEM"
                    });
                }

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
                WorkType = DesignWorkTypes.DesignService,
                Status = DesignWorkStatus.Pending,
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username ?? "CUSTOMER",
                LastModified = CoreHelper.SystemTimeNow,
                LastModifiedBy = _user.Username ?? "CUSTOMER"
            };
            _context.DesignWorks.Add(designWork);


        }
        // 3. Tính toán giá từ các Service Options đã chọn
        var requestedOptionSelections = NormalizeRequestedServiceOptions(request);
        ValidateDuplicateRequestedOptions(requestedOptionSelections);

        var requestedOptionIds = requestedOptionSelections
            .Select(x => x.ServiceOptionId)
            .ToList();

        var selectedOptions = await _context.ServiceOptions
            .Where(so => requestedOptionIds.Contains(so.Id))
            .ToListAsync(cancellationToken);

        var isAdjustmentTopUpOrder = IsAdjustmentTopUpOrder(request, selectedOptions);
        ValidateSelectedServiceOptions(requestedOptionSelections, selectedOptions, requiresDesignPackage: !isAdjustmentTopUpOrder);
        ValidateAdjustmentTopUpOptions(isAdjustmentTopUpOrder, selectedOptions);

        var selectedOptionsById = selectedOptions.ToDictionary(x => x.Id);
        var selectedOptionItems = requestedOptionSelections
            .Select(x => new
            {
                Option = selectedOptionsById[x.ServiceOptionId],
                x.Quantity
            })
            .ToList();

        decimal totalServicePrice = selectedOptionItems.Sum(x => x.Option.DefaultPrice * x.Quantity);
        var adjustmentRoundLimit = selectedOptionItems
            .Sum(x => (x.Option.AdjustmentRoundDelta ?? 0) * x.Quantity);

        // 4. Khởi tạo Order (Gán CustomerId)
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Code = _codeGenerator.GenerateOrderCode(SourceTypes.DesignService),
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
            AdjustmentRoundLimit = adjustmentRoundLimit,
            UsedAdjustmentRoundCount = 0,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "SYSTEM",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "SYSTEM"
        };

        foreach (var item in selectedOptionItems)
        {
            var opt = item.Option;
            _context.ServiceSelectedOptions.Add(new ServiceSelectedOption
            {
                Id = Guid.NewGuid(),
                ServiceSelectionId = serviceSelection.Id,
                ServiceOptionId = opt.Id,
                OptionCodeSnapshot = opt.Code,
                OptionNameSnapshot = opt.Name,
                OptionDescriptionSnapshot = opt.Description,
                OptionGroupCodeSnapshot = opt.GroupCode,
                OptionGroupNameSnapshot = opt.GroupName,
                AppliedPrice = opt.DefaultPrice,
                Quantity = item.Quantity,
                AdjustmentRoundDeltaSnapshot = opt.AdjustmentRoundDelta,
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

        _context.DesignLogs.Add(new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = designWork.Id,
            AccountId = userId,
            Content = "Đã tạo đơn dịch vụ thiết kế. Chờ thanh toán phí thiết kế để chuyển sang giai đoạn xử lý.",
            LogType = DesignLogType.System,
            Created = CoreHelper.SystemTimeNow.AddMicroseconds(200),
            CreatedBy = _user.Username ?? "SYSTEM",
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username ?? "SYSTEM"
        });

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
            InvoiceCode = _codeGenerator.GenerateInvoiceCode(SourceTypes.DesignService),
            TotalAmount = order.TotalPrice,
            PaymentStatus = InvoiceStatuses.Unpaid,
            DueDate = CoreHelper.SystemTimeNow.UtcDateTime.AddMinutes(OrderPaymentConstants.PendingPaymentLifetimeMinutes),
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

    private static bool IsAdjustmentTopUpOrder(CheckoutDesignWorkCommand request, List<ServiceOption> selectedOptions)
    {
        return request.DesignWorkId.HasValue
            && request.DesignWorkId.Value != Guid.Empty
            && selectedOptions.Any()
            && selectedOptions.All(x => x.GroupCode == "REVISION");
    }

    private static void ValidateAdjustmentTopUpOptions(bool isAdjustmentTopUpOrder, List<ServiceOption> selectedOptions)
    {
        if (!isAdjustmentTopUpOrder)
        {
            return;
        }

        var outOfScopeOptions = selectedOptions
            .Where(x => !x.AdjustmentRoundDelta.HasValue || x.AdjustmentRoundDelta.Value <= 0)
            .Select(x => x.Name)
            .ToList();

        if (outOfScopeOptions.Any())
        {
            throw new BusinessException(
                $"Không thể thêm thay đổi vượt phạm vi vào công việc thiết kế hiện tại: {string.Join(", ", outOfScopeOptions)}. Vui lòng tạo nhánh/yêu cầu thiết kế mới hoặc đơn dịch vụ mới.",
                ResponseCodeConstants.VAL_BUSINESS_RESTRICTION);
        }
    }

    private static List<CheckoutServiceOptionSelectionRequest> NormalizeRequestedServiceOptions(CheckoutDesignWorkCommand request)
    {
        if (request.ServiceOptions?.Any() == true)
        {
            return request.ServiceOptions.ToList();
        }

        return (request.ServiceOptionIds ?? new List<Guid>())
            .Select(id => new CheckoutServiceOptionSelectionRequest
            {
                ServiceOptionId = id,
                Quantity = 1
            })
            .ToList();
    }

    private static void ValidateDuplicateRequestedOptions(List<CheckoutServiceOptionSelectionRequest> requestedOptions)
    {
        var failures = new List<ValidationFailure>();

        var duplicatedIds = requestedOptions
            .GroupBy(x => x.ServiceOptionId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicatedIds.Any())
        {
            failures.Add(new ValidationFailure(
                nameof(CheckoutDesignWorkCommand.ServiceOptions),
                $"Không được chọn trùng tùy chọn dịch vụ: {string.Join(", ", duplicatedIds)}."));
        }

        failures.ThrowIfAny();
    }

    private static void ValidateSelectedServiceOptions(
        List<CheckoutServiceOptionSelectionRequest> requestedOptions,
        List<ServiceOption> selectedOptions,
        bool requiresDesignPackage)
    {
        var failures = new List<ValidationFailure>();
        var selectedOptionsById = selectedOptions.ToDictionary(x => x.Id);

        var invalidOptionIds = requestedOptions
            .Select(x => x.ServiceOptionId)
            .Where(id => !selectedOptionsById.TryGetValue(id, out var option) || !option.IsActive)
            .ToList();

        if (invalidOptionIds.Any())
        {
            failures.Add(new ValidationFailure(
                nameof(CheckoutDesignWorkCommand.ServiceOptions),
                $"Tùy chọn dịch vụ không tồn tại hoặc đã ngưng kích hoạt: {string.Join(", ", invalidOptionIds)}."));
        }

        if (requiresDesignPackage && !selectedOptions.Any(x => x.IsActive && x.GroupCode == "DESIGN_PACKAGE"))
        {
            failures.Add(new ValidationFailure(
                nameof(CheckoutDesignWorkCommand.ServiceOptions),
                "Vui lòng chọn một gói thiết kế để xác định phạm vi dịch vụ và chính sách hiệu chỉnh."));
        }

        foreach (var requestedOption in requestedOptions)
        {
            if (!selectedOptionsById.TryGetValue(requestedOption.ServiceOptionId, out var option) || !option.IsActive)
            {
                continue;
            }

            if (!ServiceOptionSelectionTypes.IsValid(option.SelectionType))
            {
                failures.Add(new ValidationFailure(
                    nameof(CheckoutDesignWorkCommand.ServiceOptions),
                    $"Tùy chọn '{option.Name}' đang có loại lựa chọn không hợp lệ: {option.SelectionType}."));
                continue;
            }

            if (option.SelectionType != ServiceOptionSelectionTypes.Quantity && requestedOption.Quantity != 1)
            {
                failures.Add(new ValidationFailure(
                    nameof(CheckoutServiceOptionSelectionRequest.Quantity),
                    $"Tùy chọn '{option.Name}' không hỗ trợ nhập số lượng. Vui lòng chọn số lượng bằng 1."));
            }

            if (requestedOption.Quantity < option.MinQuantity)
            {
                failures.Add(new ValidationFailure(
                    nameof(CheckoutServiceOptionSelectionRequest.Quantity),
                    $"Số lượng của tùy chọn '{option.Name}' phải lớn hơn hoặc bằng {option.MinQuantity}."));
            }

            if (option.MaxQuantity.HasValue && requestedOption.Quantity > option.MaxQuantity.Value)
            {
                failures.Add(new ValidationFailure(
                    nameof(CheckoutServiceOptionSelectionRequest.Quantity),
                    $"Số lượng của tùy chọn '{option.Name}' không được vượt quá {option.MaxQuantity.Value}."));
            }
        }

        var conflictedSingleSelectionGroupCodes = selectedOptions
            .Where(x => x.IsActive)
            .GroupBy(x => x.GroupCode)
            .Where(x => x.Any(option => option.SelectionType == ServiceOptionSelectionTypes.Single) && x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (conflictedSingleSelectionGroupCodes.Any())
        {
            failures.Add(new ValidationFailure(
                nameof(CheckoutDesignWorkCommand.ServiceOptions),
                $"Nhóm chọn một chỉ được chọn một tùy chọn. Nhóm bị trùng: {string.Join(", ", conflictedSingleSelectionGroupCodes)}."));
        }

        failures.ThrowIfAny();
    }
}
