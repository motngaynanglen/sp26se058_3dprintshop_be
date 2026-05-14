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

namespace sp26se058_3dprintshop_be.Application.Accounts.Commands;

[Authorize(Roles = Roles.SystemAdmin + "," + Roles.MANAGER)]
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
    public string Role { get; init; } = Roles.CUSTOMER; // Default is Customer
}
//public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
//{
//    public CreateAccountCommandValidator(IApplicationDbContext context)
//    {
//        RuleFor(x => x.Username).CustomAsync(async (username, ctx, ct) => {
//            var res = await context.Accounts.GetDuplicateResultAsync(x => x.Username == username, "Tài khoản", "Username", username, ct: ct);
//            if (res.IsDuplicate) ctx.AddFailure(res.GetErrorMessage());
//        });

//        RuleFor(x => x.Email).CustomAsync(async (email, ctx, ct) => {
//            var res = await context.Accounts.GetDuplicateResultAsync(x => x.Email == email, "Tài khoản", "Email", email, ct: ct);
//            if (res.IsDuplicate) ctx.AddFailure(res.GetErrorMessage());
//        });

//        RuleFor(x => x.Role).Must(role => new[] { Roles.MANAGER, Roles.STAFF, Roles.CUSTOMER }.Contains(role.ToUpper()))
//            .WithMessage("Vai trò không tồn tại.");
//    }
//}
public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IUser _user;

    public CreateAccountCommandHandler(IApplicationDbContext context, IPasswordService passwordService, IUser user)
    {
        _context = context;
        _passwordService = passwordService;
        _user = user;
    }
    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();
        var usernameClean = request.Username.Trim().ToLower();
        var emailClean = request.Email.Trim().ToLower();

        var userDup = await _context.Accounts.GetDuplicateResultAsync(
        x => x.Username == request.Username, nameof(Account), nameof(request.Username), request.Username, ct: cancellationToken);
        if (userDup.IsDuplicate) failures.AddFailure(nameof(request.Username), userDup.GetErrorMessage());

        var emailDup = await _context.Accounts.GetDuplicateResultAsync(
        x => x.Email == request.Email, nameof(Account), nameof(request.Email), request.Email, ct: cancellationToken);
        if (emailDup.IsDuplicate) failures.AddFailure(nameof(request.Email), emailDup.GetErrorMessage());

        if (request.Role.ToUpper() is not (Roles.MANAGER or Roles.STAFF or Roles.CUSTOMER))
        {
            failures.AddFailure(nameof(request.Role), "Vai trò không tồn tại trong hệ thống.");
        }

        if (_user.Role == Roles.MANAGER && request.Role.ToUpper() != Roles.STAFF)
        {
            failures.AddFailure(nameof(request.Role), "Manager chỉ được tạo tài khoản Staff.");
        }
        if (failures.Any()) throw new ValidationException(failures);
        // 2. Hash password with BCrypt.
        var passwordHash = _passwordService.HashPassword(request.Password);
        if (passwordHash == null)
        {
            throw new BusinessException("Không thể mã hóa mật khẩu.");
        }
        var newAccount = new Account
        {
            Username = request.Username.ToLower(),
            PasswordHash = passwordHash, 
            Fullname = request.Fullname,
            Email = request.Email.ToLower(),
            ContactPhone = request.ContactPhone,
            IsActive = true
        };

        // Initialize relationship based on role.
        switch (request.Role.ToUpper())
        {
            case Roles.MANAGER:
                var manager = new Manager { Account = newAccount };
                _context.Managers.Add(manager);
                break;
            case Roles.STAFF:
                var staff = new Staff { Account = newAccount };
                _context.Staffs.Add(staff);
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


