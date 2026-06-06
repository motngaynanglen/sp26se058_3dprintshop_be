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
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? configuration.GetConnectionString("DefaultConnection");
        Guard.Against.Null(connectionString, message: "Connection string 'DefaultConnection' not found.");

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

            options.UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 0, 0)),
                mySqlOptions => mySqlOptions.EnableRetryOnFailure(3));
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ApplicationDbContextInitialiser>();
        services.AddScoped<MockDataSeeder>();

        services.AddAuthentication()
            .AddBearerToken(IdentityConstants.BearerScheme);

        services.AddAuthorizationBuilder();

        //services
        //    .AddIdentityCore<ApplicationUser>()
        //    .AddRoles<IdentityRole>()
        //    .AddEntityFrameworkStores<ApplicationDbContext>()
        //    .AddApiEndpoints();

        services.AddSingleton(TimeProvider.System);
        //services.AddTransient<IIdentityService, IdentityService>();

        services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.ADMIN)));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IPaymentService, PayOsService>();
        services.AddScoped<IVnPayService, VnPayService>();

        // Cấu hình Email
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddTransient<IEmailService, EmailService>();
        // Cấu hình PayOS

        services.Configure<Mainflow2Options>(configuration.GetSection(Mainflow2Options.SectionName));
        services.Configure<PaymentOptions>(configuration.GetSection(PaymentOptions.SectionName));
        services.Configure<FileUploadOptions>(configuration.GetSection(FileUploadOptions.SectionName));
        services.Configure<GlbPublishOptions>(configuration.GetSection(GlbPublishOptions.SectionName));
        services.Configure<PayOsSettings>(configuration.GetSection(PayOsSettings.SectionName));
        services.Configure<VnPaySettings>(configuration.GetSection(VnPaySettings.SectionName));
        services.Configure<GhnSettings>(configuration.GetSection(GhnSettings.SectionName));

        services.AddHttpClient<GhnShippingService>();
        services.AddHttpClient<GhnMasterDataService>();
        services.AddTransient<IGhnMasterDataService>(sp => sp.GetRequiredService<GhnMasterDataService>());
        services.AddScoped<IGhnAddressResolver, GhnAddressResolver>();
        services.AddTransient<IShippingCarrierService>(sp => sp.GetRequiredService<GhnShippingService>());
        services.AddScoped<IShippingCarrierResolver, ShippingCarrierResolver>();
        services.AddSingleton(sp => {
            var settings = sp.GetRequiredService<IOptions<PayOsSettings>>().Value;
            return new PayOSClient(settings.ClientId, settings.ApiKey, settings.ChecksumKey);
        });
        services.Configure<BackblazeB2Settings>(configuration.GetSection(BackblazeB2Settings.SectionName));
        services.AddScoped<IS3StorageService, S3StorageService>();
        services.AddScoped<IMainflow2RealtimeNotifier, Mainflow2RealtimeNoop>();
        services.AddScoped<IPublicFileBaseUrlResolver, PublicFileBaseUrlResolver>();
        services.AddScoped<IPublicFileStorageService, PublicFileStorageService>();
        services.AddScoped<IGlbPublicUrlService, GlbPublicUrlService>();
        services.AddScoped<IMainflow2AccessibleFileUrlService, Mainflow2AccessibleFileUrlService>();
        services.AddHttpClient<IOpenRouterGlbGenerationService, OpenRouterGlbGenerationService>();
        return services;
    }
}
