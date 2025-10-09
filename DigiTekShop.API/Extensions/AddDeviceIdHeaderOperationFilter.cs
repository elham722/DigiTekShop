using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DigiTekShop.API.Extensions;


public class AddDeviceIdHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();
        
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Device-Id",
            In = ParameterLocation.Header,
            Required = false,
            Description = "شناسه منحصر به فرد دستگاه کلاینت",
            Schema = new OpenApiSchema { Type = "string" }
        });
    }
}

