using System.Reflection;

namespace sp26se058_3dprintshop_be.Web.Infrastructure;

public static class WebApplicationExtensions
{
    public static RouteGroupBuilder MapGroup(this WebApplication app, EndpointGroupBase group)
    {
        var groupName = group.GetType().Name;

        return app
            .MapGroup($"/api/{groupName}")
            .WithGroupName(groupName)
            .WithTags(groupName)
            .WithOpenApi();
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var endpointGroupType = typeof(EndpointGroupBase);

        //var assembly = Assembly.GetExecutingAssembly();
        var assembly = typeof(Program).Assembly;

        //var endpointGroupTypes = assembly.GetExportedTypes()
        //    .Where(t => t.IsSubclassOf(endpointGroupType));
        var endpointGroupTypes = assembly.GetExportedTypes()
        .Where(t => t.IsSubclassOf(endpointGroupType) && !t.IsAbstract);

        foreach (var type in endpointGroupTypes)
        {
            if (Activator.CreateInstance(type) is EndpointGroupBase instance)
            {
                instance.Map(app);
            }
        }

        return app;
    }
}
