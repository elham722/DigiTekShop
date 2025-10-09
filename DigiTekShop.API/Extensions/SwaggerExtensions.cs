using Microsoft.OpenApi.Models;
using System.Reflection;

namespace DigiTekShop.API.Extensions;

/// <summary>
/// Extensions for configuring Swagger/OpenAPI
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddModernSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(c =>
        {
            // ✅ API Information
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DigiTekShop API",
                Version = "v1.0",
                Description = @"
**DigiTekShop E-commerce API v1.0**

A modern, secure, and scalable e-commerce API built with:
- ✅ Clean Architecture
- ✅ CQRS with MediatR
- ✅ JWT Authentication with Refresh Token Rotation
- ✅ Redis Caching & Session Management
- ✅ Rate Limiting & Brute Force Protection
- ✅ Comprehensive Security Headers
- ✅ Health Checks for Monitoring

## Authentication
Use the `Authorize` button below to add your Bearer token.

## Rate Limiting
- Global: 100 requests/minute
- Auth endpoints: 5 requests/minute

## Support
For issues or questions, contact our support team.
                ",
                Contact = new OpenApiContact
                {
                    Name = "DigiTekShop Team",
                    Email = "support@digitekshop.com",
                    Url = new Uri("https://digitekshop.com/contact")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                },
                TermsOfService = new Uri("https://digitekshop.com/terms")
            });

            // ✅ JWT Bearer Authentication
            var jwtScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Enter your JWT token in the format: `Bearer {token}`",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            c.AddSecurityDefinition("Bearer", jwtScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { jwtScheme, Array.Empty<string>() }
            });

            // ✅ XML Comments for detailed documentation
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

            // ✅ Group endpoints by tags
            c.TagActionsBy(api =>
            {
                if (api.GroupName != null)
                    return new[] { api.GroupName };

                var controllerName = api.ActionDescriptor.RouteValues["controller"];
                return new[] { controllerName ?? "Default" };
            });

            c.DocInclusionPredicate((name, api) => true);

            // ✅ Custom operation IDs
            c.CustomOperationIds(apiDesc =>
            {
                var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
                var actionName = apiDesc.ActionDescriptor.RouteValues["action"];
                return $"{controllerName}_{actionName}";
            });

            // ✅ Add server URLs
            c.AddServer(new OpenApiServer
            {
                Url = configuration["Swagger:ServerUrl"] ?? "https://localhost:7055",
                Description = "Development Server"
            });

            // ✅ Schema customization
            c.SchemaFilter<EnumSchemaFilter>();
            c.OperationFilter<SecurityRequirementsOperationFilter>();
            c.OperationFilter<AddDeviceIdHeaderOperationFilter>();
        });

        return services;
    }

    public static IApplicationBuilder UseModernSwagger(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "api-docs/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/api-docs/v1/swagger.json", "DigiTekShop API V1");
                c.RoutePrefix = string.Empty; // Swagger UI at root
                c.DocumentTitle = "DigiTekShop API Documentation";
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                c.DefaultModelsExpandDepth(2);
                c.DisplayRequestDuration();
                c.EnableDeepLinking();
                c.EnableFilter();
                c.ShowExtensions();
                c.EnableValidator();
                
                // Custom CSS
                c.InjectStylesheet("/swagger-ui/custom.css");
            });
        }

        return app;
    }
}

/// <summary>
/// Filter to add enum value descriptions to Swagger schema
/// </summary>
public class EnumSchemaFilter : Swashbuckle.AspNetCore.SwaggerGen.ISchemaFilter
{
    public void Apply(Microsoft.OpenApi.Models.OpenApiSchema schema, Swashbuckle.AspNetCore.SwaggerGen.SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            foreach (var name in Enum.GetNames(context.Type))
            {
                schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiString(name));
            }
        }
    }
}

/// <summary>
/// Filter to add security requirements to operations
/// </summary>
public class SecurityRequirementsOperationFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(Microsoft.OpenApi.Models.OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        var hasAuthorize = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Any() ?? false;

        var hasAllowAnonymous = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>()
            .Any() ?? false;

        if (hasAuthorize && !hasAllowAnonymous)
        {
            operation.Responses.TryAdd("401", new Microsoft.OpenApi.Models.OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new Microsoft.OpenApi.Models.OpenApiResponse { Description = "Forbidden" });
        }
    }
}

