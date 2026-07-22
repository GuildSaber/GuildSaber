using System.Text.Json;
using System.Text.Json.Serialization;
using GuildSaber.Api.Transformers;
using Microsoft.AspNetCore.Http.Features;
using Scalar.AspNetCore;

namespace GuildSaber.Api.Setup;

public static class ApiDefaultsSetup
{
    public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder)
    {
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });

        OpenApiTypeMappings.RegisterGuildSaberTypeMappings();

        builder.Services.AddOutputCache();
        builder.Services.AddOpenApi(options =>
        {
            options.AddGlobalProblemDetails()
                .AddSessionCookieSecurityScheme()
                .AddEndpointsHttpSecuritySchemeResolution()
                .AddTagDescriptionSupport()
                .AddScalarTransformers()
                .AddTypeTransformationSupport();
        });

        builder.Services.AddValidation();
        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            if (context.Exception is BadHttpRequestException badHttpRequestException)
            {
                context.ProblemDetails.Title = "Bad Request";
                context.ProblemDetails.Detail = badHttpRequestException.Message;
                context.ProblemDetails.Status = StatusCodes.Status400BadRequest;

                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }

            context.ProblemDetails.Instance =
                $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions
                .TryAdd("traceId", context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity.Id);
        });

        return builder;
    }
}