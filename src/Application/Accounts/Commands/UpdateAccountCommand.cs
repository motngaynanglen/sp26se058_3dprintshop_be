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

[Authorize(Roles = Roles.SystemAdmin)]
public record UpdateAccountCommand : IRequest<Guid>
{
    [JsonIgnore] // ?n kh?i JSON Body và Swagger
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; init; }
    // D? li?u c?n update
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
              .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null) throw new Exception("Không tìm th?y tài kho?n");
        if (!string.IsNullOrEmpty(request.Email) &&
        !string.Equals(entity.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            // Ki?m tra xem có AI KHÁC (Id != request.Id) dang dùng Email này không
            var isEmailUsed = await _context.Accounts.AnyAsync(a =>
                a.Id != request.Id && a.Email == request.Email, cancellationToken);

            if (isEmailUsed)
            {
                throw new Exception("Email này dã du?c s? d?ng b?i m?t tài kho?n khác.");
            }

            entity.Email = request.Email;
        }

        if (!string.IsNullOrEmpty(request.Fullname)) entity.Fullname = request.Fullname;
        if (!string.IsNullOrEmpty(request.ContactPhone)) entity.ContactPhone = request.ContactPhone;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
        // Bam m?t kh?u b?ng BCrypt
        if (!string.IsNullOrEmpty(request.Password))
        {
            var passwordHash = _passwordService.HashPassword(request.Password);
            if (string.IsNullOrEmpty(passwordHash))
            {
                throw new Exception("L?i t?o pass");
            }
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
            .WithMessage("M?t kh?u m?i ph?i có ít nh?t 6 ký t?.");
    }
}
