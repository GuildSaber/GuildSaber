using GuildSaber.Api.Features.Auth.Sessions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GuildSaber.Api.Transformers;

public static class OpenApiSessionCookieRequestVerificationTransformer
{
    internal sealed class RequestVerificationSchemeTransformer :
        IOpenApiDocumentTransformer,
        IOpenApiOperationTransformer
    {
        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[SessionCookieDefaults.RequestVerificationHeaderName] =
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = SessionCookieDefaults.RequestVerificationHeaderName,
                    In = ParameterLocation.Header,
                    Description = "Required for state-changing requests authenticated with the session cookie."
                };

            return Task.CompletedTask;
        }

        public Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            if (!RequiresVerification(context.Description.HttpMethod) || operation.Security is null)
                return Task.CompletedTask;

            foreach (var requirement in operation.Security.Where(IsSessionCookieRequirement))
                requirement[new OpenApiSecuritySchemeReference(
                    SessionCookieDefaults.RequestVerificationHeaderName,
                    context.Document)] = [];

            return Task.CompletedTask;
        }

        private static bool IsSessionCookieRequirement(OpenApiSecurityRequirement requirement)
            => requirement.Keys.Any(x => x.Reference.Id == SessionCookieDefaults.AuthenticationScheme);

        private static bool RequiresVerification(string? method)
            => method is not null &&
               !HttpMethods.IsGet(method) &&
               !HttpMethods.IsHead(method) &&
               !HttpMethods.IsOptions(method) &&
               !HttpMethods.IsTrace(method);
    }

    public static OpenApiOptions AddSessionCookieRequestVerification(this OpenApiOptions options) => options
        .AddDocumentTransformer<RequestVerificationSchemeTransformer>()
        .AddOperationTransformer<RequestVerificationSchemeTransformer>();
}