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
using sp26se058_3dprintshop_be.Web.Hubs;
using System.Security.Claims;
using sp26se058_3dprintshop_be.Domain.Constants;


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
        {
            //options.SuppressModelStateInvalidFilter = true;
            options.InvalidModelStateResponseFactory = context =>
            {
                var error = context.ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage)
                    .FirstOrDefault();
                Console.WriteLine($"[Factory Log]: Model Binding Error - {error}");
                // Ném lỗi này ra để Middleware Exception bắt được
                throw new BadHttpRequestException($"Lỗi định dạng: {error}");
            };
        });

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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role,

                // Định nghĩa lại các loại Claim để [Authorize] và User.Identity.Name hoạt động
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrWhiteSpace(accessToken) &&
                        path.StartsWithSegments(DesignWorkChatHub.Route))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Authenticated", policy =>
                policy.RequireRole(Roles.MANAGER, Roles.STAFF, Roles.CUSTOMER));
            options.AddPolicy("SystemAdmin", policy =>
                policy.RequireRole(Roles.ADMIN));
            options.AddPolicy("Internal", policy =>
                policy.RequireRole(Roles.MANAGER, Roles.STAFF));
            options.AddPolicy("StaffOrManager", policy =>
                policy.RequireRole(Roles.STAFF, Roles.MANAGER));
            options.AddPolicy("CustomerStaffManager", policy =>
                policy.RequireRole(Roles.CUSTOMER, Roles.STAFF, Roles.MANAGER));
        }); // Kích hoạt phân quyền
        services.AddSignalR();

        services.AddOpenApiDocument((configure, sp) =>
        {
            configure.Title = "3D_printshop_API";
            configure.SchemaSettings.SchemaProcessors.Add(new ConstantSchemaProcessor());
            // Add JWT
            configure.AddSecurity("JWT", Enumerable.Empty<string>(), new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.ApiKey,
                Name = "Authorization",
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "Type into the textbox: Bearer {your JWT token}."
            });

            configure.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
            configure.OperationProcessors.Add(new ConstantOperationProcessor());

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
