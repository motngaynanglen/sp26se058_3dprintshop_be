using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Auths.Commands.Login;

public class SystemLoginCommand : IRequest<ResponseLoginModel>
{
    [DefaultValue("admin")]
    public string Username { get; init; } = null!;
    [DefaultValue("Admin@123")]
    public string Password { get; init; } = null!;
}

public class SystemLoginCommandValidator : AbstractValidator<SystemLoginCommand>
{
    public SystemLoginCommandValidator()
    {
        RuleFor(v => v.Username)
            .NotEmpty().WithMessage("Tên đăng nhập không được để trống.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.");
    }
}

public class SystemLoginCommandHandler : IRequestHandler<SystemLoginCommand, ResponseLoginModel>
{
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;

    public SystemLoginCommandHandler(
        IJwtTokenGenerator tokenGenerator,
        IConfiguration configuration,
        IApplicationDbContext context,
        IPasswordService passwordService)
    {
        _tokenGenerator = tokenGenerator;
        _configuration = configuration;
        _context = context;
        _passwordService = passwordService;
    }

    public async Task<ResponseLoginModel> Handle(SystemLoginCommand request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var devAdminUsername = _configuration["DevAccount:Username"];
        var devAdminPassword = _configuration["DevAccount:Password"];
        var devAdminFullname = _configuration["DevAccount:Fullname"];

        // 1. Tài khoản admin cấu hình trong appsettings (DevAccount)
        if (!string.IsNullOrEmpty(devAdminUsername)
            && !string.IsNullOrEmpty(devAdminPassword)
            && username.Equals(devAdminUsername, StringComparison.OrdinalIgnoreCase)
            && request.Password == devAdminPassword)
        {
            return BuildResponse(
                accountId: devAdminUsername,
                username: devAdminUsername,
                fullName: devAdminFullname ?? devAdminUsername,
                role: Roles.ADMIN,
                userId: "DEV_ADMIN",
                email: devAdminUsername);
        }

        // 2. Manager / Staff trong database
        var account = await _context.Accounts
            .Include(a => a.Manager)
            .Include(a => a.Staff)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.Username.ToLower() == username.ToLower() && a.IsActive,
                cancellationToken);

        if (account != null
            && _passwordService.VerifyPassword(request.Password, account.PasswordHash))
        {
            string? role = account switch
            {
                { Manager: not null } => Roles.MANAGER,
                { Staff: not null } => Roles.STAFF,
                _ => null
            };

            if (role != null)
            {
                return BuildResponse(
                    accountId: account.Id.ToString(),
                    username: account.Username,
                    fullName: account.Fullname,
                    role: role,
                    userId: account.Id.ToString(),
                    email: account.Email);
            }
        }

        throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không chính xác.");
    }

    private ResponseLoginModel BuildResponse(
        string accountId,
        string username,
        string fullName,
        string role,
        string userId,
        string email)
    {
        var user = new UserIdentity
        {
            Id = userId,
            Username = username,
            Email = email,
            Role = role,
        };

        return new ResponseLoginModel
        {
            AccountId = accountId,
            UserName = username,
            FullName = fullName,
            Role = role,
            Token = _tokenGenerator.GenerateToken(user),
        };
    }
}
