using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Application.Common.Validation;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Auths.Commands.Login;
public record LoginCommand : IRequest<ResponseLoginModel>
{
    [DefaultValue("Username@123")]
    public string Username { get; init; } = null!;
    [DefaultValue("Password@123")]
    public string Password { get; init; } = null!;
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(v => v.Username)
            .NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
            .ValidUsernameFormat();

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, ResponseLoginModel>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IPasswordService _passwordService;
    public LoginCommandHandler(IApplicationDbContext context, IJwtTokenGenerator tokenGenerator, IPasswordService passwordService)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
        _passwordService = passwordService;
    }

    public async Task<ResponseLoginModel> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        UserIdentity? user = null;

        // --- NHÁNH KIỂM TRA TÀI KHOẢN TRONG DATABASE ---

        Account? account = await _context.Accounts
            .Include(a => a.Manager)
            .Include(a => a.Staff)
            .Include(a => a.Customer)
            .AsNoTracking().FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower(), cancellationToken);

        // Sử dụng BCrypt để kiểm tra mật khẩu đã hash
        if (account == null || !_passwordService.VerifyPassword(request.Password, account.PasswordHash))
        {
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không chính xác.");
        }
        user = new UserIdentity
        {
            Id = account.Id.ToString(),
            Username = request.Username,
            Email = account.Email,
        };
        switch (account)
        {
            case { Manager: not null }:
                user.Role = Roles.MANAGER;
                break;
            case { Staff: not null }:
                user.Role = Roles.STAFF;
                break;
            case { Customer: not null }:
                user.Role = Roles.CUSTOMER;
                break;
            default:
                throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        string jwt = _tokenGenerator.GenerateToken(user);
        ResponseLoginModel res = new ResponseLoginModel
        {
            AccountId = account.Id.ToString(),
            UserName = user.Username,
            FullName = account.Fullname,
            Role = user.Role,
            Token = jwt,
        };


        return res;
    }
}
