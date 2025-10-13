using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DigiTekShop.API.Swagger;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerMinimal(
        this IServiceCollection services,
        bool includeXmlComments = true)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DigiTekShop API",
                Version = "v1"
            });

            var bearer = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Bearer {token}",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };
            c.AddSecurityDefinition("Bearer", bearer);

            c.OperationFilter<AuthorizeSecurityRequirementFilter>();

            if (includeXmlComments)
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                var xml = Path.Combine(AppContext.BaseDirectory, $"{asm.GetName().Name}.xml");
                if (File.Exists(xml))
                    c.IncludeXmlComments(xml, includeControllerXmlComments: true);
            }
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerMinimal(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (!env.IsDevelopment()) return app;

        app.UseSwagger(c =>
        {
            // JSON → /api-docs/{documentName}/swagger.json
            c.RouteTemplate = "api-docs/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(c =>
        {
            c.RoutePrefix = "api-docs"; // UI → /api-docs
            var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
            foreach (var desc in provider.ApiVersionDescriptions)
            {
                c.SwaggerEndpoint($"/api-docs/{desc.GroupName}/swagger.json",
                    $"DigiTekShop API {desc.GroupName}");
            }
            c.DocumentTitle = "DigiTekShop API";
            c.DisplayRequestDuration();
        });

        return app;
    }

}


public sealed class AuthorizeSecurityRequirementFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize =
            context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                .Concat(context.MethodInfo.GetCustomAttributes(true))
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                .Any() == true;

        var hasAllowAnonymous =
            context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                .Concat(context.MethodInfo.GetCustomAttributes(true))
                .OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>()
                .Any() == true;

        if (!hasAuthorize || hasAllowAnonymous) return;

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });

        
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
    }
}
