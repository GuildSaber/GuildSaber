using GuildSaber.Api.Features.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GuildSaber.Api.Transformers;

public static class OpenApiEndpointsSecuritySchemeResolutionTransformer
{
    internal sealed class EndpointsHttpSecuritySchemeResolutionTransformer(
        IAuthenticationSchemeProvider authenticationSchemeProvider,
        IAuthorizationPolicyProvider authorizationPolicyProvider) : IOpenApiOperationTransformer
    {
        public async Task TransformAsync(
            OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;
            if (metadata.OfType<IAllowAnonymous>().Any()
                || !metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<AuthorizationPolicy>().Any())
                return;

            var policy = await AuthorizationPolicy.CombineAsync(
                authorizationPolicyProvider,
                metadata.OfType<IAuthorizeData>(),
                metadata.OfType<AuthorizationPolicy>()
            );

            var schemes = policy?.AuthenticationSchemes;
            if (schemes is not { Count: > 0 })
                schemes =
                [
                    (await authenticationSchemeProvider.GetDefaultAuthenticateSchemeAsync())?.Name
                    ?? throw new InvalidOperationException(
                        "No default authentication scheme found while one was expected.")
                ];

            operation.Security ??= new List<OpenApiSecurityRequirement>(schemes.Count);
            if (metadata.OfType<IAuthorizeData>().All(x => x.Policy == AuthConstants.OptionalAuthenticationPolicy)
                && !metadata.OfType<AuthorizationPolicy>().Any())
                operation.Security.Add(new OpenApiSecurityRequirement());

            foreach (var scheme in schemes)
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(scheme, context.Document)] = []
                });
        }
    }

    /// <summary>
    /// Adds security requirements from the endpoint's named or inline authorization policies.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions" /> to add endpoints security scheme resolution to.</param>
    /// <returns>The same <see cref="OpenApiOptions" /> returned to allow chaining.</returns>
    public static OpenApiOptions AddEndpointsHttpSecuritySchemeResolution(this OpenApiOptions options)
        => options.AddOperationTransformer<EndpointsHttpSecuritySchemeResolutionTransformer>();
}