using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.DesignWorks.Queries;
using sp26se058_3dprintshop_be.Application.Orders.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;
using sp26se058_3dprintshop_be.Domain.Constants.Types;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Commands;
[Authorize(Roles = Roles.CUSTOMER)]
public class CheckoutQuickPrintCommand : IRequest<object>
{
    [DefaultValue("Dự án A")]
    public string? ProjectName { get; init; }
    [DefaultValue("Ghi chú yêu cầu về kỹ thuật in (màu sắc, độ mịn...)")]
    public string? Description { get; init; }
    // Danh sách tối đa 5 URL file .glb/.stl đã upload lên B2 thành công
    public List<string> FileUrls { get; init; } = new List<string>();
}
public class CheckoutQuickPrintCommandValidator : AbstractValidator<CheckoutQuickPrintCommand>
{
    public CheckoutQuickPrintCommandValidator()
    {
        RuleFor(v => v.ProjectName)
            .MaximumLength(200).WithMessage("Tên dự án không được quá 200 ký tự.")
            .NotEmpty().WithMessage("Tên dự án không được để trống.");

        RuleFor(v => v.FileUrls)
            .NotEmpty().WithMessage("Bạn phải cung cấp ít nhất một file 3D.")
            .Must(list => list.Count <= 5).WithMessage("Chỉ được phép gửi tối đa 5 file cho một yêu cầu in.");

        RuleForEach(v => v.FileUrls)
            .NotEmpty().WithMessage("Đường dẫn file không được để trống.")
            .Must(url => url.Contains(".glb") || url.Contains(".stl"))
            .WithMessage("Hệ thống chỉ hỗ trợ định dạng .glb hoặc .stl");

        RuleFor(v => v.Description)
            .MaximumLength(1000).WithMessage("Mô tả không được quá 1000 ký tự.");
    }
}
public class CheckoutQuickPrintCommandHandler : IRequestHandler<CheckoutQuickPrintCommand, object>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public CheckoutQuickPrintCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<object> Handle(CheckoutQuickPrintCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id.ToGuid();
        var customer = await _context.Customers.FirstOrDefaultAsync(x => x.AccountId == userId);
        if (customer == null) throw new ForbiddenAccessException();


        Guid newWorkId = Guid.NewGuid();

        // 1. Tạo Quick DesignWork
        var designWork = new DesignWork
        {
            Id = newWorkId,
            RootDesignWorkId = newWorkId,
            RelationshipType = DesignRelationshipType.Original,
            Name = request.ProjectName ?? "Yêu cầu in 3D nhanh",
            CustomerId = customer.Id,
            // Với Quick Print, trạng thái nhảy thẳng tới REVIEWING để nhân viên check file báo giá
            Status = DesignWorkStatus.Reviewing,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "CUSTOMER"
        };

        // 2. Tạo DesignLog chứa các file của khách (VERSION_UPDATE)
        var fileLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = newWorkId,
            AccountId = userId,
            Content = request.Description ?? "Khách hàng gửi file in gốc.",
            LogType = DesignLogType.VersionUpdate, // Đánh dấu là phiên bản file
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username,
            LastModified = CoreHelper.SystemTimeNow,
            LastModifiedBy = _user.Username
        };

        foreach (var url in request.FileUrls)
        {
            var versionHistory = new DesignVersionHistory
            {
                Id = Guid.NewGuid(),
                DesignWorkId = newWorkId,
                DesignLogId = fileLog.Id, // Liên kết với log để biết file này từ tin nhắn nào
                UploaderId = userId,
                FileUrl = url,
                Tilte = $"File gốc: {Path.GetFileName(url.Split('?')[0])}", // Lưu ý: 'Tilte' theo entity bạn gửi
                VersionNumber = 1,
                IsPreviewable = true,
                IsApproved = false,
                IsPrintable = true, // Vì đây là luồng in ngay nên mặc định file khách gửi là file muốn in
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username ?? "CUSTOMER",
                LastModified = CoreHelper.SystemTimeNow,
                LastModifiedBy = _user.Username ?? "CUSTOMER"
            };
            _context.DesignVersionHistorys.Add(versionHistory);
        }
        // 3. Tạo System Log để đánh dấu luồng Print Service
        var systemLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = newWorkId,
            Content = "Hệ thống: Khởi tạo luồng PRINT_SERVICE. Chờ nhân viên kiểm tra file và báo giá.",
            LogType = DesignLogType.System,
            Created = CoreHelper.SystemTimeNow.AddMilliseconds(10),
            CreatedBy = "SYSTEM"
        };

        // 4. Khởi tạo Order (Tạm thời TotalPrice = 0 vì chờ báo giá Technical Draft)
        /* var order = new Order
         {
             Id = Guid.NewGuid(),
             Code = $"ORD-PRT-{DateTime.Now:yyyyMMddHHmmss}",
             CustomerId = customer.Id,
             OrderStatus = OrderStatuses.Pending,
             TotalPrice = 0, // Báo giá in sẽ được cập nhật sau Review
             Created = CoreHelper.SystemTimeNow,
             CreatedBy = _user.Username ?? "CUSTOMER"
         };

         var orderItem = new OrderItem
         {
             Id = Guid.NewGuid(),
             OrderId = order.Id,
             Order = order,
             SourceType = SourceTypes.PrintService, // Phân biệt với DesignService
             DesignWorkId = designWork.Id,
             ItemName = $"Dịch vụ in theo yêu cầu: {designWork.Name}",
             QuantityOrdered = 1,
             UnitPrice = 0,
             TotalPrice = 0,
             FulfillmentStatus = OrderItemStatuses.Pending,
             Created = CoreHelper.SystemTimeNow
         };*/

        // 5. Lưu vào Database
        _context.DesignWorks.Add(designWork);
        _context.DesignLogs.AddRange(fileLog, systemLog);
        //_context.Orders.Add(order);
        //_context.OrderItems.Add(orderItem);

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<DesignWorkDTO>(designWork);
    }
}
