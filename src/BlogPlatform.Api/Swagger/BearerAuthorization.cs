using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace BlogPlatform.Api.Swagger
{
    public static class BearerAuthorizationExtensions
    {
        private static readonly OpenApiSecurityScheme BearerSecurityScheme = new()
        {
            Name = "Authorization",
            In = ParameterLocation.Header,
            BearerFormat = "Bearertoken",
            Reference = new OpenApiReference()
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer",
            }
        };

        public static void AddBearerAuthorization(this SwaggerGenOptions options)
        {
            options.OperationFilter<BearerAuthorization>();
            options.AddSecurityDefinition("Bearer", BearerSecurityScheme);
        }

        public static void AddTimeSpanSchema(this SwaggerGenOptions options)
        {
            options.MapType<TimeSpan>(() => new OpenApiSchema { Type = "string", Example = new OpenApiString("-d.hh:mm:ss.ffffff") });
        }

        class BearerAuthorization : IOperationFilter
        {
            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                bool hasAllowAnonymousAttribute = context.ApiDescription.CustomAttributes().OfType<AllowAnonymousAttribute>().Any();
                bool hasAuthorizeAttribute = context.ApiDescription.CustomAttributes().OfType<AuthorizeAttribute>().Any();

                if (hasAllowAnonymousAttribute || !hasAuthorizeAttribute)
                {
                    return;
                }

                operation.Security.Add(new OpenApiSecurityRequirement()
                {
                    { BearerSecurityScheme, Array.Empty<string>() }
                });
            }
        }
    }
}
