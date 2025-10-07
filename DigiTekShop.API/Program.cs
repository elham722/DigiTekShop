using Asp.Versioning;
using DigiTekShop.API.Errors;
using DigiTekShop.Application.DependencyInjection;
using DigiTekShop.ExternalServices.DependencyInjection;
using DigiTekShop.Identity.DependencyInjection;
using DigiTekShop.Infrastructure.DependencyInjection;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("App", "DigiTekShop.API"));


builder.Services.AddCors(o =>
{
    o.AddPolicy("Default", p => p
        .AllowAnyOrigin()  
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Add services to the container.
builder.Services.AddControllers();

// Infrastructure services (Redis, Caching, DataProtection)
builder.Services.AddInfrastructure(builder.Configuration);

// Identity and External services
builder.Services.ConfigureIdentityCore(builder.Configuration).ConfigureJwtAuthentication(builder.Configuration);
builder.Services.AddExternalServices(builder.Configuration);
builder.Services.ConfigureApplicationCore();



builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DigiTekShop API",
        Version = "v1.0",
        Description = "DigiTekShop E-commerce API v1.0",
        Contact = new OpenApiContact { Name = "DigiTekShop Team", Email = "support@myshop.com" }
    });

    // 🔒 JWT Bearer
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});


// 🔑 تنظیم ریدایرکت به HTTPS
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
});

builder.Services.AddApiVersioning(o =>
{
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();


app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms (TraceId: {TraceId})";
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("TraceId", http.TraceIdentifier);
        if (http.User?.Identity?.IsAuthenticated == true)
            diag.Set("User", http.User.Identity!.Name);
    };
});


// فقط در Production
if (app.Environment.IsProduction())
{
    app.UseHsts();
}

// ⬅️ خیلی مهم: قبل از Swagger
app.UseHttpsRedirection();
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DigiTekShop API V1");
        c.RoutePrefix = string.Empty; // Swagger UI در root
    });
}



app.MapControllers();

// Health checks endpoint
app.MapHealthChecks("/health");

app.Run();