using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace GuildSaber.Api.Extensions;

public static class OpenApiBearerSecurityScheme
{
    internal sealed class BearerSecuritySchemeTransformer(
        IAuthenticationSchemeProvider authenticationSchemeProvider)
        : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(
            OpenApiDocument document, OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
            if (authenticationSchemes.All(authScheme => authScheme.Name != "Bearer"))
                return;

            var requirements = new Dictionary<string, OpenApiSecurityScheme>
            {
                ["Bearer"] = new()
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer", // "bearer" refers to the header name here
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;
        }
    }

    /// <summary>
    /// Adds support for Bearer security schemes in OpenAPI documents.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions" /> to add Bearer security scheme to.</param>
    /// <returns>The same <see cref="OpenApiOptions" /> returned to allow chaining.</returns>
    public static OpenApiOptions AddBearerSecurityScheme(this OpenApiOptions options)
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        return options;
    }
}