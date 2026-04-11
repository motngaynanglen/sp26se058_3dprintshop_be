using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using FluentValidation.Results;
using sp26se058_3dprintshop_be.Application.Common.Exceptions;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;
using ValidationException = sp26se058_3dprintshop_be.Application.Common.Exceptions.ValidationException;

namespace sp26se058_3dprintshop_be.Application.Accounts.Commands;

[Authorize(Roles = Roles.ADMIN)]
public record CreateAccountCommand : IRequest<Guid>
{
    [DefaultValue("daylausername")]
    public string Username { get; init; } = null!;
    [DefaultValue("123456")]
    public string Password { get; init; } = null!;
    [DefaultValue("daylafullname")]
    public string Fullname { get; init; } = null!;
    [DefaultValue("Email@gmail.com")]
    public string Email { get; init; } = null!;
    [DefaultValue("0777777777")]
    public string? ContactPhone { get; init; }
    [DefaultValue("CUSTOMER")]
    public string Role { get; init; } = Roles.CUSTOMER; // Mặc định là Customer
}
public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;

    public CreateAccountCommandHandler(IApplicationDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }
    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();
        var usernameClean = request.Username.Trim().ToLower();
        var emailClean = request.Email.Trim().ToLower();

        // 1. Kiểm tra Username hoặc Email đã tồn tại chưa
        var existAccount = await _context.Accounts
            .Where(a => a.Username.ToLower() == usernameClean || a.Email.ToLower() == emailClean)
            .ToListAsync(cancellationToken);

        if (existAccount.Any())
        {
            if (existAccount.Any(a => a.Username.ToLower() == usernameClean))
                failures.Add(new ValidationFailure(nameof(request.Username), "Tên đăng nhập hoặc Email đã tồn tại."));
            if (existAccount.Any(a => a.Email.ToLower() == emailClean))
                failures.Add(new ValidationFailure(nameof(request.Email), "Email này đã được sử dụng."));
        }
        if (request.Role.ToUpper() is not (Roles.MANAGER or Roles.STAFF or Roles.CUSTOMER))
        {
            failures.Add(new ValidationFailure(nameof(request.Role), "Vai trò không tồn tại trong hệ thống."));
        }
        if (failures.Any()) throw new ValidationException(failures);
        // 2. Băm mật khẩu bằng BCrypt
        var passwordHash = _passwordService.HashPassword(request.Password);
        if (passwordHash == null)
        {
            throw new Exception("Không thể tạo mã băm mật khẩu.");
        }
        var newAccount = new Account
        {
            Username = request.Username.ToLower(),
            PasswordHash = passwordHash, // Lưu ý: Nên Hash password ở đây
            Fullname = request.Fullname,
            Email = request.Email.ToLower(),
            ContactPhone = request.ContactPhone,
            IsActive = true
        };

        // Khởi tạo quan hệ dựa trên Role
        switch (request.Role.ToUpper())
        {
            case Roles.MANAGER:
                var manager = new Manager { Account = newAccount };
                _context.Managers.Add(manager);
                break;
            case Roles.STAFF:
                var staff = new Staff { Account = newAccount };
                _context.Staffs.Add(staff); // Chỗ này dùng đúng tên DbSet của bạn
                break;
            case Roles.CUSTOMER:
                var customer = new Customer { Account = newAccount };
                _context.Customers.Add(customer);
                break;
        }
      
        _context.Accounts.Add(newAccount);
        await _context.SaveChangesAsync(cancellationToken);
        return newAccount.Id;
    }
}
