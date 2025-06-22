using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace GuildSaber.Api.Extensions;

public static class OpenApiEndpointsSecuritySchemeResolution
{
    internal sealed class EndpointsHttpSecuritySchemeResolutionTransformer(
        IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiOperationTransformer
    {
        private static string? _defaultSchemeName;

        public async Task TransformAsync(
            OpenApiOperation operation, OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            var authorizeAttribute = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<AuthorizeAttribute>()
                .FirstOrDefault();

            if (authorizeAttribute is null)
                return;

            var targetScheme = authorizeAttribute.AuthenticationSchemes?.Split(',')
                .FirstOrDefault();

            if (string.IsNullOrEmpty(targetScheme))
            {
                _defaultSchemeName ??=
                    (await authenticationSchemeProvider
                        .GetDefaultAuthenticateSchemeAsync())?.Name
                    ?? throw new InvalidOperationException(
                        "No default authentication scheme found.");

                targetScheme = _defaultSchemeName;
            }

            operation.Security ??= new List<OpenApiSecurityRequirement>(1);
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = targetScheme
                    }
                }] = []
            });
        }
    }

    /// <summary>
    /// Adds the security requirement for operations that have the <see cref="AuthorizeAttribute" /> applied.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions" /> to add endpoints security scheme resolution to.</param>
    /// <returns>The same <see cref="OpenApiOptions" /> returned to allow chaining.</returns>
    public static OpenApiOptions AddEndpointsHttpSecuritySchemeResolution(this OpenApiOptions options)
    {
        options.AddOperationTransformer<EndpointsHttpSecuritySchemeResolutionTransformer>();
        return options;
    }
}