using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Auth.Commands.Login;
public record LoginCommand : IRequest<string>
{
    //    public string Username { get; init; } = null!;
    //    public string Password { get; init; } = null!;
    //}

    //public class LoginCommandValidator : AbstractValidator<LoginCommand>
    //{
    //    public LoginCommandValidator()
    //    {
    //        RuleFor(v => v.Username)
    //            .NotEmpty().WithMessage("Tên đăng nhập không được để trống.");

    //        RuleFor(v => v.Password)
    //            .NotEmpty().WithMessage("Mật khẩu không được để trống.");
    //    }
    //}

    //public class LoginCommandHandler : IRequestHandler<LoginCommand, string>
    //{
    //    private readonly IApplicationDbContext _context;
    //    private readonly IJwtTokenGenerator _tokenGenerator;
    //    private readonly IConfiguration _configuration;

    //    public LoginCommandHandler(    IApplicationDbContext context, IJwtTokenGenerator tokenGenerator,   IConfiguration configuration)
    //    {
    //        _context = context;
    //        _tokenGenerator = tokenGenerator;
    //        _configuration = configuration;
    //    }

    //    public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
    //    {
    //        User? user = null;

    //        // --- NHÁNH 1: KIỂM TRA TÀI KHOẢN ADMIN DEV TRONG APPSETTINGS ---
    //        var devAdminUsername = _configuration["DevAccount:Username"];
    //        var devAdminPassword = _configuration["DevAccount:Password"];

    //        if (!string.IsNullOrEmpty(devAdminUsername) &&
    //            request.Username == devAdminUsername &&
    //            request.Password == devAdminPassword)
    //        {
    //            // Tạo User "ảo" với quyền Admin để cấp Token
    //            user = new User
    //            {
    //                Username = devAdminUsername,
    //                Role = "Admin",
    //                Email = "admin@dev.com"
    //            };
    //        }
    //        else
    //        {
    //            // --- NHÁNH 2: KIỂM TRA TÀI KHOẢN TRONG DATABASE ---
    //            user = await _context.Users
    //                .AsNoTracking() // Tăng hiệu năng vì chỉ đọc dữ liệu
    //                .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

    //            // Sử dụng BCrypt để kiểm tra mật khẩu đã hash
    //            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    //            {
    //                // Lưu ý: Đừng trả về chi tiết là sai pass hay sai user để bảo mật
    //                throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không chính xác.");
    //            }
    //        }

    //        // --- BƯỚC CUỐI: TẠO TOKEN JWT ---
    //        // Gọi đến Infrastructure thông qua Interface để lấy Token
    //        return _tokenGenerator.GenerateToken(user);
    //    }
}
