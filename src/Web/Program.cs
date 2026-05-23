using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;
using sp26se058_3dprintshop_be.Application.Common.Options;
using sp26se058_3dprintshop_be.Infrastructure.Data;
using sp26se058_3dprintshop_be.Infrastructure.Service;
using sp26se058_3dprintshop_be.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseMySql(
        cs,
        ServerVersion.AutoDetect(cs)
    );
});
// AI Service
builder.Services.AddHttpClient<IAIService, AIService>();

// Backblaze B2 Service
builder.Services.Configure<BackblazeB2Options>(
    builder.Configuration.GetSection("BackblazeB2"));
builder.Services.AddScoped<IBackblazeB2Service, BackblazeB2Service>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebServices(builder.Configuration); 

var app = builder.Build();

/*// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //await app.InitialiseDatabaseAsync();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}*/
// just use HttpsRedirection at Local (Development)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection(); 
}
app.UseForwardedHeaders();
app.UseExceptionHandler(options => { });
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowFrontend");

// Authen then Author
app.UseAuthentication();
app.UseAuthorization(); 

app.UseHealthChecks("/health");
//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseCors("AllowAll");

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

app.MapRazorPages();
app.MapHub<DesignWorkChatHub>(DesignWorkChatHub.Route)
    .RequireCors("AllowFrontend");

app.Map("/", () => "3D Print Shop API is running...");
app.MapFallbackToFile("index.html");

app.MapMethods("{*path}", new[] { "OPTIONS" }, () => Results.Ok())
   .RequireCors("AllowFrontend");

app.MapEndpoints();

app.Run();

public partial class Program { }
