using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Achievements.Http;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Achievements;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementRequests;

namespace GuildSaber.Api.Features.Guilds.Achievements.Services;

public sealed class AchievementService(
    ServerDbContext dbContext,
    IBackgroundTaskQueue taskQueue,
    IServiceScopeFactory scopeFactory)
{
    public async Task<Result<AchievementResponses.Achievement?>> CreateAsync(
        ContextId contextId, Definition request, CancellationToken token)
    {
        var guildId = await dbContext.Contexts.Where(x => x.Id == contextId)
            .Select(x => (GuildId?)x.GuildId).FirstOrDefaultAsync(token);

        if (guildId is null) return Success<AchievementResponses.Achievement?>(null);
        if ((await ValidateAsync(guildId.Value, request, token)).TryGetError(out var error))
            return Failure<AchievementResponses.Achievement?>(error);

        var info = new AchievementInfo(request.Name, System.Drawing.Color.FromArgb(request.Color));
        var bindings = new AchievementDiscordBindings(request.RoleId);
        Achievement achievement = request.Requirement switch
        {
            Requirement.RankedMapList rule => new RankedMapListAchievement(
                id: default, guildId: guildId.Value, contextId: contextId, categoryId: request.CategoryId,
                info: info, discordBindings: bindings, progressionOrder: request.ProgressionOrder,
                isLocking: request.IsLocking, unlockXp: request.UnlockXp, requiredPassCount: rule.RequiredPassCount),
            Requirement.DiffStar rule => new DiffStarAchievement(
                id: default, guildId: guildId.Value, contextId: contextId, categoryId: request.CategoryId,
                info: info, discordBindings: bindings, progressionOrder: request.ProgressionOrder,
                isLocking: request.IsLocking, unlockXp: request.UnlockXp,
                minStar: new RankedMapRating.DifficultyStar(rule.MinStar), requiredPassCount: rule.RequiredPassCount,
                maxStar: rule.MaxStar is { } max ? new RankedMapRating.DifficultyStar(max) : null),
            Requirement.AccStar rule => new AccStarAchievement(
                id: default, guildId: guildId.Value, contextId: contextId, categoryId: request.CategoryId,
                info: info, discordBindings: bindings, progressionOrder: request.ProgressionOrder,
                isLocking: request.IsLocking, unlockXp: request.UnlockXp,
                minStar: new RankedMapRating.AccuracyStar(rule.MinStar), requiredPassCount: rule.RequiredPassCount,
                maxStar: rule.MaxStar is { } max ? new RankedMapRating.AccuracyStar(max) : null)
        };

        dbContext.Achievements.Add(achievement);
        await dbContext.SaveChangesAsync(token);
        await QueueStatsRefreshAsync(contextId);

        return await dbContext.Achievements.Where(x => x.Id == achievement.Id)
            .Select(x => x.Map()).FirstOrDefaultAsync(token);
    }

    public async Task<Result<AchievementResponses.Achievement?>> UpdateAsync(
        ContextId contextId, int achievementId, Definition request, CancellationToken token)
    {
        var achievement = await dbContext.Achievements.AsTracking()
            .FirstOrDefaultAsync(x => x.Id == achievementId && x.ContextId == contextId, token);

        if (achievement is null) return Success<AchievementResponses.Achievement?>(null);

        if ((await ValidateAsync(achievement.GuildId, request, token)).TryGetError(out var error))
            return Failure<AchievementResponses.Achievement?>(error);

        var refreshStats = achievement.CategoryId != request.CategoryId
                           || achievement.ProgressionOrder != request.ProgressionOrder
                           || achievement.IsLocking != request.IsLocking;

        switch (achievement, request.Requirement)
        {
            case (RankedMapListAchievement mapList, Requirement.RankedMapList requirement):
                refreshStats |= mapList.RequiredPassCount != requirement.RequiredPassCount;
                mapList.RequiredPassCount = requirement.RequiredPassCount;
                break;
            case (DiffStarAchievement diffStar, Requirement.DiffStar requirement):
                refreshStats |= !diffStar.MinStar.Value.Equals(requirement.MinStar)
                                || !Nullable.Equals(diffStar.MaxStar?.Value, requirement.MaxStar)
                                || diffStar.RequiredPassCount != requirement.RequiredPassCount;
                diffStar.MinStar = new RankedMapRating.DifficultyStar(requirement.MinStar);
                diffStar.MaxStar = requirement.MaxStar is { } maxDifficulty
                    ? new RankedMapRating.DifficultyStar(maxDifficulty)
                    : null;
                diffStar.RequiredPassCount = requirement.RequiredPassCount;
                break;
            case (AccStarAchievement accStar, Requirement.AccStar requirement):
                refreshStats |= !accStar.MinStar.Value.Equals(requirement.MinStar)
                                || !Nullable.Equals(accStar.MaxStar?.Value, requirement.MaxStar)
                                || accStar.RequiredPassCount != requirement.RequiredPassCount;
                accStar.MinStar = new RankedMapRating.AccuracyStar(requirement.MinStar);
                accStar.MaxStar = requirement.MaxStar is { } maxAccuracy
                    ? new RankedMapRating.AccuracyStar(maxAccuracy)
                    : null;
                accStar.RequiredPassCount = requirement.RequiredPassCount;
                break;
            default:
                return Failure<AchievementResponses.Achievement?>(
                    "The completion rule must match the achievement type.");
        }

        achievement.Info = new AchievementInfo(request.Name, System.Drawing.Color.FromArgb(request.Color));
        achievement.DiscordBindings = new AchievementDiscordBindings(request.RoleId);
        achievement.CategoryId = request.CategoryId;
        achievement.ProgressionOrder = request.ProgressionOrder;
        achievement.IsLocking = request.IsLocking;
        achievement.UnlockXp = request.UnlockXp;

        await dbContext.SaveChangesAsync(token);

        if (refreshStats) await QueueStatsRefreshAsync(contextId);

        return await dbContext.Achievements.Where(x => x.Id == achievementId && x.ContextId == contextId)
            .Select(x => x.Map()).FirstOrDefaultAsync(token);
    }

    public async Task<bool> DeleteAsync(ContextId contextId, int achievementId, CancellationToken token)
    {
        if (await dbContext.Achievements.Where(x => x.Id == achievementId && x.ContextId == contextId)
                .ExecuteDeleteAsync(token) == 0)
            return false;

        await QueueStatsRefreshAsync(contextId);
        return true;
    }

    private async Task<UnitResult<string>> ValidateAsync(GuildId guildId, Definition request, CancellationToken token)
    {
        if (request.ProgressionOrder is null && request.IsLocking)
            return UnitResult.Failure("Only ordered achievements can lock progression.");

        if (request.Requirement is Requirement.DiffStar { MaxStar: { } maxDiff } diff && maxDiff <= diff.MinStar
            || request.Requirement is Requirement.AccStar { MaxStar: { } maxAcc } acc && maxAcc <= acc.MinStar)
            return UnitResult.Failure("The upper star bound must exceed the lower bound.");

        return request.CategoryId is { } categoryId && !await dbContext.Categories
            .AnyAsync(x => x.Id == categoryId && x.GuildId == guildId, token)
            ? UnitResult.Failure("The category must belong to this guild.")
            : UnitResult.Success<string>();
    }

    private ValueTask QueueStatsRefreshAsync(ContextId contextId)
        => taskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            var context = await database.Contexts.Include(x => x.Points)
                .FirstOrDefaultAsync(x => x.Id == contextId, cancellationToken);

            if (context is null) return;

            await scope.ServiceProvider.GetRequiredService<MemberStatsPipeline>().ExecuteAsync(context,
                database.ContextMembers.Where(x => x.ContextId == contextId).Select(x => x.PlayerId),
                refreshPoints: false, token: cancellationToken);
        });
}