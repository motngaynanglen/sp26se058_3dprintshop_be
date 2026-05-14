using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;
public class IdentityService : IIdentityService
{
    private readonly IUser _user;

    public IdentityService(IUser user)
    {
        _user = user;
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {

        return await Task.FromResult(
            _user.Id == userId &&
            string.Equals(_user.Role, role, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        return await Task.FromResult(true);
    }

    public async Task<string?> GetUserNameAsync(string userName)
    {
        return await Task.FromResult(_user.Username);
    }
}
