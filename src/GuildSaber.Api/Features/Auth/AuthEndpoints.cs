using System.Security.Claims;
using System.Web;
using AspNet.Security.OAuth.BeatLeader;
using AspNet.Security.OAuth.Discord;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Settings;
using GuildSaber.Api.Transformers;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using static GuildSaber.Api.Features.Auth.AuthResponse;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace GuildSaber.Api.Features.Auth;

public class AuthEndpoints : IEndpoints
{
    private const string BeatLeaderCallbackName = "BeatLeaderCallback";
    private const string DiscordCallbackName = "DiscordCallback";
    private const string BeatLeaderCallbackWithRedirectName = "BeatLeaderCallbackWithRedirect";
    private const string DiscordCallbackWithRedirectName = "DiscordCallbackWithRedirect";

    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth")
            .WithTag("Auth", description: "Authentication endpoints");

        group.MapGet("/login/beatleader", HandleBeatLeaderLogin)
            .WithName("BeatLeaderLogin")
            .WithSummary("Login with BeatLeader")
            .WithDescription("Initiate the BeatLeader authentication flow with optional callback path.");

        group.MapGet("login/discord", HandleDiscordLogin)
            .WithName("DiscordLogin")
            .WithSummary("Login with Discord")
            .WithDescription("Initiate the Discord authentication flow with optional callback path.");

        group.MapGet("/callback/beatleader", HandleBeatLeaderCallbackAsync)
            .WithName(BeatLeaderCallbackName)
            .WithSummary("Get session token after authenticating with BeatLeader.")
            .WithDescription("Handles the callback from BeatLeader after authentication and returns a token.")
            .Produces<TokenResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status423Locked);

        group.MapGet("/callback/discord", HandleDiscordCallbackAsync)
            .WithName(DiscordCallbackName)
            .WithSummary("Get session token after authenticating with Discord.")
            .WithDescription("Handles the callback from Discord after authentication and returns a token.")
            .Produces<TokenResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status423Locked);

        group.MapGet("/callback/discord/redirect", HandleDiscordCallbackWithRedirectAsync)
            .WithName(DiscordCallbackWithRedirectName)
            .WithSummary("Redirect with session token or error after Discord authentication.")
            .WithDescription("Handles the callback from Discord after authentication and redirects to a specified path"
                             + " from the calling origin with ?{token/error}&status as query params.")
            .Produces<RedirectHttpResult>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status423Locked);

        group.MapGet("/callback/beatleader/redirect", HandleBeatLeaderCallbackWithRedirectAsync)
            .WithName(BeatLeaderCallbackWithRedirectName)
            .WithSummary("Redirect with session token or error after BeatLeader authentication.")
            .WithDescription(
                "Handles the callback from BeatLeader after authentication and redirects to a specified path"
                + " from the calling origin with ?{token/error}&status as query params.")
            .Produces<RedirectHttpResult>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status423Locked);

        group.MapPost("/logout", HandleLogoutAsync)
            .WithName("Logout")
            .WithSummary("Log out the current user.")
            .RequireAuthorization();
    }

    private static ChallengeHttpResult HandleBeatLeaderLogin(
        HttpContext httpContext, LinkGenerator linkGenerator, [FromQuery] string? returnUrl = null)
        => TypedResults.Challenge(new AuthenticationProperties
        {
            RedirectUri = returnUrl is null
                ? linkGenerator.GetPathByName(BeatLeaderCallbackName)
                : linkGenerator.GetPathByName(BeatLeaderCallbackWithRedirectName, new { returnUrl })
        }, [BeatLeaderAuthenticationDefaults.AuthenticationScheme]);

    private static ChallengeHttpResult HandleDiscordLogin(
        HttpContext httpContext, LinkGenerator linkGenerator, [FromQuery] string? returnUrl = null)
        => TypedResults.Challenge(new AuthenticationProperties
        {
            RedirectUri = returnUrl is null
                ? linkGenerator.GetPathByName(DiscordCallbackName)
                : linkGenerator.GetPathByName(DiscordCallbackWithRedirectName, new { returnUrl })
        }, [DiscordAuthenticationDefaults.AuthenticationScheme]);

    private static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> HandleDiscordCallbackAsync(
        HttpContext httpContext, AuthService authService)
    {
        var authResult = await AuthenticateAsync(httpContext, DiscordAuthenticationDefaults.AuthenticationScheme);
        if (!authResult.TryGetValue(out var authValue))
            return TypedResults.Problem("Authentication failed. Please ensure you are logged in with Discord.",
                statusCode: StatusCodes.Status401Unauthorized);

        return await DiscordCallBackPipeline(httpContext, authService, authValue.claimsPrincipal)
            .Match(token => token, error => (Results<Ok<TokenResponse>, ProblemHttpResult>)error);
    }

    private static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> HandleBeatLeaderCallbackAsync(
        HttpContext httpContext, AuthService authService)
    {
        var authResult = await AuthenticateAsync(httpContext, BeatLeaderAuthenticationDefaults.AuthenticationScheme);
        if (!authResult.TryGetValue(out var authValue))
            return TypedResults.Problem("Authentication failed. Please ensure you are logged in with BeatLeader.",
                statusCode: StatusCodes.Status401Unauthorized);

        return await BeatLeaderCallBackPipeline(httpContext, authService, authValue.claimsPrincipal)
            .Match(token => token, error => (Results<Ok<TokenResponse>, ProblemHttpResult>)error);
    }

    private static async Task<Results<RedirectHttpResult, ProblemHttpResult>> HandleDiscordCallbackWithRedirectAsync(
        HttpContext httpContext, AuthService authService, [FromQuery] string returnUrl,
        IOptionsSnapshot<RedirectSettings> redirectSettings)
    {
        if (!IsValidRedirectUrl(returnUrl, redirectSettings.Value))
            return TypedResults.Problem("Invalid return URL. Please ensure the URL is allowed.",
                statusCode: StatusCodes.Status400BadRequest);

        var authResult = await AuthenticateAsync(httpContext, DiscordAuthenticationDefaults.AuthenticationScheme);
        if (!authResult.TryGetValue(out var authValue))
            return TypedResults.Problem("Authentication failed. Please ensure you are logged in with Discord.",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await DiscordCallBackPipeline(httpContext, authService, authValue.claimsPrincipal);
        return BuildCallbackRedirect(result, returnUrl);
    }

    private static async Task<Results<RedirectHttpResult, ProblemHttpResult>> HandleBeatLeaderCallbackWithRedirectAsync(
        HttpContext httpContext, AuthService authService, [FromQuery] string returnUrl,
        IOptionsSnapshot<RedirectSettings> redirectSettings)
    {
        if (!IsValidRedirectUrl(returnUrl, redirectSettings.Value))
            return TypedResults.Problem("Invalid return URL. Please ensure the URL is allowed.",
                statusCode: StatusCodes.Status400BadRequest);

        var authResult = await AuthenticateAsync(httpContext, BeatLeaderAuthenticationDefaults.AuthenticationScheme);
        if (!authResult.TryGetValue(out var authValue))
            return TypedResults.Problem("Authentication failed. Please ensure you are logged in with BeatLeader.",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await BeatLeaderCallBackPipeline(httpContext, authService, authValue.claimsPrincipal);
        return BuildCallbackRedirect(result, returnUrl);
    }

    private static RedirectHttpResult BuildCallbackRedirect(
        Result<Ok<TokenResponse>, ProblemHttpResult> result, string returnUrl)
    {
        var uriBuilder = new UriBuilder(returnUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        query["status"] = result.IsSuccess ? "200" : result.Error.ProblemDetails.Status.ToString();
        result.Match(
            onSuccess: token => query["token"] = HttpUtility.UrlEncode(token.Value.Token),
            onFailure: problem => query["error"] = HttpUtility.UrlEncode(problem.ProblemDetails.Detail)
        );
        uriBuilder.Query = query.ToString();

        return TypedResults.Redirect(uriBuilder.ToString());
    }

    private static async Task<Result<Ok<TokenResponse>, ProblemHttpResult>> DiscordCallBackPipeline(
        HttpContext httpContext, AuthService authService, ClaimsPrincipal claimsPrincipal)
        => await DiscordId.TryParse(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier))
            .MapError(_ => TypedResults.Problem("Failed to parse Discord ID from authentication claims.",
                statusCode: StatusCodes.Status400BadRequest))
            .Bind(discordId => authService
                .GetPlayerIdAsync(discordId)
                .ToResult(() => TypedResults.Problem(
                    "Discord account is not linked to any player.",
                    statusCode: StatusCodes.Status422UnprocessableEntity)))
            .Bind(playerId => authService
                .CreateSession(playerId, httpContext)
                .MapError(MapSessionCreationErrorResponse))
            .Map(token => TypedResults.Ok(new TokenResponse(token)));

    private static async Task<Result<Ok<TokenResponse>, ProblemHttpResult>> BeatLeaderCallBackPipeline(
        HttpContext httpContext, AuthService authService, ClaimsPrincipal claimsPrincipal)
        => await BeatLeaderId.TryParseUnsafe(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier))
            .MapError(_ => TypedResults.Problem("Failed to parse BeatLeaderId from authentication claims.",
                statusCode: StatusCodes.Status400BadRequest))
            .Bind(beatleaderId => authService
                .GetPlayerIdAsync(beatleaderId)
                .ToResult(() => "Treating as result")
                .Compensate(_ => authService
                    .CreateUserAsync(beatleaderId)
                    .MapError(error => TypedResults.Problem(error.ToString(),
                        statusCode: StatusCodes.Status422UnprocessableEntity))))
            .Bind(playerId => authService
                .CreateSession(playerId, httpContext)
                .MapError(MapSessionCreationErrorResponse))
            .Map(token => TypedResults.Ok(new TokenResponse(token)));

    private static ProblemHttpResult MapSessionCreationErrorResponse(SessionCreationError error) => error switch
    {
        TooManyOpenSession(var count, var maxCount) => TypedResults.Problem(
            $"You have too many active sessions ({count}/{maxCount}). Please log out from other devices or wait for other sessions to expire.",
            title: "Too many active sessions",
            statusCode: StatusCodes.Status429TooManyRequests),
        AccountLocked => TypedResults.Problem(
            "Your account is locked. If you haven't initiated this action, please contact support.",
            statusCode: StatusCodes.Status423Locked),
        _ => TypedResults.Problem("Failed to create session.",
            statusCode: StatusCodes.Status500InternalServerError)
    };

    private static async Task<Result<(ClaimsPrincipal claimsPrincipal, AuthenticationProperties authProperties)>>
        AuthenticateAsync(HttpContext httpContext, string scheme)
        => await httpContext.AuthenticateAsync(scheme) switch
        {
            { Succeeded: false }
                => Failure<(ClaimsPrincipal, AuthenticationProperties)>("Authentication failed."),
            { Principal: null }
                => Failure<(ClaimsPrincipal, AuthenticationProperties)>("Authentication principal is null."),
            { Principal: var principal, Properties: var properties }
                => Success((principal, properties))
        };

    private static bool IsValidRedirectUrl(string returnUrl, RedirectSettings redirectSettings)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
            return false;

        var returnOrigin = uri.GetLeftPart(UriPartial.Authority);

        return redirectSettings.AllowedOriginUrls
            .Any(origin => string.Equals(origin, returnOrigin, StringComparison.OrdinalIgnoreCase));
    }

    private static IResult HandleLogoutAsync(HttpContext httpContext) => throw new NotImplementedException();
}