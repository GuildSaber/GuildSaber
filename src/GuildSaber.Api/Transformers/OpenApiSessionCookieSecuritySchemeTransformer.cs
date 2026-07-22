using GuildSaber.Api.Features.Auth.Sessions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GuildSaber.Api.Transformers;

public static class OpenApiSessionCookieSecuritySchemeTransformer
{
    internal sealed class SessionCookieSecuritySchemeTransformer(IHostEnvironment environment)
        : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(
            OpenApiDocument document, OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[SessionCookieDefaults.AuthenticationScheme] =
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = environment.IsDevelopment()
                        ? SessionCookieDefaults.DevelopmentCookieName
                        : SessionCookieDefaults.ProductionCookieName,
                    In = ParameterLocation.Cookie,
                    Description =
                        "GuildSaber session cookie. Browsers and cookie-enabled HTTP clients send it automatically."
                };

            return Task.CompletedTask;
        }
    }

    public static OpenApiOptions AddSessionCookieSecurityScheme(this OpenApiOptions options)
        => options.AddDocumentTransformer<SessionCookieSecuritySchemeTransformer>();
}