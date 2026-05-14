using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Json.Serialization;
using sp26se058_3dprintshop_be.Domain.Constants;


namespace sp26se058_3dprintshop_be.Application.Accounts.Commands;

[Authorize(Roles = Roles.Authenticated)]
public record UpdateAccountMineCommand : IRequest<Guid>
{
    // Dữ liệu cần update
    [DefaultValue("newFullname")]
    public string? Fullname { get; init; }
    //[DefaultValue("newFullname")]
    //public string? DateOfBirth { get; init; }
    [DefaultValue("0777777777")]
    public string? ContactPhone { get; init; }
}
public class UpdateAccountMineCommandHandler : IRequestHandler<UpdateAccountMineCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    public UpdateAccountMineCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }
    public async Task<Guid> Handle(UpdateAccountMineCommand request, CancellationToken cancellationToken)
    {
            var currentUserId = _user.Id.ToGuid();
        if (currentUserId == Guid.Empty)
        {
            throw new Exception("Hãy đăng nhập.");
        }
        var entity = await _context.Accounts
              .FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken);

        if (entity == null) throw new Exception("Không tìm thấy tài khoản");

        if (!string.IsNullOrEmpty(request.Fullname)) entity.Fullname = request.Fullname;
        if (!string.IsNullOrEmpty(request.ContactPhone)) entity.ContactPhone = request.ContactPhone;
      
        entity.LastModified = DateTimeOffset.UtcNow;
        entity.LastModifiedBy = _user.Username;
        var result = await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
