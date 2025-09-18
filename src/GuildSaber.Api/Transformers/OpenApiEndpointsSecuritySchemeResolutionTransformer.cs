using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GuildSaber.Api.Transformers;

public static class OpenApiEndpointsSecuritySchemeResolutionTransformer
{
    internal sealed class EndpointsHttpSecuritySchemeResolutionTransformer(
        IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiOperationTransformer
    {
        private static string? _defaultSchemeName;

        public async Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            var authorizeAttribute = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<AuthorizeAttribute>()
                .FirstOrDefault();

            if (authorizeAttribute is null)
                return;

            var targetSchemes = authorizeAttribute.AuthenticationSchemes?.Split(',');
            if (targetSchemes is null)
            {
                _defaultSchemeName ??= (await authenticationSchemeProvider.GetDefaultAuthenticateSchemeAsync())?.Name;
                ArgumentException.ThrowIfNullOrWhiteSpace(
                    _defaultSchemeName,
                    "No default authentication scheme found while one was expected."
                );

                operation.Security ??= new List<OpenApiSecurityRequirement>(1);
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(_defaultSchemeName, context.Document)] = []
                });

                return;
            }

            operation.Security ??= new List<OpenApiSecurityRequirement>(targetSchemes.Length);
            foreach (var scheme in targetSchemes.Select(x => x.Trim()))
            {
                ArgumentException.ThrowIfNullOrEmpty(
                    scheme,
                    "Encountered an empty authentication scheme while processing AuthorizeAttribute."
                );
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(scheme, context.Document)] = []
                });
            }
        }
    }

    /// <summary>
    /// Adds the security requirement for operations that have the <see cref="AuthorizeAttribute" /> applied.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions" /> to add endpoints security scheme resolution to.</param>
    /// <returns>The same <see cref="OpenApiOptions" /> returned to allow chaining.</returns>
    public static OpenApiOptions AddEndpointsHttpSecuritySchemeResolution(this OpenApiOptions options)
        => options.AddOperationTransformer<EndpointsHttpSecuritySchemeResolutionTransformer>();
}