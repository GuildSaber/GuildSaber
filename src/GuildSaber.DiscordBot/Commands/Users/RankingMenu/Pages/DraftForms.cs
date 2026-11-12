using System.Globalization;
using Discord;
using Discord.Interactions;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.DiscordBot.Core.Extensions;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_draft_ratings:*")]
    public async Task ShowDraftRatingsAsync(string sessionId)
    {
        if (await GetDraftEditorAsync(sessionId) is not { } revision) return;

        await RespondWithModalAsync($"ranking_draft_ratings_save:{sessionId}", new RankingDraftRatingsModal
        {
            DifficultyStar = revision.Definition.Rating.DifficultyStar?.ToString("R", CultureInfo.InvariantCulture),
            AccuracyStar = revision.Definition.Rating.AccuracyStar?.ToString("R", CultureInfo.InvariantCulture)
        });
    }

    [ModalInteraction("ranking_draft_ratings_save:*")]
    public async Task SaveDraftRatingsAsync(string sessionId, RankingDraftRatingsModal modal)
    {
        await DeferRankingInteractionAsync();
        if (await GetDraftEditorAsync(sessionId) is not { } revision) return;

        if (!TryDraftNumber(modal.DifficultyStar, out var difficulty) ||
            !TryDraftNumber(modal.AccuracyStar, out var accuracy))
        {
            await FollowupAsync(
                "Star ratings must be finite, non-negative numbers. Use a dot for decimals, or leave a rating blank for automatic calculation.",
                ephemeral: true);
            return;
        }

        await StoreDraftEditorAsync(sessionId, revision with
        {
            Definition = revision.Definition with { Rating = new RankedMapRequests.ManualRating(difficulty, accuracy) }
        });
        await ShowDraftEditorAsync(sessionId);
    }

    [ComponentInteraction("ranking_draft_note:*")]
    public async Task ShowDraftNoteAsync(string sessionId)
    {
        if (await GetDraftEditorAsync(sessionId) is not { } revision) return;

        await RespondWithModalAsync($"ranking_draft_note_save:{sessionId}",
            new RankingDraftNoteModal { Comment = revision.Definition.Comment });
    }

    [ModalInteraction("ranking_draft_note_save:*")]
    public async Task SaveDraftNoteAsync(string sessionId, RankingDraftNoteModal modal)
    {
        await DeferRankingInteractionAsync();
        if (await GetDraftEditorAsync(sessionId) is not { } revision) return;

        await StoreDraftEditorAsync(sessionId, revision with
        {
            Definition = revision.Definition with
            {
                Comment = string.IsNullOrWhiteSpace(modal.Comment) ? null : modal.Comment.Trim()
            }
        });
        await ShowDraftEditorAsync(sessionId);
    }

    [ComponentInteraction("ranking_draft_requirements:*")]
    public async Task ShowDraftRequirementsAsync(string sessionId)
    {
        await DeferRankingInteractionAsync();
        if (await GetDraftEditorAsync(sessionId) is not { } revision) return;

        await ModifyOriginalResponseAsync(x =>
        {
            x.Components = new ComponentBuilderV2()
                .WithContainer(container => container.WithAccentColor(Color.Orange)
                    .WithTextDisplay(
                        $"### Draft score requirements\n{revision.Definition.Requirements.ToDisplayText()}")
                    .WithActionRow(row => row
                        .WithButton("Accuracy and pause limits", $"ranking_draft_limits:{sessionId}",
                            ButtonStyle.Secondary)
                        .WithButton($"Full combo: {(revision.Definition.Requirements.NeedFullCombo ? "On" : "Off")}",
                            $"ranking_draft_toggle:{sessionId},FullCombo", ButtonStyle.Secondary)
                        .WithButton(
                            $"Confirmation: {(revision.Definition.Requirements.NeedConfirmation ? "On" : "Off")}",
                            $"ranking_draft_toggle:{sessionId},Confirmation", ButtonStyle.Secondary))
                    .WithActionRow(row => row
                        .WithButton("Required modifiers", $"ranking_draft_options:{sessionId},MandatoryModifiers,1",
                            ButtonStyle.Secondary)
                        .WithButton("Forbidden modifiers", $"ranking_draft_options:{sessionId},ProhibitedModifiers,1",
                            ButtonStyle.Secondary)
                        .WithButton(revision.IsRevision ? "Back to revision" : "Back to draft",
                            $"ranking_draft_edit:{sessionId}", ButtonStyle.Secondary)))
                .Build();
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_draft_toggle:*,*")]
    public async Task ToggleDraftRequirementAsync(string sessionId, string requirement)
    {
        await DeferRankingInteractionAsync();
        if (await GetDraftEditorAsync(sessionId) is not { } revision) return;

        await StoreDraftEditorAsync(sessionId, revision with
        {
            Definition = revision.Definition with
            {
                Requirements = requirement switch
                {
                    "FullCombo" => revision.Definition.Requirements with
                    {
                        NeedFullCombo = !revision.Definition.Requirements.NeedFullCombo
                    },
                    "Confirmation" => revision.Definition.Requirements with
                    {
                        NeedConfirmation = !revision.Definition.Requirements.NeedConfirmation
                    },
                    _ => throw new ArgumentOutOfRangeException(nameof(requirement))
                }
            }
        });
        await ShowDraftRequirementsAsync(sessionId);
    }

    [ComponentInteraction("ranking_draft_limits:*")]
    public async Task ShowDraftLimitsAsync(string sessionId)
    {
        if (await GetDraftEditorAsync(sessionId) is not { } revision) return;

        await RespondWithModalAsync($"ranking_draft_limits_save:{sessionId}", new RankingDraftLimitsModal
        {
            MinAccuracy = revision.Definition.Requirements.MinAccuracy?.ToString("R", CultureInfo.InvariantCulture),
            MaxPauseDuration =
                revision.Definition.Requirements.MaxPauseDurationSec?.ToString("R", CultureInfo.InvariantCulture)
        });
    }

    [ModalInteraction("ranking_draft_limits_save:*")]
    public async Task SaveDraftLimitsAsync(string sessionId, RankingDraftLimitsModal modal)
    {
        await DeferRankingInteractionAsync();
        if (await GetDraftEditorAsync(sessionId) is not { } revision) return;

        if (!TryDraftNumber(modal.MaxPauseDuration, out var pause)
            || !TryDraftAccuracy(modal.MinAccuracy, out var minimum))
        {
            await FollowupAsync(
                "Minimum accuracy must be between 0 and 100. The pause limit must be finite and non-negative. Leave a field blank for no limit.",
                ephemeral: true);
            return;
        }

        await StoreDraftEditorAsync(sessionId, revision with
        {
            Definition = revision.Definition with
            {
                Requirements = revision.Definition.Requirements with
                {
                    MinAccuracy = minimum, MaxPauseDurationSec = pause
                }
            }
        });
        await ShowDraftRequirementsAsync(sessionId);
    }

    private static bool TryDraftNumber(string? input, out float? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(input)) return true;
        if (!float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            || !float.IsFinite(number) || number < 0) return false;

        value = number;
        return true;
    }

    private static bool TryDraftAccuracy(string? input, out double? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(input)) return true;
        if (!double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            || !double.IsFinite(number) || number is < 0 or > 100) return false;

        value = number;
        return true;
    }
}

public class RankingDraftRatingsModal : IModal
{
    public string Title => "Revise draft ratings";

    [InputLabel("Difficulty stars", "Leave blank to calculate when submitting. Use a dot for decimals.")]
    [ModalTextInput("difficultyStar", maxLength: 64)]
    [RequiredInput(false)]
    public string? DifficultyStar { get; set; }

    [InputLabel("Accuracy stars", "Leave blank to calculate when submitting. Use a dot for decimals.")]
    [ModalTextInput("accuracyStar", maxLength: 64)]
    [RequiredInput(false)]
    public string? AccuracyStar { get; set; }
}

public class RankingDraftNoteModal : IModal
{
    public string Title => "Revise author's note";

    [InputLabel("Author's note", "Optional explanation of the proposed changes.")]
    [ModalTextInput("comment", TextInputStyle.Paragraph, maxLength: 2000)]
    [RequiredInput(false)]
    public string? Comment { get; set; }
}

public class RankingDraftLimitsModal : IModal
{
    public string Title => "Revise score limits";

    [InputLabel("Minimum accuracy (%)", "Leave blank for no minimum. Use a dot for decimals.")]
    [ModalTextInput("minAccuracy", maxLength: 64)]
    [RequiredInput(false)]
    public string? MinAccuracy { get; set; }

    [InputLabel("Maximum total pause (seconds)",
        "Leave blank for no limit. Scores must provide pause statistics when a limit is set.")]
    [ModalTextInput("maxPauseDuration", maxLength: 64)]
    [RequiredInput(false)]
    public string? MaxPauseDuration { get; set; }
}