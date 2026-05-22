using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using sp26se058_3dprintshop_be.Infrastructure.Data;

namespace sp26se058_3dprintshop_be.Application.UnitTests.Common.TestHelpers;

/// <summary>
/// Factory tạo ApplicationDbContext dùng InMemory provider cho unit test.
/// Mỗi test nên dùng tên database khác nhau để đảm bảo độc lập.
/// </summary>
public static class TestDbContextFactory
{
    public static ApplicationDbContext Create(string? dbName = null)
    {
        var name = dbName ?? Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: name)
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(IApplicationDbContext).Assembly));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
