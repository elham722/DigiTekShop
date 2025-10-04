using DigiTekShop.ExternalServices.DependencyInjection;
using DigiTekShop.Identity.DependencyInjection;
using DigiTekShop.Infrastructure.DependencyInjection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Infrastructure services (Redis, Caching, DataProtection)
builder.Services.AddInfrastructure(builder.Configuration);

// Identity and External services
builder.Services.ConfigureIdentityCore(builder.Configuration).ConfigureJwtAuthentication(builder.Configuration);
builder.Services.AddExternalServices(builder.Configuration);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MyShop API",
        Version = "v1.0",
        Description = "MyShop E-commerce API v1.0",
        Contact = new OpenApiContact
        {
            Name = "MyShop Team",
            Email = "support@myshop.com"
        }
    });
});

// 🔑 تنظیم ریدایرکت به HTTPS
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 7055; // دقیقاً همونی که در launchSettings نوشتی
});

var app = builder.Build();

// فقط در Production
if (app.Environment.IsProduction())
{
    app.UseHsts();
}

// ⬅️ خیلی مهم: قبل از Swagger
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyShop API V1");
        c.RoutePrefix = string.Empty; // Swagger UI در root
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health checks endpoint
app.MapHealthChecks("/health");

app.Run();