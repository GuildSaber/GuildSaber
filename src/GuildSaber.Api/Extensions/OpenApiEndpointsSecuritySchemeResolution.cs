using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace GuildSaber.Api.Extensions;

public static class OpenApiEndpointsSecuritySchemeResolution
{
    internal sealed class EndpointsSecuritySchemeResolutionTransformer(
        IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(
            OpenApiDocument document, OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            var defaultScheme = await authenticationSchemeProvider.GetDefaultAuthenticateSchemeAsync();

            foreach (var operation in document.Paths.Values.SelectMany(pathItem => pathItem.Operations.Values))
            {
                if (!TryGetAuthorizeAttribute(operation, context, out var authorizeAttribute))
                    continue;

                var targetScheme = authorizeAttribute.AuthenticationSchemes;
                targetScheme ??= defaultScheme?.Name;

                if (targetScheme == null)
                    continue;

                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = targetScheme
                        }
                    }] = []
                });
            }
        }

        private static bool TryGetAuthorizeAttribute(
            OpenApiOperation operation,
            OpenApiDocumentTransformerContext context,
            [NotNullWhen(true)] out AuthorizeAttribute? authorizeAttribute)
        {
            if (!operation.Annotations.TryGetValue("x-aspnetcore-id", out var aspNetCoreId))
            {
                authorizeAttribute = null;
                return false;
            }

            var id = aspNetCoreId as string;
            authorizeAttribute = context.DescriptionGroups
                .SelectMany(group => group.Items)
                .Where(endpoint => endpoint.ActionDescriptor.Id == id)
                .Select(endpoint => endpoint.ActionDescriptor.EndpointMetadata
                    .OfType<AuthorizeAttribute>()
                    .FirstOrDefault())
                .FirstOrDefault();

            return authorizeAttribute != null;
        }
    }

    /// <summary>
    /// Adds the security requirement for operations that have the <see cref="AuthorizeAttribute" /> applied.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions" /> to add endpoints security scheme resolution to.</param>
    /// <returns>The same <see cref="OpenApiOptions" /> returned to allow chaining.</returns>
    public static OpenApiOptions AddEndpointsSecuritySchemeResolution(this OpenApiOptions options)
    {
        options.AddDocumentTransformer<EndpointsSecuritySchemeResolutionTransformer>();
        return options;
    }
}