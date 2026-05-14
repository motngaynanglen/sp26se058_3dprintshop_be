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
public record DeactiveAccountCommand : IRequest<bool>
{
    [JsonIgnore] // Ẩn khỏi JSON Body và Swagger
    public Guid Id { get; init; }
}
public class DeactiveAccountCommandHandler : IRequestHandler<DeactiveAccountCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IUser _user;

    public DeactiveAccountCommandHandler(IApplicationDbContext context, IPasswordService passwordService, IUser user)
    {
        _context = context;
        _passwordService = passwordService;
        _user = user;
    }
    public async Task<bool> Handle(DeactiveAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id.ToGuid();
     
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
            throw new ForbiddenAccessException("Manager chỉ được vô hiệu hóa tài khoản Staff.");
        }
        if (account.IsActive)
        {
            account.IsActive = false;
            account.LastModified = DateTimeOffset.UtcNow;
            account.LastModifiedBy = _user.Username;
        }
        else
        {
            throw new Exception("Tài khoản đã bị vô hiệu hóa.");
        }
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

