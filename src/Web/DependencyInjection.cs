using Azure.Identity;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Infrastructure.Data;
using sp26se058_3dprintshop_be.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

using NSwag;
using NSwag.Generation.Processors.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddScoped<IUser, CurrentUser>();

        services.AddHttpContextAccessor();

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>();

        services.AddExceptionHandler<CustomExceptionHandler>();

        services.AddRazorPages();

        // Customise default API behaviour
        services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        services.AddEndpointsApiExplorer();
        // --- CẤU HÌNH AUTHENTICATION THỰC TẾ ---
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"];

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true, // Kiểm tra xem Token còn hạn không
                ValidateIssuerSigningKey = true, // Kiểm tra chữ ký bảo mật
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
            };
        });
        services.AddAuthorization(); // Kích hoạt phân quyền

        services.AddOpenApiDocument((configure, sp) =>
        {
            configure.Title = "sp26se058_3dprintshop_be API";

            // Add JWT
            configure.AddSecurity("JWT", Enumerable.Empty<string>(), new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.ApiKey,
                Name = "Authorization",
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "Type into the textbox: Bearer {your JWT token}."
            });

            configure.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
        });

        return services;
    }

    public static IServiceCollection AddKeyVaultIfConfigured(this IServiceCollection services, ConfigurationManager configuration)
    {
        var keyVaultUri = configuration["AZURE_KEY_VAULT_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }

        return services;
    }
}
