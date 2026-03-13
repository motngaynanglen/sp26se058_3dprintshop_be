using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Models;
using sp26se058_3dprintshop_be.Application.Common.Models.ResponseModels;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Auths.Commands.Login;
public class SystemLoginCommand : IRequest<ResponseLoginModel>
{
    [DefaultValue("AdminUsername@123")]
    public string Username { get; init; } = null!;
    [DefaultValue("AdminPassword@123")]
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
    public SystemLoginCommandHandler(IJwtTokenGenerator tokenGenerator, IConfiguration configuration)
    {
        _tokenGenerator = tokenGenerator;
        _configuration = configuration;
    }

    public async Task<ResponseLoginModel> Handle(SystemLoginCommand request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        UserIdentity? user = null;
        // --- NHÁNH 1: KIỂM TRA TÀI KHOẢN ADMIN DEV TRONG APPSETTINGS ---
        var devAdminUsername = _configuration["DevAccount:Username"];
        var devAdminPassword = _configuration["DevAccount:Password"];
        var devAdminFullname = _configuration["DevAccount:Fullname"];
        if (!string.IsNullOrEmpty(devAdminUsername) &&
            request.Username == devAdminUsername &&
            request.Password == devAdminPassword)
        {
            user = new UserIdentity
            {
                Id = "DEV_ADMIN",
                Username = devAdminUsername,
                Email = devAdminUsername,
                Role = Roles.ADMIN,
            };
        }
        if (user == null)
        {
            throw new ForbiddenAccessException();
        }

        // --- BƯỚC CUỐI: TẠO TOKEN JWT ---
        // Gọi đến Infrastructure thông qua Interface để lấy Token
        // _tokenGenerator.GenerateToken(user);
        string jwt = _tokenGenerator.GenerateToken(user);
        ResponseLoginModel res = new ResponseLoginModel
        {
            AccountId = user.Username,
            UserName = user.Username,
            FullName = devAdminFullname ?? user.Username,
            Role = Roles.ADMIN,
            Token = jwt,
        };
      
        return res;
    }
}
