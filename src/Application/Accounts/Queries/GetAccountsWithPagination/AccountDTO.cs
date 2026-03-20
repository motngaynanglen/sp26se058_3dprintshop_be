using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Accounts.Queries.GetAccountsWithPagination;
public class AccountDTO
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string Fullname { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? ProfileImageURL { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }

    // Thuộc tính tính toán dựa trên quan hệ
    public string Role { get; set; } = null!;

    // Thông tin Audit và Soft Delete
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset? Deleted { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted => Deleted.HasValue;
    // Cấu hình Mapping ngay trong DTO (Chuẩn của Jason Taylor - Clean Architecture)
    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Account, AccountDTO>()
                .ForMember(d => d.Role, opt => opt.MapFrom(s =>
                    s.Manager != null ? Roles.MANAGER :
                    s.Staff != null ? Roles.STAFF :
                    s.Customer != null ? Roles.CUSTOMER : Roles.GUEST));

            // Lưu ý: Nếu Fullname trong Account khác với Fullname trong bảng Customer/Staff, 
            // bạn có thể map ưu tiên tại đây.
        }
    }
    public static void Register(IMapperConfigurationExpression cfg)
    {
        cfg.AddProfile<Mapping>();
    } // Bản cũ gặp lỗi bảo mật, đổi lên 15 nên phải làm thủ công, nhớ add vào để đăng kí mapp
}
