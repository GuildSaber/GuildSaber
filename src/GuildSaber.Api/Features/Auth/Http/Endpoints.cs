using System.Security.Claims;
using System.Web;
using AspNet.Security.OAuth.BeatLeader;
using AspNet.Security.OAuth.Discord;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Players.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Api.Transformers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace GuildSaber.Api.Features.Auth.Http;

public class AuthEndpoints : IEndpoints
{
    private const string BeatLeaderCallbackName = "BeatLeaderCallback";
    private const string DiscordCallbackName = "DiscordCallback";
    private const string DiscordLinkCallbackName = "DiscordLinkCallback";
    private const string DiscordLinkCallbackWithRedirectName = "DiscordLinkCallbackWithRedirect";
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

        group.MapGet("link/discord", HandleDiscordLink)
            .WithName("DiscordLink")
            .WithSummary("Initiate linking Discord from authentication flow.")
            .WithDescription("Initiate the Discord authentication flow to link Discord account"
                             + " to an existing authenticated user with optional callback path.")
            .RequireSession();

        group.MapGet("/callback/link/discord", HandleDiscordLinkCallbackAsync)
            .WithName(DiscordLinkCallbackName)
            .WithSummary("Link Discord account to existing authenticated user.")
            .WithDescription(
                "Handles the callback from Discord to link Discord account to existing authenticated user.")
            .Produces<RedirectHttpResult>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireSession();

        group.MapGet("/callback/link/discord/redirect", HandleDiscordLinkCallbackWithRedirectAsync)
            .WithName(DiscordLinkCallbackWithRedirectName)
            .WithSummary("Link Discord account to existing authenticated user with redirect.")
            .WithDescription("Handles the callback from Discord after authentication to link Discord account"
                             + " to an existing authenticated user and redirects to a specified path"
                             + " from the calling origin with ?type=discordLink&{error}&status as query params.")
            .Produces<RedirectHttpResult>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireSession();

        group.MapGet("/callback/beatleader", HandleBeatLeaderCallbackAsync)
            .WithName(BeatLeaderCallbackName)
            .WithSummary("Create a session cookie after authenticating with BeatLeader.")
            .WithDescription("Handles the callback from BeatLeader and sets the GuildSaber session cookie.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status423Locked);

        group.MapGet("/callback/discord", HandleDiscordCallbackAsync)
            .WithName(DiscordCallbackName)
            .WithSummary("Create a session cookie after authenticating with Discord.")
            .WithDescription("Handles the callback from Discord and sets the GuildSaber session cookie.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status423Locked);

        group.MapGet("/callback/discord/redirect", HandleDiscordCallbackWithRedirectAsync)
            .WithName(DiscordCallbackWithRedirectName)
            .WithSummary("Set the session cookie and redirect after Discord authentication.")
            .WithDescription("Handles the callback from Discord, sets the GuildSaber session cookie, and redirects"
                             + " with authentication status or error query parameters.")
            .Produces<RedirectHttpResult>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status423Locked);

        group.MapGet("/callback/beatleader/redirect", HandleBeatLeaderCallbackWithRedirectAsync)
            .WithName(BeatLeaderCallbackWithRedirectName)
            .WithSummary("Set the session cookie and redirect after BeatLeader authentication.")
            .WithDescription(
                "Handles the callback from BeatLeader, sets the GuildSaber session cookie, and redirects"
                + " with authentication status or error query parameters.")
            .Produces<RedirectHttpResult>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status423Locked);

        group.MapPost("/logout", HandleLogoutAsync)
            .WithName("Logout")
            .WithSummary("Log out the current user by invalidating their session.")
            .RequireSession();

        group.MapPost("/logout-all", HandleLogoutAllAsync)
            .WithName("LogoutAll")
            .WithSummary("Log out the current user from all sessions by invalidating all their sessions.")
            .RequireSession();

        group.MapPost("/logout/redirect", HandleLogoutWithRedirectAsync)
            .WithName("LogoutWithRedirect")
            .WithSummary("Log out the current user by invalidating their session and redirecting.")
            .RequireSession();

        group.MapPost("/logout-all/redirect", HandleLogoutAllWithRedirectAsync)
            .WithName("LogoutAllWithRedirect")
            .WithSummary(
                "Log out the current user from all sessions by invalidating all their sessions and redirecting.")
            .RequireSession();
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

    private static ChallengeHttpResult HandleDiscordLink(
        HttpContext httpContext, LinkGenerator linkGenerator, [FromQuery] string? returnUrl = null)
        => TypedResults.Challenge(new AuthenticationProperties
        {
            RedirectUri = returnUrl is null
                ? linkGenerator.GetPathByName(DiscordLinkCallbackName)
                : linkGenerator.GetPathByName(DiscordLinkCallbackWithRedirectName, new { returnUrl })
        }, [DiscordAuthenticationDefaults.AuthenticationScheme]);

    private static async Task<Results<RedirectHttpResult, ProblemHttpResult>> HandleDiscordLinkCallbackAsync(
        HttpContext httpContext, ClaimsPrincipal claimsPrincipal, AuthService authService)
    {
        var discordAuthResult =
            await AuthenticateAsync(httpContext, DiscordAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignOutAsync(AuthConstants.DiscordCookieScheme);
        if (!discordAuthResult.TryGetValue(out var discordAuthValue))
            return TypedResults.Problem("Authentication failed. Please ensure you are logged in with Discord.",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await DiscordLinkPipeline(
            authService, discordAuthValue.claimsPrincipal, claimsPrincipal.GetPlayerId()!.Value);
        if (result.TryGetError(out var error))
            return error;

        return TypedResults.Redirect("/");
    }

    private static async Task<Results<RedirectHttpResult, ProblemHttpResult>>
        HandleDiscordLinkCallbackWithRedirectAsync(
            HttpContext httpContext, ClaimsPrincipal claimsPrincipal, AuthService authService,
            [FromQuery] string returnUrl,
            IOptionsSnapshot<RedirectSettings> redirectSettings)
    {
        if (!IsValidRedirectUrl(returnUrl, redirectSettings.Value))
            return TypedResults.Problem("Invalid return URL. Please ensure the URL is allowed.",
                statusCode: StatusCodes.Status400BadRequest);

        var discordAuthResult =
            await AuthenticateAsync(httpContext, DiscordAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignOutAsync(AuthConstants.DiscordCookieScheme);
        if (!discordAuthResult.TryGetValue(out var discordAuthValue))
            return TypedResults.Problem("Authentication failed. Please ensure you are logged in with Discord.",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await DiscordLinkPipeline(
            authService, discordAuthValue.claimsPrincipal, claimsPrincipal.GetPlayerId()!.Value);
        return BuildCallbackRedirect(result, eventType: "discordLink", returnUrl);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleDiscordCallbackAsync(
        HttpContext httpContext, AuthService authService)
    {
        var discordAuthResult =
            await AuthenticateAsync(httpContext, DiscordAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignOutAsync(AuthConstants.DiscordCookieScheme);
        if (!discordAuthResult.TryGetValue(out var discordAuthValue))
            return TypedResults.Problem("Authentication failed. Please ensure you are logged in with Discord.",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await DiscordCallBackPipeline(httpContext, authService, discordAuthValue.claimsPrincipal);
        return result.Match(
            onSuccess: () => (Results<NoContent, ProblemHttpResult>)TypedResults.NoContent(),
            onFailure: error => (Results<NoContent, ProblemHttpResult>)error);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleBeatLeaderCallbackAsync(
        HttpContext httpContext, AuthService authService, IBackgroundTaskQueue taskQueue,
        IServiceScopeFactory scopeFactory)
    {
        var authResult = await AuthenticateAsync(httpContext, BeatLeaderAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignOutAsync(AuthConstants.BeatLeaderCookieScheme);
        if (!authResult.TryGetValue(out var authValue))
            return TypedResults.Problem("Authentication failed. Please ensure you are logged in with BeatLeader.",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await BeatLeaderCallBackPipeline(
            httpContext, authService, authValue.claimsPrincipal, taskQueue, scopeFactory);
        return result.Match(
            onSuccess: () => (Results<NoContent, ProblemHttpResult>)TypedResults.NoContent(),
            onFailure: error => (Results<NoContent, ProblemHttpResult>)error);
    }

    private static async Task<Results<RedirectHttpResult, ProblemHttpResult>> HandleDiscordCallbackWithRedirectAsync(
        HttpContext httpContext, AuthService authService, [FromQuery] string returnUrl,
        IOptionsSnapshot<RedirectSettings> redirectSettings)
    {
        if (!IsValidRedirectUrl(returnUrl, redirectSettings.Value))
            return TypedResults.Problem("Invalid return URL. Please ensure the URL is allowed.",
                statusCode: StatusCodes.Status400BadRequest);

        var authResult = await AuthenticateAsync(httpContext, DiscordAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignOutAsync(AuthConstants.DiscordCookieScheme);
        if (!authResult.TryGetValue(out var authValue))
            return TypedResults.Problem("Authentication failed. Please ensure you are logged in with Discord.",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await DiscordCallBackPipeline(httpContext, authService, authValue.claimsPrincipal);
        return BuildCallbackRedirect(result, eventType: "auth", returnUrl);
    }

    private static async Task<Results<RedirectHttpResult, ProblemHttpResult>> HandleBeatLeaderCallbackWithRedirectAsync(
        HttpContext httpContext, AuthService authService, [FromQuery] string returnUrl,
        IBackgroundTaskQueue taskQueue, IServiceScopeFactory scopeFactory,
        IOptionsSnapshot<RedirectSettings> redirectSettings)
    {
        if (!IsValidRedirectUrl(returnUrl, redirectSettings.Value))
            return TypedResults.Problem("Invalid return URL. Please ensure the URL is allowed.",
                statusCode: StatusCodes.Status400BadRequest);

        var authResult = await AuthenticateAsync(httpContext, BeatLeaderAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignOutAsync(AuthConstants.BeatLeaderCookieScheme);
        if (!authResult.TryGetValue(out var authValue))
            return TypedResults.Problem("Authentication failed. Please ensure you are logged in with BeatLeader.",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await BeatLeaderCallBackPipeline(httpContext, authService, authValue.claimsPrincipal,
            taskQueue, scopeFactory);
        return BuildCallbackRedirect(result, eventType: "auth", returnUrl);
    }

    private static RedirectHttpResult BuildCallbackRedirect(
        UnitResult<ProblemHttpResult> result, string eventType, string returnUrl)
    {
        var uriBuilder = new UriBuilder(returnUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        query["type"] = eventType;
        query["status"] = result.IsSuccess ? "200" : result.Error.ProblemDetails.Status.ToString();
        result.Match(
            onSuccess: () => { },
            onFailure: problem => query["error"] = HttpUtility.UrlEncode(problem.ProblemDetails.Detail)
        );
        uriBuilder.Query = query.ToString();
        return TypedResults.Redirect(uriBuilder.ToString());
    }

    private static async Task<UnitResult<ProblemHttpResult>> DiscordCallBackPipeline(
        HttpContext httpContext, AuthService authService, ClaimsPrincipal discordClaims)
        => await DiscordId.TryParse(discordClaims.FindFirstValue(ClaimTypes.NameIdentifier))
            .MapError(_ => TypedResults.Problem("Failed to parse Discord ID from authentication claims.",
                statusCode: StatusCodes.Status400BadRequest))
            .Bind(discordId => authService
                .GetPlayerIdAsync(discordId)
                .ToResult(() => TypedResults.Problem(
                    "Discord account is not linked to a player. Log in with BeatLeader first, then link Discord.",
                    statusCode: StatusCodes.Status422UnprocessableEntity)))
            .Bind(playerId => authService
                .CreateSession(playerId, httpContext)
                .MapError(MapSessionCreationErrorResponse));

    private static async Task<UnitResult<ProblemHttpResult>> DiscordLinkPipeline(
        AuthService authService, ClaimsPrincipal discordClaims, PlayerId playerId)
        => await DiscordId.TryParse(discordClaims.FindFirstValue(ClaimTypes.NameIdentifier))
            .MapError(_ => TypedResults.Problem("Failed to parse Discord ID from authentication claims.",
                statusCode: StatusCodes.Status400BadRequest))
            .Bind(async discordId => await authService.LinkDiscordIdAsync(playerId, discordId)
                ? UnitResult.Success<ProblemHttpResult>()
                : Failure(TypedResults.Problem(
                    "Failed to link Discord account to player.",
                    statusCode: StatusCodes.Status500InternalServerError)));

    private static async Task<UnitResult<ProblemHttpResult>> BeatLeaderCallBackPipeline(
        HttpContext httpContext, AuthService authService, ClaimsPrincipal claimsPrincipal,
        IBackgroundTaskQueue taskQueue, IServiceScopeFactory scopeFactory)
        => await BeatLeaderId.TryParse(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier))
            .MapError(_ => TypedResults.Problem("Failed to parse BeatLeaderId from authentication claims.",
                statusCode: StatusCodes.Status400BadRequest))
            .Bind(beatleaderId => authService
                .GetPlayerIdAsync(beatleaderId)
                .Tap(playerId => authService.UpdatePlayerInfoAsync(playerId, beatleaderId))
                .ToResult(() => "Treating as result")
                .Compensate(_ => authService
                    .CreatePlayerAsync(beatleaderId)
                    .Map(static async (player, state) =>
                    {
                        await state.taskQueue.QueueBackgroundWorkItemAsync(async token =>
                        {
                            await using var scope = state.scopeFactory.CreateAsyncScope();
                            var pipeline = scope.ServiceProvider.GetRequiredService<PlayerScoresPipeline>();

                            await pipeline.ImportBeatLeaderScoresAsync(
                                player.Id, player.LinkedAccounts.BeatLeaderId(), token
                            );

                            if (player.LinkedAccounts.ScoreSaberId is { } scoreSaberId)
                                await pipeline.ImportScoreSaberScoresAsync(player.Id, scoreSaberId, token);
                        });

                        return player.Id;
                    }, (taskQueue, scopeFactory))
                    .MapError(error => TypedResults.Problem(error.ToString(),
                        statusCode: StatusCodes.Status422UnprocessableEntity))))
            .Bind(playerId => authService
                .CreateSession(playerId, httpContext)
                .MapError(MapSessionCreationErrorResponse));

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
        => OriginPolicy.IsAllowed(returnUrl, redirectSettings.AllowedOriginUrls);

    private static async Task<NoContent> HandleLogoutAsync(
        HttpContext httpContext, ClaimsPrincipal claimsPrincipal, AuthService authService)
    {
        await authService.InvalidateSessionAsync(claimsPrincipal.GetSessionId()!.Value);
        authService.DeleteSessionCookie(httpContext.Response);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> HandleLogoutAllAsync(
        HttpContext httpContext, ClaimsPrincipal claimsPrincipal, AuthService authService)
    {
        await authService.InvalidateAllSessionsAsync(claimsPrincipal.GetPlayerId()!.Value);
        authService.DeleteSessionCookie(httpContext.Response);
        return TypedResults.NoContent();
    }

    private static async Task<Results<RedirectHttpResult, ProblemHttpResult>> HandleLogoutWithRedirectAsync(
        HttpContext httpContext, ClaimsPrincipal claimsPrincipal, AuthService authService,
        IOptionsSnapshot<RedirectSettings> redirectSettings, [FromQuery] string returnUrl)
    {
        if (!IsValidRedirectUrl(returnUrl, redirectSettings.Value))
            return TypedResults.Problem("Invalid return URL. Please ensure the URL is allowed.",
                statusCode: StatusCodes.Status400BadRequest);

        await authService.InvalidateSessionAsync(claimsPrincipal.GetSessionId()!.Value);
        authService.DeleteSessionCookie(httpContext.Response);

        return TypedResults.Redirect(returnUrl);
    }

    private static async Task<Results<RedirectHttpResult, ProblemHttpResult>> HandleLogoutAllWithRedirectAsync(
        HttpContext httpContext, ClaimsPrincipal claimsPrincipal, AuthService authService,
        IOptionsSnapshot<RedirectSettings> redirectSettings, [FromQuery] string returnUrl)
    {
        if (!IsValidRedirectUrl(returnUrl, redirectSettings.Value))
            return TypedResults.Problem("Invalid return URL. Please ensure the URL is allowed.",
                statusCode: StatusCodes.Status400BadRequest);

        await authService.InvalidateAllSessionsAsync(claimsPrincipal.GetPlayerId()!.Value);
        authService.DeleteSessionCookie(httpContext.Response);

        return TypedResults.Redirect(returnUrl);
    }
}