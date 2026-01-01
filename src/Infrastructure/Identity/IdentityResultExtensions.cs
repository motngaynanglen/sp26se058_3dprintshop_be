using sp26se058_3dprintshop_be.Application.Common.Models;
using Microsoft.AspNetCore.Identity;

namespace sp26se058_3dprintshop_be.Infrastructure.Identity;

public static class IdentityResultExtensions
{
    public static Result ToApplicationResult(this IdentityResult result)
    {
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(result.Errors.Select(e => e.Description));
    }
}
