using System.Globalization;
using CSharpFunctionalExtensions;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.DiscordBot.Core.Handlers;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementRequests;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_achievement_appearance:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task ShowRankingAchievementAppearanceAsync(ContextId contextId, string sessionId)
    {
        if (await GetRankingAchievementEditorAsync(contextId, sessionId) is not { } editor) return;

        await RespondWithModalAsync($"ranking_achievement_appearance_save:{contextId},{sessionId}",
            new RankingAchievementAppearanceModal
            {
                Name = editor.Definition.Name,
                Color = $"#{(uint)editor.Definition.Color:X8}"
            });
    }

    [ModalInteraction("ranking_achievement_appearance_save:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task SaveRankingAchievementAppearanceAsync(
        ContextId contextId, string sessionId, RankingAchievementAppearanceModal modal)
        => UpdateRankingAchievementEditorAsync(contextId, sessionId,
            request => ApplyAchievementAppearance(request, modal));

    [ComponentInteraction("ranking_achievement_rule:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task ShowRankingAchievementRuleAsync(ContextId contextId, string sessionId)
    {
        if (await GetRankingAchievementEditorAsync(contextId, sessionId) is not { } editor) return;

        var args = $"{contextId},{sessionId}";
        await (editor.Definition.Requirement switch
        {
            Requirement.RankedMapList list => RespondWithModalAsync(
                $"ranking_achievement_maps_save:{args}", new RankingAchievementMapListModal
                {
                    PassCount = list.RequiredPassCount.ToString(CultureInfo.InvariantCulture)
                }),
            Requirement.DiffStar diff => RespondWithModalAsync(
                $"ranking_achievement_stars_save:{args}", new RankingAchievementStarModal
                {
                    Title = "Difficulty-star rule",
                    Minimum = diff.MinStar.ToString("R", CultureInfo.InvariantCulture),
                    Maximum = diff.MaxStar?.ToString("R", CultureInfo.InvariantCulture),
                    PassCount = diff.RequiredPassCount.ToString(CultureInfo.InvariantCulture)
                }),
            Requirement.AccStar acc => RespondWithModalAsync(
                $"ranking_achievement_stars_save:{args}", new RankingAchievementStarModal
                {
                    Title = "Accuracy-star rule",
                    Minimum = acc.MinStar.ToString("R", CultureInfo.InvariantCulture),
                    Maximum = acc.MaxStar?.ToString("R", CultureInfo.InvariantCulture),
                    PassCount = acc.RequiredPassCount.ToString(CultureInfo.InvariantCulture)
                })
        });
    }

    [ModalInteraction("ranking_achievement_stars_save:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task SaveRankingAchievementStarsAsync(
        ContextId contextId, string sessionId, RankingAchievementStarModal modal)
        => UpdateRankingAchievementEditorAsync(contextId, sessionId,
            request => ApplyAchievementStars(request, modal));

    [ModalInteraction("ranking_achievement_maps_save:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task SaveRankingAchievementMapListAsync(
        ContextId contextId, string sessionId, RankingAchievementMapListModal modal)
        => UpdateRankingAchievementEditorAsync(contextId, sessionId, request =>
            request.Requirement is Requirement.RankedMapList && uint.TryParse(modal.PassCount, out var count)
                ? request with { Requirement = new Requirement.RankedMapList(count) }
                : Failure<Definition>("The required pass count must be a whole number from 0 to 4294967295."));

    [ComponentInteraction("ranking_achievement_standalone:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task MakeRankingAchievementStandaloneAsync(ContextId contextId, string sessionId)
        => UpdateRankingAchievementEditorAsync(contextId, sessionId,
            request => request with { ProgressionOrder = null, IsLocking = false });

    [ComponentInteraction("ranking_achievement_as_level:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task ChooseRankingAchievementLevelAsync(ContextId contextId, string sessionId)
        => ShowRankingAchievementLevelAsync(contextId, sessionId);

    [ComponentInteraction("ranking_achievement_level:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task ShowRankingAchievementLevelAsync(ContextId contextId, string sessionId)
    {
        if (await GetRankingAchievementEditorAsync(contextId, sessionId) is not { } editor) return;

        await RespondWithModalAsync($"ranking_achievement_level_save:{contextId},{sessionId}",
            new RankingAchievementLevelModal
            {
                Order = (editor.Definition.ProgressionOrder
                         ?? (editor.Original?.Progression as AchievementProgression.Ordered)?.Order)
                    ?.ToString(CultureInfo.InvariantCulture) ?? ""
            });
    }

    [ModalInteraction("ranking_achievement_level_save:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task SaveRankingAchievementLevelAsync(
        ContextId contextId, string sessionId, RankingAchievementLevelModal modal)
        => UpdateRankingAchievementEditorAsync(contextId, sessionId,
            request => uint.TryParse(modal.Order, out var order)
                ? request with { ProgressionOrder = order }
                : Failure<Definition>("The level order must be a whole number from 0 to 4294967295."));

    [ComponentInteraction("ranking_achievement_xp:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task ShowRankingAchievementXpAsync(ContextId contextId, string sessionId)
    {
        if (await GetRankingAchievementEditorAsync(contextId, sessionId) is not { } editor) return;

        await RespondWithModalAsync($"ranking_achievement_xp_save:{contextId},{sessionId}",
            new RankingAchievementXpModal { UnlockXp = editor.Definition.UnlockXp.ToString() });
    }

    [ModalInteraction("ranking_achievement_xp_save:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task SaveRankingAchievementXpAsync(
        ContextId contextId, string sessionId, RankingAchievementXpModal modal)
        => UpdateRankingAchievementEditorAsync(contextId, sessionId,
            request => Xp.TryParse(modal.UnlockXp)
                .Map(static (xp, definition) => definition with { UnlockXp = xp }, request));

    [ComponentInteraction("ranking_achievement_locking:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task ToggleRankingAchievementLockingAsync(ContextId contextId, string sessionId)
        => UpdateRankingAchievementEditorAsync(contextId, sessionId,
            request => request.ProgressionOrder is null
                ? Failure<Definition>("Choose Progression level before enabling blocking.")
                : Success(request with { IsLocking = !request.IsLocking }));

    private static Result<Definition> ApplyAchievementAppearance(
        Definition request, RankingAchievementAppearanceModal modal)
    {
        var colorText = modal.Color.Trim().TrimStart('#');
        if (string.IsNullOrWhiteSpace(modal.Name) || colorText.Length is not (6 or 8)
            || !uint.TryParse(colorText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var color))
            return Failure<Definition>("Enter a name and a hexadecimal color such as #52FF17 or #FF52FF17.");

        return request with
        {
            Name = modal.Name.Trim(),
            Color = unchecked((int)(colorText.Length == 6 ? color | 0xFF000000 : color))
        };
    }

    private static Result<Definition> ApplyAchievementStars(Definition request, RankingAchievementStarModal modal)
    {
        if (!TryDraftNumber(modal.Minimum, out var min) || min is not { } minimum
            || !TryDraftNumber(modal.Maximum, out var maximum) || maximum <= minimum
            || !uint.TryParse(modal.PassCount, out var count))
            return Failure<Definition>(
                "Star bounds must be finite, non-negative numbers. The upper bound must exceed the lower bound, " +
                "or be blank for no upper limit. The required pass count must be a whole number from 0 to 4294967295.");

        return request.Requirement switch
        {
            Requirement.DiffStar => request with { Requirement = new Requirement.DiffStar(minimum, count, maximum) },
            Requirement.AccStar => request with { Requirement = new Requirement.AccStar(minimum, count, maximum) },
            Requirement.RankedMapList => Failure<Definition>("Open the map-list rule to edit this achievement.")
        };
    }
}

public class RankingAchievementAppearanceModal : IModal
{
    public string Title => "Name and color";

    [InputLabel("Name")]
    [ModalTextInput("name", maxLength: 32)]
    public string Name { get; set; } = "";

    [InputLabel("Color", "Hexadecimal RGB or ARGB, such as #52FF17 or #FF52FF17.")]
    [ModalTextInput("color", maxLength: 9)]
    public string Color { get; set; } = "";
}

public class RankingAchievementStarModal : IModal
{
    public string Title { get; set; } = "Star rule";

    [InputLabel("Minimum stars", "Inclusive lower bound.")]
    [ModalTextInput("minimum", maxLength: 48)]
    public string Minimum { get; set; } = "";

    [InputLabel("Maximum stars", "Exclusive upper bound. Leave blank for no upper limit.")]
    [ModalTextInput("maximum", maxLength: 48)]
    [RequiredInput(false)]
    public string? Maximum { get; set; }

    [InputLabel("Required passes")]
    [ModalTextInput("passes", maxLength: 10)]
    public string PassCount { get; set; } = "";
}

public class RankingAchievementMapListModal : IModal
{
    public string Title => "Map-list rule";

    [InputLabel("Required passes", "Number of associated ranked maps needed for completion.")]
    [ModalTextInput("passes", maxLength: 10)]
    public string PassCount { get; set; } = "";
}

public class RankingAchievementLevelModal : IModal
{
    public string Title => "Level progression";

    [InputLabel("Level order", "Position in the level progression shown on player cards and /progress.")]
    [ModalTextInput("order", maxLength: 10)]
    public string Order { get; set; } = "";
}

public class RankingAchievementXpModal : IModal
{
    public string Title => "Unlock XP";

    [InputLabel("Unlock XP (reserved)", "Stored setting. Current completion rules use pass counts and progression.")]
    [ModalTextInput("xp", maxLength: 48)]
    public string UnlockXp { get; set; } = "0";
}