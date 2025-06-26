using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace GuildSaber.Api.Transformers;

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
                [JwtBearerDefaults.AuthenticationScheme] = new()
                {
                    Type = SecuritySchemeType.Http,
                    Name = JwtBearerDefaults.AuthenticationScheme,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = JwtBearerDefaults.AuthenticationScheme
                    }
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
        => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
}