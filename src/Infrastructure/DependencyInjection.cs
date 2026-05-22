using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Domain.Constants;
using sp26se058_3dprintshop_be.Infrastructure.Data;
using sp26se058_3dprintshop_be.Infrastructure.Data.Interceptors;
using sp26se058_3dprintshop_be.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using sp26se058_3dprintshop_be.Infrastructure.Service;
using sp26se058_3dprintshop_be.Application.Common.Config;
using Microsoft.Extensions.Options;
using PayOS;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        Guard.Against.Null(connectionString, message: "Connection string 'DefaultConnection' not found.");

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

            //options.UseSqlServer(connectionString);
            options.UseMySql(connectionString,
                ServerVersion.AutoDetect(connectionString), // Tự động nhận diện bản MySQL/MariaDB
                mysqlOptions =>
                {
                    // Quan trọng nhất để fix lỗi bạn đang gặp
                    mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ApplicationDbContextInitialiser>();

        services.AddAuthentication()
            .AddBearerToken(IdentityConstants.BearerScheme);

        services.AddAuthorizationBuilder();

        //services
        //    .AddIdentityCore<ApplicationUser>()
        //    .AddRoles<IdentityRole>()
        //    .AddEntityFrameworkStores<ApplicationDbContext>()
        //    .AddApiEndpoints();

        services.AddSingleton(TimeProvider.System);
        services.AddTransient<IIdentityService, IdentityService>();

        services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.MANAGER)));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IPaymentService, PayOsService>();
        services.AddScoped<IOrderWorkflowService, OrderWorkflowService>();
        services.AddScoped<IOrderPendingService, OrderPendingService>();
        services.AddSingleton<ICodeGeneratorService, CodeGeneratorService>();
        services.AddScoped<IPricingEngine, PricingEngine>();
        services.Configure<ExpiredPendingOrderJobOptions>(configuration.GetSection(ExpiredPendingOrderJobOptions.SectionName));
        services.AddHostedService<ExpiredPendingOrderHostedService>();

        // Cấu hình Email
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddTransient<IEmailService, EmailService>();
        // Cấu hình PayOS

        services.Configure<PayOsSettings>(configuration.GetSection(PayOsSettings.SectionName));
        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<PayOsSettings>>().Value;
            return new PayOSClient(settings.ClientId, settings.ApiKey, settings.ChecksumKey);
        });
        services.Configure<BackblazeB2Settings>(configuration.GetSection(BackblazeB2Settings.SectionName));
        services.AddScoped<IS3StorageService, S3StorageService>();
        return services;
    }
}
