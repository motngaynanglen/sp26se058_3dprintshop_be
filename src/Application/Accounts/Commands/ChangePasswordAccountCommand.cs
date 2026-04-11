using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Accounts.Commands;

[Authorize(Roles = Roles.ADMIN)]
public record ChangePasswordAccountCommand : IRequest<bool>
{
    [DefaultValue("Matkhaucu123")]
    public string OldPassword { get; init; } = null!;
    [DefaultValue("Matkhaumoi123456")]
    public string NewPassword { get; init; } = null!;
    [DefaultValue("Matkhaumoi123456")]
    public string ConfirmNewPassword { get; init; } = null!;
}
public class ChangePasswordAccountCommandHandler : IRequestHandler<ChangePasswordAccountCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IUser _user;

    public ChangePasswordAccountCommandHandler(IApplicationDbContext context, IPasswordService passwordService, IUser user)
    {
        _context = context;
        _passwordService = passwordService;
        _user = user;
    }
    public async Task<bool> Handle(ChangePasswordAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id.ToGuid();
        // 1. Kiểm tra Username hoặc Email đã tồn tại chưa
        var account = await _context.Accounts.FirstOrDefaultAsync(a =>  a.Id == userId, cancellationToken);

        if (account == null)
        {
            throw new Exception("Tài khoảng không tồn tại trong hệ thống.");
        }
        // 2. Kiểm tra mật khẩu cũ
        if (!_passwordService.VerifyPassword(request.OldPassword, account.PasswordHash))
        {
            throw new Exception("Mật khẩu cũ không chính xác.");
        }
        // 3. Lưu mật khẩu mới.
        var newPasswordHash = _passwordService.HashPassword(request.NewPassword);
        if (newPasswordHash == null)
        {
            throw new Exception("Lỗi tạo pass (đáng lẽ nó sẽ không xảy ra");
        }
        account.PasswordHash = newPasswordHash;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
