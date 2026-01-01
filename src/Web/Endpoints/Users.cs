using sp26se058_3dprintshop_be.Infrastructure.Identity;

namespace sp26se058_3dprintshop_be.Web.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapIdentityApi<ApplicationUser>();
    }
}
