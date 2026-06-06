using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Options;
using sp26se058_3dprintshop_be.Infrastructure.Data;
using sp26se058_3dprintshop_be.Infrastructure.Service;
using sp26se058_3dprintshop_be.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
});

var dockerConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (!string.IsNullOrWhiteSpace(dockerConnectionString))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:DefaultConnection"] = dockerConnectionString
    });
}


var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000", "http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// Add services to the container.
// DbContext is registered in AddInfrastructureServices.
// AI Service
builder.Services.AddHttpClient<IAIService, AIService>();

// Backblaze B2 Service
builder.Services.Configure<BackblazeB2Options>(
    builder.Configuration.GetSection("BackblazeB2"));
builder.Services.AddScoped<IBackblazeB2Service, BackblazeB2Service>();

builder.Services.AddKeyVaultIfConfigured(builder.Configuration);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebServices(builder.Configuration); 

var app = builder.Build();

app.UseExceptionHandler(_ => { });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //await app.InitialiseDatabaseAsync();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
var allowedStaticOrigins = new HashSet<string>(corsOrigins, StringComparer.OrdinalIgnoreCase);

var staticFileProvider = new FileExtensionContentTypeProvider();
staticFileProvider.Mappings[".glb"] = "model/gltf-binary";
staticFileProvider.Mappings[".gltf"] = "model/gltf+json";
staticFileProvider.Mappings[".stl"] = "model/stl";
staticFileProvider.Mappings[".obj"] = "text/plain";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = staticFileProvider,
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream",
    OnPrepareResponse = ctx =>
    {
        var origin = ctx.Context.Request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin) && allowedStaticOrigins.Contains(origin))
        {
            ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            ctx.Context.Response.Headers["Access-Control-Allow-Methods"] = "GET, HEAD, OPTIONS";
            ctx.Context.Response.Headers.Vary = "Origin";
        }
    },
});

app.UseRouting();
app.UseCors("AllowFrontend");

// Authen then Author
app.UseAuthentication();
app.UseAuthorization(); 

app.UseHealthChecks("/health");

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

app.MapRazorPages();

app.MapFallbackToFile("index.html");

app.Map("/", () => "3D Print Shop API is running...");

app.MapMethods("{*path}", new[] { "OPTIONS" }, () => Results.Ok())
   .RequireCors("AllowFrontend");

app.MapEndpoints();

app.MapHub<Mainflow2DesignHub>("/hubs/mainflow-2-design")
   .RequireCors("AllowFrontend");

app.Run();

public partial class Program { }
