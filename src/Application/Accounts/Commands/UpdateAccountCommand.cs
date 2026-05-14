using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Security;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Accounts.Commands;

[Authorize(Roles = Roles.SystemAdmin + "," + Roles.MANAGER)]
public record UpdateAccountCommand : IRequest<Guid>
{
    [JsonIgnore] // Hidden from JSON body and Swagger
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; init; }
    // Data to update
    [DefaultValue("newFullname")]
    public string? Fullname { get; init; }
    [DefaultValue("newemail123@gmail.com")]
    public string? Email { get; init; }
    [DefaultValue("newPassword123")]
    public string? Password { get; init; }
    [DefaultValue("0777777777")]
    public string? ContactPhone { get; init; }
    //public Roles? NewRole { get; init; } // "MANAGER", "STAFF", "CUSTOMER"
    public bool? IsActive { get; init; }
}
public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IUser _user;
    public UpdateAccountCommandHandler(IApplicationDbContext context, IPasswordService passwordService, IUser user)
    {
        _context = context;
        _passwordService = passwordService;
        _user = user;
    }
    public async Task<Guid> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Accounts
              .Include(x => x.Staff)
              .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null) throw new Exception("Không tìm thấy tài khoản.");
        if (_user.Role == Roles.MANAGER && entity.Staff == null)
        {
            throw new ForbiddenAccessException("Manager chỉ được cập nhật tài khoản Staff.");
        }
        if (!string.IsNullOrEmpty(request.Email) &&
        !string.Equals(entity.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            // Check whether another account is using this email.
            var isEmailUsed = await _context.Accounts.AnyAsync(a =>
                a.Id != request.Id && a.Email == request.Email, cancellationToken);

            if (isEmailUsed)
            {
                throw new Exception("Email này đã được sử dụng bởi một tài khoản khác.");
            }

            entity.Email = request.Email;
        }

        if (!string.IsNullOrEmpty(request.Fullname)) entity.Fullname = request.Fullname;
        if (!string.IsNullOrEmpty(request.ContactPhone)) entity.ContactPhone = request.ContactPhone;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
        // Hash password with BCrypt.
        if (!string.IsNullOrEmpty(request.Password))
        {
            var passwordHash = _passwordService.HashPassword(request.Password);
            if (string.IsNullOrEmpty(passwordHash))
            {
                throw new Exception("Lỗi tạo mật khẩu.");
            }
            entity.PasswordHash = passwordHash;
        }
        entity.LastModified = DateTimeOffset.UtcNow;
        entity.LastModifiedBy = _user.Username;
        var result = await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(v => v.Email).EmailAddress().When(v => !string.IsNullOrEmpty(v.Email));
        RuleFor(v => v.Password).MinimumLength(6).When(v => !string.IsNullOrEmpty(v.Password))
            .WithMessage("Mật khẩu mới phải có ít nhất 6 ký tự.");
    }
}

