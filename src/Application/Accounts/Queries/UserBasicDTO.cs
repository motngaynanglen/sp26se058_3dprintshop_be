using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.Accounts.Queries;

public class UserBasicDTO
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string Fullname { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? ProfileImageURL { get; set; }
    public string? ContactPhone { get; set; }
    public string? ZaloPhone { get; set; }
    public bool IsActive { get; set; }
    public string Role { get; set; } = null!;

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Account, UserBasicDTO>()
                .ForMember(d => d.Role, opt => opt.MapFrom(s =>
                    s.Manager != null ? Roles.MANAGER :
                    s.Staff != null ? Roles.STAFF :
                    s.Customer != null ? Roles.CUSTOMER : Roles.GUEST));
        }
    }
}
