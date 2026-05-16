using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Auths.Commands.Login;
public record ForgetPasswordCommand : IRequest<bool>
{
    [EmailAddress]
    public string Email { get; set; } = null!;
}

public class ForgetPasswordCommandHandler : IRequestHandler<ForgetPasswordCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService; 
    public ForgetPasswordCommandHandler(IApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<bool> Handle(ForgetPasswordCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
             .FirstOrDefaultAsync(a => a.Email == request.Email, cancellationToken);

        if (account == null)
        {
            throw new DataNotFoundException("Email của bạn không tồn tại trong hệ thống.");
        }
        

        account.PasswordResetToken = Guid.NewGuid().ToString();
        account.ResetTokenExpires = DateTimeOffset.UtcNow.AddMinutes(15); // Hết hạn sau 15p
        
        await _context.SaveChangesAsync(cancellationToken);

        await _emailService.SendEmailAsync(account.Email, "Đặt lại mật khẩu",
            $"Mã đặt lại mật khẩu của bạn là: {account.PasswordResetToken}");

        return true;
    }
}
