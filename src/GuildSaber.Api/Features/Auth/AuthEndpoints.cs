using System.Security.Claims;
using AspNet.Security.OAuth.BeatLeader;
using AspNet.Security.OAuth.Discord;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Extensions;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using static GuildSaber.Api.Features.Auth.AuthResponse;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace GuildSaber.Api.Features.Auth;

public class AuthEndpoints : IEndPoints
{
    private const string BeatLeaderCallbackName = "BeatLeaderCallback";
    private const string DiscordCallbackName = "DiscordCallback";

    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth")
            .WithTag("Auth", description: "Authentication endpoints");

        group.MapGet("/login/beatleader", HandleBeatLeaderLogin)
            .WithName("BeatLeaderLogin")
            .WithSummary("Initiate the BeatLeader authentication flow.");

        group.MapGet("login/discord", HandleDiscordLogin)
            .WithName("DiscordLogin")
            .WithSummary("Initiate the Discord authentication flow.");

        group.MapGet("/callback/beatleader", HandleBeatLeaderLoginCallbackAsync)
            .WithName(BeatLeaderCallbackName)
            .WithSummary("Handle the callback from BeatLeader after authentication.")
            .Produces<TokenResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<string>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ProblemHttpResult>(StatusCodes.Status429TooManyRequests)
            .Produces<ProblemHttpResult>(StatusCodes.Status423Locked);

        group.MapGet("/callback/discord", HandleDiscordCallbackAsync)
            .WithName(DiscordCallbackName)
            .WithSummary("Handle the callback from Discord after authentication.")
            .Produces<TokenResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<string>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ProblemHttpResult>(StatusCodes.Status429TooManyRequests)
            .Produces<ProblemHttpResult>(StatusCodes.Status423Locked);

        group.MapPost("/logout", HandleLogoutAsync)
            .WithName("Logout")
            .WithSummary("Log out the current user.");
    }

    private static ChallengeHttpResult HandleBeatLeaderLogin(HttpContext httpContext, LinkGenerator linkGenerator)
        => TypedResults.Challenge(new AuthenticationProperties
        {
            RedirectUri = linkGenerator.GetPathByName(BeatLeaderCallbackName)
        }, [BeatLeaderAuthenticationDefaults.AuthenticationScheme]);

    private static ChallengeHttpResult HandleDiscordLogin(HttpContext httpContext, LinkGenerator linkGenerator)
        => TypedResults.Challenge(new AuthenticationProperties
        {
            RedirectUri = linkGenerator.GetPathByName(DiscordCallbackName)
        }, [DiscordAuthenticationDefaults.AuthenticationScheme]);

    private static async Task<IResult> HandleBeatLeaderLoginCallbackAsync(
        HttpContext httpContext, AuthService authService)
        => await AuthenticateAsync(httpContext,
                BeatLeaderAuthenticationDefaults.AuthenticationScheme)
            .MapError(_ => Results.Unauthorized())
            .Bind(claimsPrincipal => BeatLeaderId
                .TryParseUnsafe(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier))
                .MapError(Results.BadRequest))
            .Bind(beatLeaderId => authService
                .GetPlayerIdAsync(beatLeaderId)
                .ToResult(() => "Treating as result")
                .Compensate(_ => authService
                    .CreateUserAsync(beatLeaderId)
                    .MapError(Results.UnprocessableEntity)))
            .Bind(playerId => authService
                .CreateSession(playerId, httpContext)
                .MapError(error => error switch
                {
                    TooManyOpenSession(var count, var maxCount) => Results.Problem(
                        $"You have too many active sessions ({count}/{maxCount}). Please log out from other devices or wait for other sessions to expire.",
                        title: "Too many active sessions",
                        statusCode: StatusCodes.Status429TooManyRequests),
                    AccountLocked => Results.Problem(
                        "Your account is locked. If you haven't initiated this action, please contact support.",
                        statusCode: StatusCodes.Status423Locked),
                    _ => Results.UnprocessableEntity("Failed to create session.")
                }))
            .Match(
                token => TypedResults.Ok(new TokenResponse(token)),
                error => error
            );


    private static async Task<IResult> HandleDiscordCallbackAsync(
        HttpContext httpContext, AuthService authService)
        => await AuthenticateAsync(httpContext,
                DiscordAuthenticationDefaults.AuthenticationScheme)
            .MapError(_ => Results.Unauthorized())
            .Bind(claimsPrincipal => DiscordId
                .TryParse(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier))
                .MapError(Results.InternalServerError))
            .Bind(beatLeaderId => authService
                .GetPlayerIdAsync(beatLeaderId)
                .ToResult(() => Results.UnprocessableEntity("Discord account is not linked to any player.")))
            .Bind(playerId => authService
                .CreateSession(playerId, httpContext)
                .MapError(error => error switch
                {
                    TooManyOpenSession(var count, var maxCount) => Results.Problem(
                        $"You have too many active sessions ({count}/{maxCount}). Please log out from other devices or wait for other sessions to expire.",
                        title: "Too many active sessions",
                        statusCode: StatusCodes.Status429TooManyRequests),
                    AccountLocked => Results.Problem(
                        "Your account is locked. If you haven't initiated this action, please contact support.",
                        statusCode: StatusCodes.Status423Locked),
                    _ => Results.UnprocessableEntity("Failed to create session.")
                }))
            .Match(
                token => TypedResults.Ok(new TokenResponse(token)),
                error => error
            );

    private static async Task<Result<ClaimsPrincipal>> AuthenticateAsync(HttpContext httpContext, string scheme)
        => await httpContext.AuthenticateAsync(scheme) switch
        {
            { Succeeded: false } => Failure<ClaimsPrincipal>("Authentication failed."),
            { Principal: null } => Failure<ClaimsPrincipal>("Authentication principal is null."),
            { Principal: var principal } => Success(principal)
        };

    private static IResult HandleLogoutAsync(HttpContext httpContext)
        => TypedResults.SignOut(new AuthenticationProperties
        {
            RedirectUri = httpContext.Request.PathBase + "/"
        }, [JwtBearerDefaults.AuthenticationScheme]);
}