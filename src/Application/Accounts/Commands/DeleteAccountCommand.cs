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

[Authorize(Roles = Roles.ADMIN)]
public record DeleteAccountCommand : IRequest<bool>
{
    [JsonIgnore] // Ẩn khỏi JSON Body và Swagger
    public Guid Id { get; init; }
}
public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteAccountCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }
    public async Task<bool> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("Tài khoản chưa được đăng nhập.");
        }
        // 1. Kiểm tra Username hoặc Email đã tồn tại chưa
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (account == null)
        {
            throw new Exception("Tài khoản không tồn tại trong hệ thống.");
        }
        if (!account.IsActive)
        {
            account.LastModified = DateTimeOffset.UtcNow;
            account.LastModifiedBy = _user.Username;
            account.Deleted = DateTimeOffset.UtcNow;
            account.DeletedBy = _user.Username;
        }
        else
        {
            throw new Exception("Tài khoản đang hoạt động! Hãy hủy quyền trước.");
        }
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
