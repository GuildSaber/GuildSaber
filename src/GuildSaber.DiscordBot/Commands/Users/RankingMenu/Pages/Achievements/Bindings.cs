using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Categories.Http;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementRequests;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_achievement_bindings:*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task ShowRankingAchievementBindingsAsync(ContextId contextId, string sessionId, int bindingPage)
    {
        var (editor, categories) = await (
            GetRankingAchievementEditorAsync(contextId, sessionId, defer: true).AsTask(),
            Cache.GetGuildCategoriesAsync(await GetGuildIdAsync(), Client.Value).AsTask()
        ).WhenAll();
        if (editor is null) return;

        var component = RankingAchievementBindingsView.BuildComponent(sessionId, editor, categories,
            editor.Definition.RoleId is { } roleId ? Context.Guild.GetRole(roleId) : null,
            Math.Clamp(bindingPage, 1, Math.Max(1, (categories.Length + 23) / 24))).Build();

        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component;
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_achievement_category:*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task SetRankingAchievementCategoryAsync(
        ContextId contextId, string sessionId, int bindingPage, string[] values)
        => UpdateRankingAchievementEditorAsync(contextId, sessionId, request => request with
        {
            CategoryId = int.Parse(values[0]) switch { 0 => null, var id => new CategoryId(id) }
        }, bindingPage);

    [ComponentInteraction("ranking_achievement_role:*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task SetRankingAchievementRoleAsync(
        ContextId contextId, string sessionId, int bindingPage, string[] values)
        => UpdateRankingAchievementEditorAsync(contextId, sessionId, request => values switch
        {
            [] => request with { RoleId = null },
            [var value, ..] when DiscordRoleId.TryParse(value).TryGetValue(out var roleId)
                                 && Context.Guild.GetRole(roleId) is not null
                => request with { RoleId = roleId },
            _ => Failure<Definition>("This role is no longer available in this Discord server.")
        }, bindingPage);

    [ComponentInteraction("ranking_achievement_role_clear:*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task ClearRankingAchievementRoleAsync(ContextId contextId, string sessionId, int bindingPage)
        => UpdateRankingAchievementEditorAsync(contextId, sessionId,
            request => request with { RoleId = null }, bindingPage);

    [ComponentInteraction("ranking_achievement_role_color:*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public Task UseRankingAchievementRoleColorAsync(ContextId contextId, string sessionId, int bindingPage)
        => UpdateRankingAchievementEditorAsync(contextId, sessionId,
            request => request.RoleId is { } roleId && Context.Guild.GetRole(roleId) is { } role
                ? Success(request with { Color = unchecked((int)(0xFF000000 | role.Colors.PrimaryColor.RawValue)) })
                : Failure<Definition>("Choose an existing Discord role first."), bindingPage);
}

file static class RankingAchievementBindingsView
{
    public static ComponentBuilderV2 BuildComponent(
        string sessionId, RankingAchievementEditor editor, CategoryResponses.Category[] categories,
        IRole? role, int page)
    {
        var definition = editor.Definition;
        var args = $"{editor.ContextId},{sessionId}";
        var categoryName = definition.GetCategoryName(categories);
        var name = editor.Original?.Info.Name ?? definition.Name;
        var builder = new ComponentBuilderV2().WithContainer(container => container
            .WithAccentColor(new Color((uint)definition.Color & 0xFFFFFF))
            .WithTextDisplay($"### {Format.Sanitize(name.Length == 0 ? "New achievement" : name)}\n" +
                             "Category and Discord role" +
                             (definition.CategoryId is null ? "\nGlobal covers every category." : ""))
            .WithActionRow(row => row.WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId($"ranking_achievement_category:{args},{page}")
                .WithPlaceholder($"Category: {categoryName}".Truncate(150))
                .WithOptions([
                    new SelectMenuOptionBuilder("Global", "0", isDefault: definition.CategoryId is null),
                    .. categories.OrderBy(x => x.Info.Name, StringComparer.OrdinalIgnoreCase)
                        .Skip((page - 1) * 24).Take(24)
                        .Select(x => new SelectMenuOptionBuilder(x.Info.Name.Truncate(100), x.Id.ToString(),
                            isDefault: definition.CategoryId == x.Id))
                ])))
            .WithTextDisplay("Discord role " + (definition.RoleId is { } roleId
                ? role is null ? $"Deleted role #{roleId}" : $"<@&{roleId}>"
                : "None"))
            .WithActionRow(row => row.WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId($"ranking_achievement_role:{args},{page}")
                .WithType(ComponentType.RoleSelect).WithMinValues(0).WithMaxValues(1)
                .WithPlaceholder("Choose a Discord role")
                .WithDefaultValues(role is null ? [] : [SelectMenuDefaultValue.FromRole(role)]))));

        if (editor is { Original: { } saved, HasChanges: true })
            builder.WithTextDisplay("**Unsaved changes** · Saved → Proposed\n" +
                                    definition.DescribeValues(saved.ToDefinition(), categories, onlyChanges: true));

        if (categories.Length > 24)
            builder.WithActionRow(row => row
                .WithButton("Prev categories", $"ranking_achievement_bindings:{args},{page - 1}",
                    ButtonStyle.Secondary, disabled: page <= 1)
                .WithButton("Next categories", $"ranking_achievement_bindings:{args},{page + 1}",
                    ButtonStyle.Secondary, disabled: page * 24 >= categories.Length));

        return builder.WithActionRow(row => row
            .WithButton("Back", $"ranking_achievement_editor:{args}", ButtonStyle.Secondary)
            .WithButton("Clear role", $"ranking_achievement_role_clear:{args},{page}", ButtonStyle.Secondary,
                disabled: definition.RoleId is null)
            .WithButton("Use role color", $"ranking_achievement_role_color:{args},{page}", ButtonStyle.Secondary,
                disabled: role is null));
    }
}