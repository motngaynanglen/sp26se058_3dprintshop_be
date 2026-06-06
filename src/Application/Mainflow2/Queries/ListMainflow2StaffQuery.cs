using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;

namespace sp26se058_3dprintshop_be.Application.Mainflow2.Queries;

public record ListMainflow2StaffQuery : IRequest<IReadOnlyList<Mainflow2StaffListItemDto>>;

public class Mainflow2StaffListItemDto
{
    public Guid StaffId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public class ListMainflow2StaffQueryHandler : IRequestHandler<ListMainflow2StaffQuery, IReadOnlyList<Mainflow2StaffListItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public ListMainflow2StaffQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<IReadOnlyList<Mainflow2StaffListItemDto>> Handle(
        ListMainflow2StaffQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_user.Id))
            throw new UnauthorizedAccessException("Cần đăng nhập.");

        if (!Mainflow2DesignAccess.IsManager(_user.Role))
            throw new UnauthorizedAccessException("Chỉ quản lý xem được danh sách nhân viên.");

        return await _context.Staffs
            .AsNoTracking()
            .Include(s => s.Account)
            .Where(s => s.Account.IsActive)
            .OrderBy(s => s.Account.Fullname)
            .Select(s => new Mainflow2StaffListItemDto
            {
                StaffId = s.Id,
                FullName = s.Account.Fullname ?? s.Account.Username,
                Username = s.Account.Username
            })
            .ToListAsync(cancellationToken);
    }
}
