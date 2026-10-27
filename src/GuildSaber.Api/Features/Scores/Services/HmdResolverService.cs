using System.Collections.Frozen;
using System.Linq.Expressions;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.ScoreSaber.Models;
using GuildSaber.Common.Services.ScoreSaber.Models.Responses;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Hardware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Npgsql;

namespace GuildSaber.Api.Features.Scores.Services;

public sealed class HmdResolverService(
    HybridCache cache,
    IServiceScopeFactory scopeFactory,
    ILogger<HmdResolverService> logger)
{
    private const string BlCacheKeyPrefix = "bl_hmd_";
    private const string SsCacheKeyPrefix = "ss_hmd_";

    private static readonly FrozenDictionary<BeatLeaderHmdId,
        (string BeatLeaderKey, string[] ScoreSaberKeys, string Name)> _aliases =
        new Dictionary<BeatLeaderHmdId, (string BeatLeaderKey, string[] ScoreSaberKeys, string Name)>
        {
            [BeatLeaderHmdId.CreateUnsafe(0).Value] = ("unknown", ["Unknown"], "Unknown"),
            [BeatLeaderHmdId.CreateUnsafe(1).Value] = ("rift", ["Rift CV1", "CV1"], "Oculus Rift"),
            [BeatLeaderHmdId.CreateUnsafe(2).Value] = ("vive", ["Vive"], "HTC Vive"),
            [BeatLeaderHmdId.CreateUnsafe(4).Value] = ("vivePro", ["Vive Pro"], "HTC Vive Pro"),
            [BeatLeaderHmdId.CreateUnsafe(16).Value] = ("riftS", ["Rift S"], "Oculus Rift S"),
            [BeatLeaderHmdId.CreateUnsafe(32).Value] = ("quest", ["Quest 1", "Quest"], "Meta Quest"),
            [BeatLeaderHmdId.CreateUnsafe(35).Value] = ("vivePro2", ["Vive Pro 2"], "HTC Vive Pro 2"),
            [BeatLeaderHmdId.CreateUnsafe(36).Value] = ("viveElite", ["Vive Elite"], "HTC Vive Elite"),
            [BeatLeaderHmdId.CreateUnsafe(41).Value] = ("hpReverb", ["HP Reverb", "HP Reverb G2"], "HP Reverb"),
            [BeatLeaderHmdId.CreateUnsafe(60).Value] = ("picoNeo4", ["PICO 4", "PICO 4S"], "Pico Neo 4"),
            [BeatLeaderHmdId.CreateUnsafe(61).Value] = ("questPro", ["Quest Pro"], "Meta Quest Pro"),
            [BeatLeaderHmdId.CreateUnsafe(62).Value] = ("pimaxCrystal", ["Pimax Crystal"], "Pimax Crystal"),
            [BeatLeaderHmdId.CreateUnsafe(64).Value] = ("index", ["Valve Index"], "Valve Index"),
            [BeatLeaderHmdId.CreateUnsafe(66).Value] = ("bigscreenbeyond", ["Bigscreen Beyond"], "Bigscreen Beyond"),
            [BeatLeaderHmdId.CreateUnsafe(70).Value] = ("psvr2", ["PSVR2"], "PlayStation VR2"),
            [BeatLeaderHmdId.CreateUnsafe(256).Value] =
                ("quest2", ["Quest 2", "Quest 2 (Virtual Desktop)"], "Meta Quest 2"),
            [BeatLeaderHmdId.CreateUnsafe(512).Value] = ("quest3", ["Quest 3"], "Meta Quest 3"),
            [BeatLeaderHmdId.CreateUnsafe(513).Value] = ("quest3s", ["Quest 3S"], "Meta Quest 3S")
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, BeatLeaderHmdId> _scoreSaberAliases = _aliases
        .SelectMany(x => x.Value.ScoreSaberKeys.Select(key => new KeyValuePair<string, BeatLeaderHmdId>(key, x.Key)))
        .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public ValueTask<HmdId?> ResolveHmdIdAsync(BeatLeaderHmdId hmdId, CancellationToken token)
        => cache.GetOrCreateAsync($"{BlCacheKeyPrefix}{hmdId}", (scopeFactory, logger, hmdId),
            async static (state, token) =>
            {
                await using var scope = state.scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                if (_aliases.ContainsKey(state.hmdId))
                    return await ResolveAliasAsync(state.hmdId, context, token);

                var hmd = await context.Hmds.FirstOrDefaultAsync(x => x.BeatLeaderHmdId == state.hmdId, token);
                if (hmd is not null) return hmd.Id;

                var beatLeaderApi = scope.ServiceProvider.GetRequiredService<BeatLeaderApi>();
                if (!(await beatLeaderApi.GetHmdAsync(state.hmdId, token)).TryGetValue(out var blHmd, out var error))
                {
                    state.logger.LogWarning("Failed to resolve HMD ID {HmdId} from BeatLeader API: {Error}",
                        state.hmdId, error);

                    /* Note: Yeah it would cache a bad response but realistically that's very unlikely to happen
                     * given we get the scores form bl already previously. */
                    return (HmdId?)null;
                }

                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (blHmd is null)
                {
                    state.logger.LogWarning("HMD ID {HmdId} was not found on BeatLeader.", state.hmdId);
                    return null;
                }

                hmd = await context.Hmds.AsTracking()
                    .FirstOrDefaultAsync(x => x.BeatLeaderKey == blHmd.Key, token);

                hmd ??= await context.Hmds.AsTracking()
                    .FirstOrDefaultAsync(x => x.BeatLeaderKey == null && x.ScoreSaberKey == blHmd.Key, token);

                var name = blHmd.Name ?? $"{char.ToUpperInvariant(blHmd.Key[0])}{blHmd.Key[1..]}";
                if (hmd is null)
                {
                    hmd = new Hmd { Name = name };
                    context.Hmds.Add(hmd);
                }

                hmd.BeatLeaderHmdId = state.hmdId;
                hmd.BeatLeaderKey = blHmd.Key;
                hmd.Name = name;

                return await SaveHmdAsync(
                    hmd, context,
                    x => x.BeatLeaderHmdId == state.hmdId || x.BeatLeaderKey == blHmd.Key, token
                );
            }, cancellationToken: token);

    public ValueTask<HmdId?> ResolveHmdIdAsync(Device? device, LegacyHMD? hmd, CancellationToken token)
        => (device, hmd) switch
        {
            ({ Hmd: not null }, _) => ResolveHmdIdAsync(device.Hmd, token),
            // All LegacyHmd translates 1:1 to BeatLeaderHmdId.
            (_, not null) => ResolveHmdIdAsync(BeatLeaderHmdId.CreateUnsafe((int)hmd.Value).Value, token),
            _ => ValueTask.FromResult<HmdId?>(null)
        };

    private ValueTask<HmdId?> ResolveHmdIdAsync(string? hmd, CancellationToken token) => hmd switch
    {
        null => ValueTask.FromResult<HmdId?>(null),
        var key when _scoreSaberAliases.TryGetValue(key, out var hmdId) => ResolveHmdIdAsync(hmdId, token),
        var key => cache.GetOrCreateAsync($"{SsCacheKeyPrefix}{key}", (key, scopeFactory),
            async static (state, token) =>
            {
                await using var scope = state.scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                var dbHmd = await context.Hmds.FirstOrDefaultAsync(x => x.ScoreSaberKey == state.key, token);
                if (dbHmd is not null) return (HmdId?)dbHmd.Id;

                dbHmd = await context.Hmds.AsTracking()
                    .FirstOrDefaultAsync(x => x.ScoreSaberKey == null && x.BeatLeaderKey == state.key, token);

                if (dbHmd is null)
                {
                    dbHmd = new Hmd { Name = state.key };
                    context.Hmds.Add(dbHmd);
                }

                dbHmd.ScoreSaberKey = state.key;
                return await SaveHmdAsync(dbHmd, context, x => x.ScoreSaberKey == state.key, token);
            }, cancellationToken: token)
    };

    private static async Task<HmdId> ResolveAliasAsync(
        BeatLeaderHmdId hmdId, ServerDbContext context, CancellationToken token)
    {
        var alias = _aliases[hmdId];
        var hmds = await context.Hmds.AsTracking()
            .Where(x => x.BeatLeaderHmdId == hmdId
                        || EF.Functions.ILike(x.BeatLeaderKey!, alias.BeatLeaderKey)
                        || alias.ScoreSaberKeys.Any(key => EF.Functions.ILike(x.ScoreSaberKey!, key)))
            .ToListAsync(token);

        var hmd = hmds.FirstOrDefault(x => x.BeatLeaderHmdId == hmdId)
                  ?? hmds.FirstOrDefault(x =>
                      x.BeatLeaderHmdId == null
                      && string.Equals(x.BeatLeaderKey, alias.BeatLeaderKey, StringComparison.OrdinalIgnoreCase))
                  ?? hmds.FirstOrDefault(x => x.BeatLeaderHmdId == null && x.BeatLeaderKey == null);

        if (hmd is null)
        {
            hmd = new Hmd { Name = alias.Name };
            context.Hmds.Add(hmd);
        }

        hmd.BeatLeaderHmdId = hmdId;
        hmd.BeatLeaderKey ??= alias.BeatLeaderKey;
        hmd.Name = alias.Name;

        // Leave existing duplicate rows and their provider keys for a separate merge.
        if (!hmds.Any(x => x.Id != hmd.Id
                           && x.ScoreSaberKey is { } key
                           && _scoreSaberAliases.TryGetValue(key, out var id) && id == hmdId))
            hmd.ScoreSaberKey ??= alias.ScoreSaberKeys[0];

        return await SaveHmdAsync(hmd, context, x => x.BeatLeaderHmdId == hmdId, token);
    }

    private static async Task<HmdId> SaveHmdAsync(
        Hmd hmd, ServerDbContext context, Expression<Func<Hmd, bool>> identity, CancellationToken token)
    {
        try
        {
            await context.SaveChangesAsync(token);
            return hmd.Id;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
                  {
                      SqlState: PostgresErrorCodes.UniqueViolation
                  })
        {
            // Independent cache factories can discover the same provider key before either save completes.
            context.Entry(hmd).State = EntityState.Detached;
            return await context.Hmds.Where(identity).Select(x => x.Id).SingleAsync(token);
        }
    }
}