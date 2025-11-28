using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using GuildSaber.Api.Features.RankedScores;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedScores;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Guilds.Members.Pipelines;

public sealed class MemberPointStatsPipeline(ServerDbContext dbContext)
{
    private static readonly string _calculatePointsFormattableString =
        $$"""
          SELECT COALESCE(SUM("{{nameof(RankedScore.RawPoints)}}" * POWER({0}, position - 1)), 0)::float AS "Value"
          FROM (
              SELECT
                  "{{nameof(RankedScore.RawPoints)}}",
                  ROW_NUMBER() OVER (ORDER BY "{{nameof(RankedScore.RawPoints)}}" DESC) as position
              FROM "{{nameof(ServerDbContext.RankedScores)}}"
              WHERE "{{nameof(RankedScore.GuildId)}}" = {1}
                  AND "{{nameof(RankedScore.ContextId)}}" = {2}
                  AND "{{nameof(RankedScore.PlayerId)}}" = {3}
                  AND "{{nameof(RankedScore.PointId)}}" = {4}
                  AND ("{{nameof(RankedScore.State)}}" & {{(int)RankedScore.EState.Selected}}) = {{(int)RankedScore.EState.Selected}}
                  AND ("{{nameof(RankedScore.State)}}" & {{(int)RankedScore.EState.NonPointGiving}}) = 0
          ) ranked_scores
          """;

    private static readonly string _calculatePointsWithCategoryFormattableString =
        $$"""
          SELECT COALESCE(SUM("{{nameof(RankedScore.RawPoints)}}" * POWER({0}, position - 1)), 0)::float AS "Value"
          FROM (
              SELECT
                  rs."{{nameof(RankedScore.RawPoints)}}",
                  ROW_NUMBER() OVER (ORDER BY rs."{{nameof(RankedScore.RawPoints)}}" DESC) as position
              FROM "{{nameof(ServerDbContext.RankedScores)}}" rs
              JOIN "{{nameof(ServerDbContext.RankedMaps)}}" rm
                  ON rs."{{nameof(RankedScore.RankedMapId)}}" = rm."{{nameof(RankedMap.Id)}}"
              JOIN "{{nameof(Category) + nameof(RankedMap)}}" rmc
                  ON rm."{{nameof(RankedMap.Id)}}" = rmc."{{nameof(RankedMap) + nameof(RankedMap.Id)}}"
              WHERE rs."{{nameof(RankedScore.GuildId)}}" = {1}
                  AND rs."{{nameof(RankedScore.ContextId)}}" = {2}
                  AND rs."{{nameof(RankedScore.PlayerId)}}" = {3}
                  AND rs."{{nameof(RankedScore.PointId)}}" = {4}
                  AND rmc."{{nameof(Category)[..^1] + "ies" + nameof(Category.Id)}}" = {5}
                  AND (rs."{{nameof(RankedScore.State)}}" & {{(int)RankedScore.EState.Selected}}) = {{(int)RankedScore.EState.Selected}}
                  AND (rs."{{nameof(RankedScore.State)}}" & {{(int)RankedScore.EState.NonPointGiving}}) = 0
          ) ranked_scores
          """;

    public async Task ExecuteAsync(PlayerId playerId, Context context)
    {
        Guard.IsNotNull(context.Points);

        var categories = await dbContext.Categories
            .Where(c => c.GuildId == context.GuildId)
            .ToListAsync();

        foreach (var point in context.Points)
        {
            await GetOrCreateMemberStatAsync(
                context.GuildId, context.Id, playerId, null, point);

            foreach (var category in categories)
                await GetOrCreateMemberStatAsync(
                    context.GuildId, context.Id, playerId, category.Id, point);
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task GetOrCreateMemberStatAsync(
        GuildId guildId,
        ContextId contextId,
        PlayerId playerId,
        Category.CategoryId? categoryId,
        Point point)
    {
        var memberStat = await dbContext.MemberPointStats
            .AsTracking()
            .Where(x =>
                x.GuildId == guildId &&
                x.ContextId == contextId &&
                x.PlayerId == playerId &&
                x.PointId == point.Id &&
                x.CategoryId == categoryId)
            .FirstOrDefaultAsync();

        if (memberStat is null)
        {
            memberStat = new MemberPointStat
            {
                GuildId = guildId,
                ContextId = contextId,
                PlayerId = playerId,
                PointId = point.Id,
                CategoryId = categoryId
            };

            dbContext.MemberPointStats.Add(memberStat);
        }

        var validPassesQuery = dbContext.RankedScores
            .Where(x =>
                x.GuildId == guildId &&
                x.ContextId == contextId &&
                x.PlayerId == playerId &&
                x.PointId == point.Id)
            .Where(RankedScore.IsValidPassesExpression);

        if (categoryId is not null)
            validPassesQuery = validPassesQuery.Where(x => x.RankedMap.Categories.Any(c => c.Id == categoryId));

        memberStat.Points = point.WeightingSettings.IsEnabled
            ? await CalculateMemberPointsWithWeightQuery(dbContext, guildId, contextId, playerId, point, categoryId)
            : await validPassesQuery.SumAsync(x => x.RawPoints);

        memberStat.PassCount = validPassesQuery.Count();
    }

    public static Task<float> CalculateMemberPointsWithWeightQuery(
        ServerDbContext dbContext,
        GuildId guildId,
        ContextId contextId,
        PlayerId playerId,
        Point point,
        Category.CategoryId? categoryId)
        => dbContext.Database.SqlQuery<float>(categoryId switch
        {
            null => FormattableStringFactory.Create(
                _calculatePointsFormattableString,
                point.WeightingSettings.Multiplier,
                (int)guildId,
                (int)contextId,
                (long)playerId,
                (int)point.Id
            ),
            _ => FormattableStringFactory.Create(
                _calculatePointsWithCategoryFormattableString,
                point.WeightingSettings.Multiplier,
                (int)guildId,
                (int)contextId,
                (long)playerId,
                (int)point.Id,
                (int)categoryId.Value
            )
        }).FirstOrDefaultAsync();
}