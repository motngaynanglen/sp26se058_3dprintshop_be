using System.ComponentModel;
using MediatR;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Application.DesignWorks.Queries;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Domain.Constants.Statuses;

namespace sp26se058_3dprintshop_be.Application.DesignWorks.Commands;

//[Authorize(Roles = Roles.CUSTOMER)]
public record CreateDesignWorkCommand : IRequest<DesignWorkDTO>
{
    [DefaultValue("Thiết kế mô hình nhân vật Custom")]
    public string Name { get; init; } = null!;

    [DefaultValue("https://example.com/image-mo-ta.png")]
    public string? BaseImageUrl { get; init; }

    [DefaultValue("https://example.com/initial-file.stl")]
    public string? InitialFileUrl { get; init; } // File đính kèm lúc tạo

    [DefaultValue("Bản phác thảo ban đầu")]
    public string? VersionTitle { get; init; }
}

public class CreateDesignWorkCommandHandler : IRequestHandler<CreateDesignWorkCommand, DesignWorkDTO>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    public CreateDesignWorkCommandHandler(IApplicationDbContext context, IMapper mapper, IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<DesignWorkDTO> Handle(CreateDesignWorkCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _user.Id;
        if (string.IsNullOrEmpty(currentUserId)) throw new UnauthorizedAccessException();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.AccountId == Guid.Parse(currentUserId), cancellationToken);

        if (customer == null) throw new NotFoundException(nameof(Customer), currentUserId);

        // 1. Khởi tạo DesignWork
        var designWork = new DesignWork
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            BaseImageUrl = request.BaseImageUrl,
            CustomerId = customer.Id,
            Status = DesignWorkStatus.Pending,
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "SYSTEM"
        };

        // 2. Tạo Log đầu tiên cho hệ thống
        var initialLog = new DesignLog
        {
            Id = Guid.NewGuid(),
            DesignWorkId = designWork.Id,
            LogType = "COMMUNICATION",
            Content = $"Khách hàng đã tạo yêu cầu thiết kế: {request.Name}",
            IsAI = false,
            AccountId = Guid.Parse(currentUserId),
            Created = CoreHelper.SystemTimeNow,
            CreatedBy = _user.Username ?? "SYSTEM"
        };

        _context.DesignWorks.Add(designWork);
        _context.DesignLogs.Add(initialLog);

        // 3. Nếu có file đính kèm, tạo VersionHistory đầu tiên
        if (!string.IsNullOrEmpty(request.InitialFileUrl))
        {
            var firstVersion = new DesignVersionHistory
            {
                Id = Guid.NewGuid(),
                DesignWorkId = designWork.Id,
                DesignLogId = initialLog.Id, // Liên kết với log vừa tạo
                UploaderId = Guid.Parse(currentUserId),
                Tilte = request.VersionTitle ?? "Initial Version",
                FileUrl = request.InitialFileUrl,
                VersionNumber = 1,
                IsPreviewable = true,
                IsApproved = false,
                IsPrintable = false,
                Created = CoreHelper.SystemTimeNow,
                CreatedBy = _user.Username ?? "SYSTEM"
            };
            _context.DesignVersionHistorys.Add(firstVersion);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new CreateFailureException(nameof(DesignWork), ex.Message);
        }

        return _mapper.Map<DesignWorkDTO>(designWork);
    }
}
