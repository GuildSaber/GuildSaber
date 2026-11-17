using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Categories.Http;
using GuildSaber.Api.Features.Guilds.Members.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.DiscordBot.Core.Extensions;
using GuildSaber.DiscordBot.Core.Handlers;
using Microsoft.Extensions.Caching.Hybrid;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementRequests;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementResponses;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_achievement_edit:*,*,*,*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task EditRankingAchievementAsync(
        ContextId contextId, int achievementId, int categoryFilter, int page, int categoryPage)
    {
        if (!(await (
                DeferRankingInteractionAsync(),
                Client.Value.Achievements.GetByIdAsync(achievementId)
            ).WhenAll()).TryGetValue(out var achievement, out var error))
        {
            await FollowupAsync(error, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        if (achievement is not { } existing || existing.ContextId != contextId)
        {
            await FollowupAsync("This achievement no longer exists in this context.", ephemeral: true);
            return;
        }

        var sessionId = Guid.NewGuid().ToString("N");
        await StoreRankingAchievementEditorAsync(sessionId, new RankingAchievementEditor(
            Context.User.DiscordId, contextId, existing, existing.ToDefinition(),
            new RankingAchievementNavigation(categoryFilter, page, categoryPage)));
        await ShowRankingAchievementEditorAsync(contextId, sessionId);
    }

    [ComponentInteraction("ranking_achievement_editor:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task ShowRankingAchievementEditorAsync(ContextId contextId, string sessionId)
    {
        var (editor, categories) = await (
            GetRankingAchievementEditorAsync(contextId, sessionId, defer: true).AsTask(),
            Cache.GetGuildCategoriesAsync(await GetGuildIdAsync(), Client.Value).AsTask()
        ).WhenAll();
        if (editor is null) return;

        var coverUrl = editor.Original is { } original ? Client.Value.Achievements.GetCoverUrl(original.Id) : null;
        var component = RankingAchievementEditorView.BuildComponent(sessionId, editor, categories, coverUrl).Build();
        await ModifyOriginalResponseAsync(x =>
        {
            x.Flags = MessageFlags.ComponentsV2;
            x.Components = component;
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_achievement_save:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task SaveRankingAchievementEditorAsync(ContextId contextId, string sessionId)
    {
        if (await GetRankingAchievementEditorAsync(contextId, sessionId) is not { } editor) return;

        if (!editor.HasChanges)
        {
            await RespondAsync("There are no unsaved changes.", ephemeral: true);
            return;
        }

        if (!(await (
                DeferRankingInteractionAsync(),
                editor.Original is { } original
                    ? Client.Value.Achievements.UpdateAsync(contextId, original.Id, editor.Definition)
                    : Client.Value.Achievements.CreateAsync(contextId, editor.Definition)
            ).WhenAll()).TryGetValue(out var achievement, out var error))
        {
            await FollowupAsync(error, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        if (achievement is not { } saved)
        {
            await FollowupAsync("The achievement or its context no longer exists. Your unsaved setup is kept here.",
                ephemeral: true);
            return;
        }

        await Cache.RemoveAsync($"RankingAchievementEditor_{sessionId}");
        await ShowRankingAchievementAsync(contextId, saved.Id, editor.Navigation.CategoryFilter,
            editor.Navigation.Page, editor.Navigation.CategoryPage);
        await FollowupAsync(editor.Original is null
                ? "Achievement created. Member stats refresh in the background."
                : "Achievement saved. Completion and progression changes refresh member stats in the background.",
            ephemeral: true);
    }

    [ComponentInteraction("ranking_achievement_cancel:*,*")]
    [PermissionHandler.RequirePermissionAttributeSlash(
        MemberResponses.EPermission.RankingTeam | MemberResponses.EPermission.GuildLeader, matchAny: true)]
    public async Task CancelRankingAchievementEditorAsync(ContextId contextId, string sessionId)
    {
        if (await GetRankingAchievementEditorAsync(contextId, sessionId, defer: true) is not { } editor) return;

        await Cache.RemoveAsync($"RankingAchievementEditor_{sessionId}");
        await (editor.Original is { } original
            ? ShowRankingAchievementAsync(contextId, original.Id, editor.Navigation.CategoryFilter,
                editor.Navigation.Page, editor.Navigation.CategoryPage)
            : ShowRankingAchievementsAsync(contextId, editor.Navigation.CategoryFilter,
                editor.Navigation.Page, editor.Navigation.CategoryPage));
    }

    private async Task UpdateRankingAchievementEditorAsync(
        ContextId contextId, string sessionId, Func<Definition, Result<Definition>> update, int? bindingPage = null)
    {
        if (await GetRankingAchievementEditorAsync(contextId, sessionId, defer: true) is not { } editor) return;

        if (!update(editor.Definition).TryGetValue(out var definition, out var error))
        {
            await (bindingPage is { } bindings
                ? ShowRankingAchievementBindingsAsync(contextId, sessionId, bindings)
                : ShowRankingAchievementEditorAsync(contextId, sessionId));
            await FollowupAsync(error, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await StoreRankingAchievementEditorAsync(sessionId, editor with { Definition = definition });
        await (bindingPage is { } page
            ? ShowRankingAchievementBindingsAsync(contextId, sessionId, page)
            : ShowRankingAchievementEditorAsync(contextId, sessionId));
    }

    private async ValueTask<RankingAchievementEditor?> GetRankingAchievementEditorAsync(
        ContextId contextId, string sessionId, bool defer = false)
    {
        var editor = await (
            defer ? DeferRankingInteractionAsync() : Task.CompletedTask,
            Cache.GetOrCreateAsync<RankingAchievementEditor?>($"RankingAchievementEditor_{sessionId}",
                static _ => ValueTask.FromResult<RankingAchievementEditor?>(null)).AsTask()
        ).WhenAll();
        if (editor is not null && editor.OwnerId == Context.User.DiscordId && editor.ContextId == contextId)
            return editor;

        const string message = "This achievement editor has expired or belongs to another player. " +
                               "Open Manage achievements from /ranking to start again.";
        await (Context.Interaction.HasResponded
            ? FollowupAsync(message, ephemeral: true)
            : RespondAsync(message, ephemeral: true));
        return null;
    }

    private ValueTask StoreRankingAchievementEditorAsync(string sessionId, RankingAchievementEditor editor)
        => Cache.SetAsync($"RankingAchievementEditor_{sessionId}", editor,
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(20), LocalCacheExpiration = TimeSpan.FromMinutes(20)
            });
}

public sealed record RankingAchievementEditor(
    DiscordId OwnerId,
    ContextId ContextId,
    Achievement? Original,
    Definition Definition,
    RankingAchievementNavigation Navigation)
{
    [JsonIgnore] public bool HasChanges => Definition != Original?.ToDefinition();
}

file static class RankingAchievementEditorView
{
    public static ComponentBuilderV2 BuildComponent(
        string sessionId, RankingAchievementEditor editor, CategoryResponses.Category[] categories, Uri? coverUrl)
    {
        var definition = editor.Definition;
        var args = $"{editor.ContextId},{sessionId}";
        var header = editor.Original is { } original
            ? $"### Edit {Format.Sanitize(original.Info.Name)}\nAchievement **#{original.Id}** · " +
              (editor.HasChanges ? "Unsaved changes · Saved → Proposed" : "No changes yet")
            : "### Create an achievement";
        var container = new ContainerBuilder().WithAccentColor(new Color((uint)definition.Color & 0xFFFFFF));

        if (coverUrl is null)
            container.WithTextDisplay(header);
        else
            container.WithSection(section => section.WithTextDisplay(header)
                .WithAccessory(new ThumbnailBuilder().WithMedia(coverUrl.ToString())));

        var builder = new ComponentBuilderV2().WithContainer(container
            .WithTextDisplay(definition.DescribeValues(editor.Original?.ToDefinition() ?? definition, categories))
            .WithTextDisplay(definition.ProgressionDescription +
                             (definition.Requirement is Requirement.RankedMapList
                                 ? "\nAssign maps through ranked-map proposals."
                                 : ""))
            .WithActionRow(row => row
                .WithButton("Standalone achievement", $"ranking_achievement_standalone:{args}",
                    definition.ProgressionOrder is null ? ButtonStyle.Primary : ButtonStyle.Secondary,
                    disabled: definition.ProgressionOrder is null)
                .WithButton("Progression level", $"ranking_achievement_as_level:{args}",
                    definition.ProgressionOrder is not null ? ButtonStyle.Primary : ButtonStyle.Secondary,
                    disabled: definition.ProgressionOrder is not null))
            .WithSeparator()
            .WithActionRow(row => row
                .WithButton("Name and color", $"ranking_achievement_appearance:{args}", ButtonStyle.Secondary)
                .WithButton(definition.Requirement is Requirement.RankedMapList ? "Map-list rule" : "Star rule",
                    $"ranking_achievement_rule:{args}", ButtonStyle.Secondary)
                .WithButton("Category and role", $"ranking_achievement_bindings:{args},1", ButtonStyle.Secondary))
            .WithActionRow(row => row
                .WithButton("Level order", $"ranking_achievement_level:{args}", ButtonStyle.Secondary,
                    disabled: definition.ProgressionOrder is null)
                .WithButton(definition.IsLocking ? "Blocking on" : "Blocking off",
                    $"ranking_achievement_locking:{args}", ButtonStyle.Secondary,
                    disabled: definition.ProgressionOrder is null)
                .WithButton("Unlock XP", $"ranking_achievement_xp:{args}", ButtonStyle.Secondary)));

        if (editor.Original is null)
            builder.WithTextDisplay("**Completion rule type**")
                .WithActionRow(row => row.WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId($"ranking_achievement_type:{args}")
                    .WithPlaceholder("Completion rule")
                    .WithOptions(RankingAchievementCreationView.TypeOptions(definition.Requirement))));

        return builder.WithTextDisplay("-# Unsaved edits expire 20 minutes after your last change.")
            .WithActionRow(row => row
                .WithButton(editor.Original is null ? "Create achievement" : "Save changes",
                    $"ranking_achievement_save:{args}", ButtonStyle.Success,
                    disabled: !editor.HasChanges || string.IsNullOrWhiteSpace(definition.Name))
                .WithButton("Cancel", $"ranking_achievement_cancel:{args}", ButtonStyle.Secondary));
    }
}