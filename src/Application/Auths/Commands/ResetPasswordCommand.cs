using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Generators;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;
using sp26se058_3dprintshop_be.Domain.Utils;

namespace sp26se058_3dprintshop_be.Application.Auths.Commands.Login;
public record ResetPasswordCommand : IRequest<bool>
{
    [DefaultValue("Username@123")]
    public string Username { get; set; } = null!;
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public string Token { get; init; } = string.Empty;
    [DefaultValue("NewPassword@123")]
    public string NewPassword { get; init; } = string.Empty;
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    public ResetPasswordCommandHandler(IApplicationDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Username.ToLower() == request.Username.ToLower() && a.PasswordResetToken == request.Token, cancellationToken);

        if (account == null || account.ResetTokenExpires < DateTimeOffset.UtcNow)
        {
            throw new BusinessException("Mã đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }
        var passwordHash = _passwordService.HashPassword(request.NewPassword);
        if (string.IsNullOrEmpty(passwordHash))
        {
            throw new BusinessException("Không thể mã hóa mật khẩu.");
        }
        account.PasswordHash = passwordHash;

        // Xóa token sau khi dùng xong
        account.PasswordResetToken = null;
        account.ResetTokenExpires = null;
        account.LastModified = CoreHelper.SystemTimeNow;
        account.LastModifiedBy = nameof(ResetPasswordCommand);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
