using System;
using System.Collections.Generic;
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

[Authorize(Roles = Roles.ADMIN)]
public record UpdateAccountCommand : IRequest<Guid>
{
    [JsonIgnore] // Ẩn khỏi JSON Body và Swagger
    public Guid Id { get; init; }
    // Dữ liệu cần update
    public string? Fullname { get; init; }
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? ContactPhone { get; init; }
    //public Roles? NewRole { get; init; } // "MANAGER", "STAFF", "CUSTOMER"
    public bool? IsActive { get; init; }
}
public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;

    public UpdateAccountCommandHandler(IApplicationDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }
    public async Task<Guid> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Accounts
              .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null) throw new Exception("Không tìm thấy tài khoản");
        if (!string.IsNullOrEmpty(request.Email) &&
        !string.Equals(entity.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            // Kiểm tra xem có AI KHÁC (Id != request.Id) đang dùng Email này không
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
        // Băm mật khẩu bằng BCrypt
        if (!string.IsNullOrEmpty(request.Password))
        {
            var passwordHash = _passwordService.HashPassword(request.Password);
            if (string.IsNullOrEmpty(passwordHash))
            {
                throw new Exception("Lỗi tạo pass");
            }
        }

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
