using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Accounts.Commands;

[Authorize(Roles = Roles.SystemAdmin + "," + Roles.MANAGER)]
public record ActiveAccountCommand : IRequest<bool>
{
    [JsonIgnore] // Ẩn khỏi JSON Body và Swagger
    public Guid Id { get; init; }
}
public class ActiveAccountCommandHandler : IRequestHandler<ActiveAccountCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public ActiveAccountCommandHandler(IApplicationDbContext context,  IUser user)
    {
        _context = context;
        _user = user;
    }
    public async Task<bool> Handle(ActiveAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id.ToGuid();
        //if (string.IsNullOrEmpty(userId))
        //{
        //    throw new UnauthorizedAccessException("Tài khoản chưa được đăng nhập.");
        //}
        // 1. Kiểm tra Username hoặc Email đã tồn tại chưa
        var account = await _context.Accounts
            .Include(x => x.Staff)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (account == null)
        {
            throw new Exception("Tài khoản không tồn tại trong hệ thống.");
        }
        if (_user.Role == Roles.MANAGER && account.Staff == null)
        {
            throw new ForbiddenAccessException("Quản lý chỉ được kích hoạt tài khoản nhân viên.");
        }
        if (!account.IsActive)
        {
            account.IsActive = true;
            account.LastModified = DateTimeOffset.UtcNow;
            account.LastModifiedBy = _user.Username;
        }
        else
        {
            throw new Exception("Tài khoản đang hoạt động");
        }
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

