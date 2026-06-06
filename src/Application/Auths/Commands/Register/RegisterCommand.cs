using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Validation;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Auths.Commands.Register;
public record RegisterCommand : IRequest<bool>
{
    [DefaultValue("Username@123")]
    public string Username { get; init; } = null!;
    [DefaultValue("Password@123")]
    public string Password { get; init; } = null!;
    [DefaultValue("Nguyen van A")]
    public string Fullname { get; init; } = null!;
    [DefaultValue("VanA@gmail.com")]
    public string Email { get; init; } = null!;
    [DefaultValue("0777777777")]
    public string? ContactPhone { get; init; }
}

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private readonly IApplicationDbContext _context;

    public RegisterCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Username)
            .NotEmpty()
            .MinimumLength(3).WithMessage("Tên đăng nhập tối thiểu 3 ký tự.")
            .ValidUsernameFormat()
            .MustAsync(BeUniqueUsername).WithMessage("Tên đăng nhập đã tồn tại trong hệ thống.");

        RuleFor(v => v.Password).NotEmpty().MinimumLength(6).WithMessage("Mật khẩu tối thiểu 6 ký tự.");
        RuleFor(v => v.Fullname).NotEmpty().MaximumLength(100);

        RuleFor(v => v.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("Định dạng email không hợp lệ.")
            .MustAsync(BeUniqueEmail).WithMessage("Email đã tồn tại trong hệ thống.");

        RuleFor(v => v.ContactPhone)
            .NotEmpty()
            .MustAsync(BeUniqueContactPhone).WithMessage("Số điện thoại đã được sử dụng.");
    }

    private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
    {
        return !await _context.Accounts.AnyAsync(
            a => a.Username.ToLower() == username.ToLower(),
            cancellationToken);
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        return !await _context.Accounts.AnyAsync(
            a => a.Email.ToLower() == email.ToLower(),
            cancellationToken);
    }

    private async Task<bool> BeUniqueContactPhone(string? contactPhone, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contactPhone))
        {
            return true;
        }

        return !await _context.Accounts.AnyAsync(
            a => a.ContactPhone == contactPhone,
            cancellationToken);
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;

    public RegisterCommandHandler(IApplicationDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task<bool> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // 1. Băm mật khẩu bằng BCrypt
        var passwordHash = _passwordService.HashPassword(request.Password);
        if (passwordHash == null)
        {
            throw new Exception("Lỗi tạo pass");
        }
        // 2. Khởi tạo Account mới
        var newAccount = new Account
        {
            Username = request.Username.ToLower(),
            PasswordHash = passwordHash,
            Fullname = request.Fullname,
            Email = request.Email.ToLower(),
            ContactPhone = request.ContactPhone,
        };

        // 3. Khởi tạo thông tin Customer liên kết với Account
        // Vì đây là đăng ký từ phía khách hàng nên mặc định tạo bảng Customer
        var newCustomer = new Customer
        {
            Account = newAccount, // EF Core sẽ tự động map AccountId
            // Các trường thông tin khác của Customer nếu có
        };

        _context.Accounts.Add(newAccount);
        _context.Customers.Add(newCustomer);

        // 4. Lưu xuống Database
        var result = await _context.SaveChangesAsync(cancellationToken);

        return result > 0;
    }
}
