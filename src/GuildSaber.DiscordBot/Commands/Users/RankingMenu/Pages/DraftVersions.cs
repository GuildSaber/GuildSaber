using System.Globalization;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [ComponentInteraction("ranking_draft_versions:*,*")]
    public async Task ShowDraftVersionsAsync(string sessionId, int page)
    {
        if (await GetDraftEditorAsync(sessionId, defer: true) is not { } revision) return;

        page = Math.Clamp(page, 1, revision.Definition.MapVersions.Length);
        await ModifyOriginalResponseAsync(x =>
        {
            x.Components = RankingDraftVersionView.BuildComponent(sessionId, revision, page).Build();
            x.AllowedMentions = AllowedMentions.None;
        });
    }

    [ComponentInteraction("ranking_draft_version_edit:*,*")]
    public async Task ShowDraftVersionFormAsync(string sessionId, int index)
    {
        if (await GetDraftEditorAsync(sessionId) is not { } revision) return;
        if (index < -1 || index >= revision.Definition.MapVersions.Length)
        {
            await RespondAsync("The map-version list changed. Reopen the list and try again.", ephemeral: true);
            return;
        }

        var version = index < 0 ? null : revision.Definition.MapVersions[index];
        await RespondWithModalAsync($"ranking_draft_version_save:{sessionId},{index}", new RankingDraftVersionModal
        {
            Song = version?.Song switch
            {
                SongReference.ByKey(var key) => key.ToString(),
                SongReference.ByHash(var hash) => hash.ToString(),
                _ => ""
            },
            Characteristic = version?.Characteristic ?? "Standard",
            Difficulty = (version?.Difficulty ?? EDifficulty.ExpertPlus).ToString(),
            PlayMode = version?.PlayMode ?? "Standard",
            Order = (version?.Order ?? Math.Min(byte.MaxValue, revision.Definition.MapVersions.Max(x => x.Order) + 1))
                .ToString(CultureInfo.InvariantCulture)
        });
    }

    [ModalInteraction("ranking_draft_version_save:*,*")]
    public async Task SaveDraftVersionAsync(string sessionId, int index, RankingDraftVersionModal modal)
    {
        if (await GetDraftEditorAsync(sessionId, defer: true) is not { } revision) return;

        if (!ParseDraftSong(modal.Song).TryGetValue(out var song, out var error))
        {
            await FollowupAsync(error, ephemeral: true);
            return;
        }

        if (!Enum.TryParse<EDifficulty>(modal.Difficulty.Trim().Replace("+", "Plus"), true, out var difficulty)
            || !Enum.IsDefined(difficulty) || !byte.TryParse(modal.Order, out var order))
        {
            await FollowupAsync("Use Easy, Normal, Hard, Expert or ExpertPlus, and a display order between 0 and 255.",
                ephemeral: true);
            return;
        }

        if (index < -1 || index >= revision.Definition.MapVersions.Length)
        {
            await ShowDraftVersionsAsync(sessionId, 1);
            await FollowupAsync("The map-version list changed. Reopen the version and try again.", ephemeral: true);
            return;
        }

        var version = new MapVersionInput(song, modal.Characteristic.Trim(), difficulty, modal.PlayMode.Trim(), order);
        await StoreDraftEditorAsync(sessionId, revision with
        {
            Definition = revision.Definition with
            {
                MapVersions = index < 0
                    ? [.. revision.Definition.MapVersions, version]
                    : revision.Definition.MapVersions.Select((value, position) => position == index ? version : value)
                        .ToArray()
            }
        });
        await ShowDraftVersionsAsync(sessionId, index < 0 ? revision.Definition.MapVersions.Length + 1 : index + 1);
    }

    [ComponentInteraction("ranking_draft_version_remove:*,*")]
    public async Task RemoveDraftVersionAsync(string sessionId, int index)
    {
        if (await GetDraftEditorAsync(sessionId, defer: true) is not { } revision) return;

        if (revision.Definition.MapVersions.Length <= 1)
        {
            await FollowupAsync("A draft needs at least one map version. Edit this version instead of removing it.",
                ephemeral: true);
            return;
        }

        await StoreDraftEditorAsync(sessionId, revision with
        {
            Definition = revision.Definition with
            {
                MapVersions = revision.Definition.MapVersions.Where((_, position) => position != index).ToArray()
            }
        });
        await ShowDraftVersionsAsync(sessionId, Math.Max(1, index));
    }

    private static Result<SongReference> ParseDraftSong(string input)
    {
        var value = input.Trim().ToLowerInvariant();
        if (Uri.TryCreate(value, UriKind.Absolute, out var link))
        {
            if (link.Scheme is not ("http" or "https") || link.Host is not ("beatsaver.com" or "www.beatsaver.com")
                                                       || link.Segments is not ["/", "maps/", var key])
                return Failure<SongReference>("Use a BeatSaver map link, key or exact song hash.");

            value = key.TrimEnd('/');
        }

        if (value.StartsWith("!bsr ")) value = value[5..].Trim();
        if (value.Length == 0) return Failure<SongReference>("Enter a BeatSaver key or exact song hash.");

        return value.Length == SongHash.ExactLength
            ? SongHash.TryCreate(value).Map(hash => (SongReference)new SongReference.ByHash(hash))
            : BeatSaverKey.TryCreate(value).Map(key => (SongReference)new SongReference.ByKey(key));
    }
}

public class RankingDraftVersionModal : IModal
{
    public string Title => "Revise map version";

    [InputLabel("BeatSaver key or exact hash",
        "A key fetches the latest version when saving. A hash keeps that exact song version.")]
    [ModalTextInput("song", placeholder: "1bd7, !bsr 1bd7, or a 40-character hash", maxLength: 128)]
    public string Song { get; set; } = "";

    [InputLabel("Characteristic", "For example Standard or OneSaber.")]
    [ModalTextInput("characteristic", maxLength: 128)]
    public string Characteristic { get; set; } = "Standard";

    [InputLabel("Difficulty", "Easy, Normal, Hard, Expert or ExpertPlus.")]
    [ModalTextInput("difficulty", maxLength: 32)]
    public string Difficulty { get; set; } = "ExpertPlus";

    [InputLabel("Play mode", "For example Standard or ReBeat.")]
    [ModalTextInput("playMode", maxLength: 128)]
    public string PlayMode { get; set; } = "Standard";

    [InputLabel("Display order", "0 to 255. The lowest-order difficulty supplies automatic ratings.")]
    [ModalTextInput("order", maxLength: 3)]
    public string Order { get; set; } = "0";
}

file static class RankingDraftVersionView
{
    public static ComponentBuilderV2 BuildComponent(string sessionId, RankingDraftEditor revision, int page)
    {
        var version = revision.Definition.MapVersions[page - 1];
        return new ComponentBuilderV2()
            .WithContainer(container => container.WithAccentColor(Color.Orange)
                .WithTextDisplay($"### Map version {page}/{revision.Definition.MapVersions.Length}\n" +
                                 version.Song switch
                                 {
                                     SongReference.ByKey(var key) =>
                                         $"**BeatSaver:** {key.ToBsrKey()} (resolve latest version on save)",
                                     SongReference.ByHash(var hash) => $"**Exact song hash:** `{hash}`",
                                     _ => throw new ArgumentOutOfRangeException(nameof(version.Song))
                                 } +
                                 $"\n**Difficulty:** {version.Difficulty}, {version.Characteristic}\n" +
                                 $"**Play mode:** {version.PlayMode}\n**Display order:** {version.Order}")
                .WithActionRow(row => row
                    .WithButton("Revise version", $"ranking_draft_version_edit:{sessionId},{page - 1}")
                    .WithButton("Add version", $"ranking_draft_version_edit:{sessionId},-1", ButtonStyle.Secondary)
                    .WithButton("Remove version", $"ranking_draft_version_remove:{sessionId},{page - 1}",
                        ButtonStyle.Danger, disabled: revision.Definition.MapVersions.Length <= 1))
                .WithActionRow(row => row
                    .WithButton(revision.IsRevision ? "Back to revision" : "Back to draft",
                        $"ranking_draft_edit:{sessionId}", ButtonStyle.Secondary)
                    .WithButton("Previous", $"ranking_draft_versions:{sessionId},{page - 1}", disabled: page <= 1)
                    .WithButton("Next", $"ranking_draft_versions:{sessionId},{page + 1}",
                        disabled: page >= revision.Definition.MapVersions.Length)));
    }
}