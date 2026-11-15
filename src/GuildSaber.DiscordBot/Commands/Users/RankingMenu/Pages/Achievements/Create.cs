using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementRequests;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_achievement_new:*,*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task NewRankingAchievementAsync(ContextId contextId, int categoryFilter, int page, int categoryPage)
    {
        await DeferRankingInteractionAsync();
        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = RankingAchievementCreationView.BuildComponent(contextId,
                new RankingAchievementNavigation(categoryFilter, page, categoryPage)).Build();
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_achievement_create:*,*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task StartRankingAchievementAsync(
        ContextId contextId, int categoryFilter, int page, int categoryPage, string[] values)
    {
        if (ChangeRankingAchievementType(values[0], new Requirement.DiffStar(0, 1)) is not { } rule)
        {
            await RespondAsync("Choose an available achievement type.", ephemeral: true);
            return;
        }

        var sessionId = Guid.NewGuid().ToString("N");
        await StoreRankingAchievementEditorAsync(sessionId, new RankingAchievementEditor(
            Context.User.DiscordId, contextId, null,
            new Definition(Name: "", Color: unchecked((int)0xFFFFFFFF),
                CategoryId: categoryFilter > 0 ? new CategoryId(categoryFilter) : null, RoleId: null,
                ProgressionOrder: null, IsLocking: false, UnlockXp: default, Requirement: rule),
            new RankingAchievementNavigation(categoryFilter, page, categoryPage)));
        await ShowRankingAchievementAppearanceAsync(contextId, sessionId);
    }

    [ComponentInteraction("ranking_achievement_type:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task ChangeRankingAchievementTypeAsync(ContextId contextId, string sessionId, string[] values)
    {
        if (await GetRankingAchievementEditorAsync(contextId, sessionId) is not { } editor) return;

        if (editor.Original is not null)
        {
            await RespondAsync("An existing achievement must retain its rule type.", ephemeral: true);
            return;
        }

        await UpdateRankingAchievementEditorAsync(contextId, sessionId, request =>
            ChangeRankingAchievementType(values[0], request.Requirement) is { } rule
                ? request with { Requirement = rule }
                : Failure<Definition>("Choose an available achievement type."));
    }

    private static Requirement? ChangeRankingAchievementType(string kind, Requirement current) => kind switch
    {
        "RankedMapList" => new Requirement.RankedMapList(current.RequiredPassCount),
        "DiffStar" => current switch
        {
            Requirement.DiffStar => current,
            Requirement.AccStar acc => new Requirement.DiffStar(acc.MinStar, acc.RequiredPassCount, acc.MaxStar),
            Requirement.RankedMapList list => new Requirement.DiffStar(0, list.RequiredPassCount)
        },
        "AccStar" => current switch
        {
            Requirement.AccStar => current,
            Requirement.DiffStar diff => new Requirement.AccStar(diff.MinStar, diff.RequiredPassCount, diff.MaxStar),
            Requirement.RankedMapList list => new Requirement.AccStar(0, list.RequiredPassCount)
        },
        _ => null
    };
}

internal static class RankingAchievementCreationView
{
    public static ComponentBuilderV2 BuildComponent(ContextId contextId, RankingAchievementNavigation navigation)
        => new ComponentBuilderV2().WithContainer(container => container.WithAccentColor(Color.Orange)
                .WithTextDisplay("### Create an achievement\nChoose how players complete it. " +
                                 "Then choose its category, Discord role and whether it appears " +
                                 "in the level progression.")
                .WithActionRow(row => row.WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId("ranking_achievement_create:" +
                                  $"{contextId},{navigation.CategoryFilter},{navigation.Page}," +
                                  $"{navigation.CategoryPage}")
                    .WithPlaceholder("Choose a completion rule")
                    .WithOptions(TypeOptions(null)))))
            .WithActionRow(row => row.WithButton("Back", navigation.ListId(contextId), ButtonStyle.Secondary));

    public static List<SelectMenuOptionBuilder> TypeOptions(Requirement? selected) =>
    [
        new("Difficulty stars", "DiffStar", "Pass ranked maps within a difficulty-star range.",
            isDefault: selected is Requirement.DiffStar),
        new("Accuracy stars", "AccStar", "Pass ranked maps within an accuracy-star range.",
            isDefault: selected is Requirement.AccStar),
        new("Map list", "RankedMapList", "Pass maps associated through ranked-map proposals.",
            isDefault: selected is Requirement.RankedMapList)
    ];
}