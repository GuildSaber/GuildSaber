using System.Collections.Frozen;
using System.Linq.Expressions;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Hardware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Npgsql;

namespace GuildSaber.Api.Features.Scores.Services;

public sealed class ControllerResolverService(
    HybridCache cache,
    IServiceScopeFactory scopeFactory,
    ILogger<ControllerResolverService> logger)
{
    private const string BlCacheKeyPrefix = "bl_controller_";
    private const string SsCacheKeyPrefix = "ss_controller_";

    private static readonly FrozenDictionary<BeatLeaderControllerId,
        (string BeatLeaderKey, string[] ScoreSaberKeys, string Name)> _aliases =
        new Dictionary<BeatLeaderControllerId, (string BeatLeaderKey, string[] ScoreSaberKeys, string Name)>
        {
            [BeatLeaderControllerId.CreateUnsafe(0).Value] = ("unknown", ["Unknown"], "Unknown"),
            [BeatLeaderControllerId.CreateUnsafe(1).Value] = ("oculustouch", ["Rift CV1"], "Oculus Touch"),
            [BeatLeaderControllerId.CreateUnsafe(2).Value] = ("vive", ["Vive"], "Vive Wands"),
            [BeatLeaderControllerId.CreateUnsafe(16).Value] =
                ("oculustouch2", ["Quest 1 Touch", "Quest Touch", "Rift S"], "Oculus Touch 2"),
            [BeatLeaderControllerId.CreateUnsafe(61).Value] =
                ("questPro", ["Quest Pro Touch", "Quest Pro"], "Touch Pro"),
            [BeatLeaderControllerId.CreateUnsafe(64).Value] = ("knuckles", ["Knuckles", "Index"], "Knuckles"),
            [BeatLeaderControllerId.CreateUnsafe(67).Value] = ("hands", ["Hands"], "Hands"),
            [BeatLeaderControllerId.CreateUnsafe(68).Value] = ("viveTracker3", ["Vive 3.0 Tracker"], "Vive Tracker 3"),
            [BeatLeaderControllerId.CreateUnsafe(69).Value] = ("pimax", ["Pimax"], "Pimax"),
            [BeatLeaderControllerId.CreateUnsafe(79).Value] =
                ("quest3", ["Quest 3 Touch", "Quest 3", "Quest 3S Touch"], "Quest 3 Touch"),
            [BeatLeaderControllerId.CreateUnsafe(256).Value] = ("quest2", ["Quest 2 Touch"], "Oculus Touch")
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, BeatLeaderControllerId> _scoreSaberAliases = _aliases
        .SelectMany(x => x.Value.ScoreSaberKeys
            .Select(key => new KeyValuePair<string, BeatLeaderControllerId>(key, x.Key)))
        .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public ValueTask<ControllerId?> ResolveControllerIdAsync(
        BeatLeaderControllerId controllerId,
        CancellationToken token)
        => cache.GetOrCreateAsync($"{BlCacheKeyPrefix}{controllerId}", (scopeFactory, logger, controllerId),
            async static (state, token) =>
            {
                await using var scope = state.scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                if (_aliases.ContainsKey(state.controllerId))
                    return await ResolveAliasAsync(state.controllerId, context, token);

                var controller = await context.Controllers
                    .FirstOrDefaultAsync(x => x.BeatLeaderControllerId == state.controllerId, token);

                if (controller is not null)
                    return controller.Id;

                var beatLeaderApi = scope.ServiceProvider.GetRequiredService<BeatLeaderApi>();
                if (!(await beatLeaderApi.GetControllerAsync(state.controllerId, token))
                    .TryGetValue(out var blController, out var error))
                {
                    state.logger.LogWarning(
                        "Failed to resolve controller ID {ControllerId} from BeatLeader API: {Error}",
                        state.controllerId, error);
                    return (ControllerId?)null;
                }

                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (blController is null)
                {
                    state.logger.LogWarning("Controller ID {ControllerId} was not found on BeatLeader.",
                        state.controllerId);
                    return null;
                }

                controller = await context.Controllers.AsTracking()
                    .FirstOrDefaultAsync(x => x.BeatLeaderKey == blController.Key, token);

                controller ??= await context.Controllers.AsTracking()
                    .FirstOrDefaultAsync(x => x.BeatLeaderKey == null && x.ScoreSaberKey == blController.Key, token);

                var name = blController.Name ?? $"{char.ToUpperInvariant(blController.Key[0])}{blController.Key[1..]}";
                if (controller is null)
                {
                    controller = new Controller { Name = name };
                    context.Controllers.Add(controller);
                }

                controller.BeatLeaderControllerId = state.controllerId;
                controller.BeatLeaderKey = blController.Key;
                controller.Name = name;

                return await SaveControllerAsync(
                    controller, context,
                    x => x.BeatLeaderControllerId == state.controllerId || x.BeatLeaderKey == blController.Key, token
                );
            }, cancellationToken: token);

    public ValueTask<ControllerId?> ResolveControllerIdAsync(string? controller, CancellationToken token)
        => controller switch
        {
            null => ValueTask.FromResult<ControllerId?>(null),
            var key when _scoreSaberAliases.TryGetValue(key, out var controllerId)
                => ResolveControllerIdAsync(controllerId, token),
            var key => cache.GetOrCreateAsync($"{SsCacheKeyPrefix}{key}", (key, scopeFactory),
                async static (state, token) =>
                {
                    await using var scope = state.scopeFactory.CreateAsyncScope();
                    var context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                    var dbController = await context.Controllers
                        .FirstOrDefaultAsync(x => x.ScoreSaberKey == state.key, token);

                    if (dbController is not null)
                        return (ControllerId?)dbController.Id;

                    dbController = await context.Controllers.AsTracking()
                        .FirstOrDefaultAsync(x => x.ScoreSaberKey == null && x.BeatLeaderKey == state.key, token);

                    if (dbController is null)
                    {
                        dbController = new Controller { Name = state.key };
                        context.Controllers.Add(dbController);
                    }

                    dbController.ScoreSaberKey = state.key;

                    return await SaveControllerAsync(dbController, context, x => x.ScoreSaberKey == state.key, token);
                }, cancellationToken: token)
        };

    private static async Task<ControllerId> ResolveAliasAsync(
        BeatLeaderControllerId controllerId, ServerDbContext context, CancellationToken token)
    {
        var alias = _aliases[controllerId];
        var controllers = await context.Controllers.AsTracking()
            .Where(x => x.BeatLeaderControllerId == controllerId
                        || EF.Functions.ILike(x.BeatLeaderKey!, alias.BeatLeaderKey)
                        || alias.ScoreSaberKeys.Any(key => EF.Functions.ILike(x.ScoreSaberKey!, key)))
            .ToListAsync(token);

        var controller = controllers.FirstOrDefault(x => x.BeatLeaderControllerId == controllerId)
                         ?? controllers.FirstOrDefault(x =>
                             x.BeatLeaderControllerId == null
                             && string.Equals(x.BeatLeaderKey, alias.BeatLeaderKey,
                                 StringComparison.OrdinalIgnoreCase))
                         ?? controllers.FirstOrDefault(x =>
                             x.BeatLeaderControllerId == null
                             && x.BeatLeaderKey == null);

        if (controller is null)
        {
            controller = new Controller { Name = alias.Name };
            context.Controllers.Add(controller);
        }

        controller.BeatLeaderControllerId = controllerId;
        controller.BeatLeaderKey ??= alias.BeatLeaderKey;
        controller.Name = alias.Name;

        // Leave existing duplicate rows and their provider keys for a separate merge.
        if (!controllers.Any(x =>
                x.Id != controller.Id
                && x.ScoreSaberKey is { } key
                && _scoreSaberAliases.TryGetValue(key, out var id)
                && id == controllerId))
            controller.ScoreSaberKey ??= alias.ScoreSaberKeys[0];

        return await SaveControllerAsync(controller, context, x => x.BeatLeaderControllerId == controllerId, token);
    }

    private static async Task<ControllerId> SaveControllerAsync(
        Controller controller, ServerDbContext context, Expression<Func<Controller, bool>> identity,
        CancellationToken token)
    {
        try
        {
            await context.SaveChangesAsync(token);
            return controller.Id;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
                  {
                      SqlState: PostgresErrorCodes.UniqueViolation
                  })
        {
            // Independent cache factories can discover the same provider key before either save completes.
            context.Entry(controller).State = EntityState.Detached;
            return await context.Controllers.Where(identity).Select(x => x.Id).SingleAsync(token);
        }
    }
}