using System.Numerics;
using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.Guilds.Achievements.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Common.Helpers;
using GuildSaber.Common.Result;
using GuildSaber.DiscordBot.Core.Extensions;
using RankDraft = GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests.CreateDraft.Rank;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    public enum ERankingDraftSelection
    {
        Categories,
        Achievements,
        MandatoryModifiers,
        ProhibitedModifiers
    }

    [ComponentInteraction("ranking_draft_options:*,*,*")]
    public async Task ShowDraftOptionsAsync(string sessionId, ERankingDraftSelection selection, int page)
    {
        if (await GetDraftEditorAsync(sessionId) is not { } revision) return;

        var options = await (
            DeferRankingInteractionAsync(),
            GetDraftOptionsAsync(revision, selection)
        ).WhenAll();
        page = Math.Clamp(page, 1, Math.Max(1, (options.Length + 24) / 25));

        await ModifyOriginalResponseAsync(x =>
        {
            x.Components = RankingDraftSelectionView.BuildComponent(sessionId, revision, selection, options, page)
                .Build();
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_draft_select:*,*,*")]
    public async Task SelectDraftOptionsAsync(
        string sessionId, ERankingDraftSelection selection, int page, string[] values)
    {
        if (await GetDraftEditorAsync(sessionId) is not { } revision) return;

        var visible = (await (
                DeferRankingInteractionAsync(),
                GetDraftOptionsAsync(revision, selection)
            ).WhenAll()).Skip((page - 1) * 25).Take(25)
            .Select(x => x.Id).ToHashSet();
        var selected = RankingDraftSelectionView.GetSelected(revision.Definition, selection)
            .Where(x => !visible.Contains(x))
            .Concat(values.Select(int.Parse).Where(visible.Contains))
            .Distinct().Order();

        await StoreDraftEditorAsync(sessionId, revision with
        {
            Definition = RankingDraftSelectionView.Apply(revision.Definition, selection, selected)
        });
        await ShowDraftOptionsAsync(sessionId, selection, page);
    }

    [ComponentInteraction("ranking_draft_clear:*,*")]
    public async Task ClearDraftOptionsAsync(string sessionId, ERankingDraftSelection selection)
    {
        if (await GetDraftEditorAsync(sessionId, defer: true) is not { } revision) return;

        await StoreDraftEditorAsync(sessionId, revision with
        {
            Definition = RankingDraftSelectionView.Apply(revision.Definition, selection, [])
        });
        await ShowDraftOptionsAsync(sessionId, selection, 1);
    }

    [ComponentInteraction("ranking_draft_default_modifiers:*")]
    public async Task ResetDraftModifiersAsync(string sessionId)
    {
        if (await GetDraftEditorAsync(sessionId, defer: true) is not { } revision) return;

        await StoreDraftEditorAsync(sessionId, revision with
        {
            Definition = revision.Definition with
            {
                Requirements = revision.Definition.Requirements with
                {
                    ProhibitedModifiers = RankedMapRequests.EModifiers.ProhibitedDefaults
                }
            }
        });
        await ShowDraftOptionsAsync(sessionId, ERankingDraftSelection.ProhibitedModifiers, 1);
    }

    private async Task<RankingDraftOption[]> GetDraftOptionsAsync(
        RankingDraftEditor revision, ERankingDraftSelection selection)
        => selection switch
        {
            ERankingDraftSelection.Categories => (await Cache.GetGuildCategoriesAsync(await GetGuildIdAsync(),
                    Client.Value))
                .OrderBy(x => x.Info.Name).Select(x => new RankingDraftOption(x.Id.Value, x.Info.Name)).ToArray(),
            ERankingDraftSelection.Achievements => (await Client.Value.Achievements
                    .GetByContextIdAsync(revision.ContextId.Value, null, true).Unwrap())
                .OfType<AchievementResponses.Achievement.RankedMapListAchievement>()
                .OrderBy(x => x.Info.Name).Select(x => new RankingDraftOption(x.Id, x.Info.Name)).ToArray(),
            _ => Enum.GetValues<RankedMapRequests.EModifiers>()
                .Where(x => BitOperations.IsPow2((uint)x))
                .Select(x => new RankingDraftOption((int)x,
                    x == RankedMapRequests.EModifiers.Unk
                        ? "Unknown modifiers"
                        : Regex.Replace(x.ToString(), "(?<=[a-z])(?=[A-Z])", " ")))
                .ToArray()
        };
}

public readonly record struct RankingDraftOption(int Id, string Name);

file static class RankingDraftSelectionView
{
    public static IEnumerable<int> GetSelected(RankDraft definition, UserModuleSlash.ERankingDraftSelection selection)
        => selection switch
        {
            UserModuleSlash.ERankingDraftSelection.Categories => definition.CategoryIds.Select(x => x.Value),
            UserModuleSlash.ERankingDraftSelection.Achievements => definition.AchievementIds,
            _ => Enum.GetValues<RankedMapRequests.EModifiers>()
                .Where(x => BitOperations.IsPow2((uint)x) &&
                            (selection == UserModuleSlash.ERankingDraftSelection.MandatoryModifiers
                                ? definition.Requirements.MandatoryModifiers.HasFlag(x)
                                : definition.Requirements.ProhibitedModifiers.HasFlag(x)))
                .Select(x => (int)x)
        };

    public static RankDraft Apply(
        RankDraft definition, UserModuleSlash.ERankingDraftSelection selection, IEnumerable<int> values)
        => selection switch
        {
            UserModuleSlash.ERankingDraftSelection.Categories => definition with
            {
                CategoryIds = values.Select(x => new CategoryId(x)).ToArray()
            },
            UserModuleSlash.ERankingDraftSelection.Achievements =>
                definition with { AchievementIds = values.ToArray() },
            UserModuleSlash.ERankingDraftSelection.MandatoryModifiers => definition with
            {
                Requirements = definition.Requirements with
                {
                    MandatoryModifiers = values.Aggregate(RankedMapRequests.EModifiers.None,
                        (current, value) => current | (RankedMapRequests.EModifiers)value)
                }
            },
            UserModuleSlash.ERankingDraftSelection.ProhibitedModifiers => definition with
            {
                Requirements = definition.Requirements with
                {
                    ProhibitedModifiers = values.Aggregate(RankedMapRequests.EModifiers.None,
                        (current, value) => current | (RankedMapRequests.EModifiers)value)
                }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(selection))
        };

    public static ComponentBuilderV2 BuildComponent(
        string sessionId, RankingDraftEditor revision, UserModuleSlash.ERankingDraftSelection selection,
        RankingDraftOption[] options, int page)
    {
        var builder = new ComponentBuilderV2().WithTextDisplay("### " + selection switch
        {
            UserModuleSlash.ERankingDraftSelection.Categories => "Categories",
            UserModuleSlash.ERankingDraftSelection.Achievements => "Map-list associations",
            UserModuleSlash.ERankingDraftSelection.MandatoryModifiers => "Required modifiers",
            UserModuleSlash.ERankingDraftSelection.ProhibitedModifiers => "Forbidden modifiers",
            _ => throw new ArgumentOutOfRangeException(nameof(selection))
        });
        var selected = GetSelected(revision.Definition, selection).ToHashSet();
        var visible = options.Skip((page - 1) * 25).Take(25).ToArray();
        var backToRequirements = selection is UserModuleSlash.ERankingDraftSelection.MandatoryModifiers
                                              or UserModuleSlash.ERankingDraftSelection.ProhibitedModifiers;

        if (visible.Length == 0)
            builder.WithTextDisplay(selection == UserModuleSlash.ERankingDraftSelection.Achievements
                ? "No map-list achievements exist in this context.\n" +
                  "Star achievements match automatically from ratings and categories."
                : "No available options.");
        else
            builder.WithActionRow(row => row.WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId($"ranking_draft_select:{sessionId},{selection},{page}")
                .WithPlaceholder("Select any that apply")
                .WithMinValues(0).WithMaxValues(visible.Length)
                .WithOptions(visible.Select(x => new SelectMenuOptionBuilder(x.Name, x.Id.ToString(),
                    isDefault: selected.Contains(x.Id))).ToList())));

        if (selection == UserModuleSlash.ERankingDraftSelection.ProhibitedModifiers)
            builder.WithActionRow(row => row.WithButton("Restore defaults",
                $"ranking_draft_default_modifiers:{sessionId}", ButtonStyle.Secondary));

        return builder
            .WithTextDisplay(
                $"Page **{page}**/{Math.Max(1, (options.Length + 24) / 25)} · **{selected.Count}** selected")
            .WithActionRow(row => row
                .WithButton(
                    backToRequirements  ? "Back to requirements" :
                    revision.IsRevision ? "Back to revision" : "Back to draft",
                    backToRequirements ? $"ranking_draft_requirements:{sessionId}" : $"ranking_draft_edit:{sessionId}",
                    ButtonStyle.Secondary)
                .WithButton("Clear all", $"ranking_draft_clear:{sessionId},{selection}", ButtonStyle.Secondary,
                    disabled: selected.Count == 0)
                .WithButton("Prev", $"ranking_draft_options:{sessionId},{selection},{page - 1}",
                    disabled: page <= 1)
                .WithButton("Next", $"ranking_draft_options:{sessionId},{selection},{page + 1}",
                    disabled: page * 25 >= options.Length));
    }
}