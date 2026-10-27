using GuildSaber.Api.Features.Auth.CustomApiKey;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GuildSaber.Api.Transformers;

public static class OpenApiBasicSecuritySchemeTransformer
{
    internal sealed class BasicSecuritySchemeTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(
            OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[BasicAuthenticationDefaults.AuthenticationScheme] =
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic",
                    Description = "Trusted API clients: Discord ID as username and the API key as password."
                };

            return Task.CompletedTask;
        }
    }

    public static OpenApiOptions AddBasicSecurityScheme(this OpenApiOptions options)
        => options.AddDocumentTransformer<BasicSecuritySchemeTransformer>();
}